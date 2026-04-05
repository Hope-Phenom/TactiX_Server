using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 排行榜时间周期常量
/// </summary>
public static class LeaderboardPeriods
{
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string All = "all";

    public static readonly string[] ValidPeriods = { Daily, Weekly, Monthly, All };

    public static bool IsValid(string period) => ValidPeriods.Contains(period.ToLower());

    public static DateTime GetStartDate(string period)
    {
        var now = DateTime.UtcNow;
        return period.ToLower() switch
        {
            Daily => now.AddDays(-1),
            Weekly => now.AddDays(-7),
            Monthly => now.AddMonths(-1),
            _ => DateTime.MinValue
        };
    }
}

/// <summary>
/// 排行榜排序方式常量
/// </summary>
public static class LeaderboardSortBy
{
    public const string Downloads = "downloads";
    public const string Likes = "likes";

    public static readonly string[] ValidSortBy = { Downloads, Likes };

    public static bool IsValid(string sortBy) => ValidSortBy.Contains(sortBy.ToLower());
}

/// <summary>
/// 排行榜服务接口
/// </summary>
public interface ITacticsLeaderboardService
{
    /// <summary>获取热门战术排行榜</summary>
    Task<LeaderboardResponse> GetHotFilesAsync(string period, string? race, string sortBy, int page, int pageSize);

    /// <summary>获取贡献者排行榜</summary>
    Task<UploaderLeaderboardResponse> GetTopUploadersAsync(string period, int page, int pageSize);
}

/// <summary>
/// 排行榜服务实现
/// </summary>
public class TacticsLeaderboardService : ITacticsLeaderboardService
{
    private readonly TacticsDbContext _context;
    private readonly ILogger<TacticsLeaderboardService> _logger;

    public TacticsLeaderboardService(
        TacticsDbContext context,
        ILogger<TacticsLeaderboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeaderboardResponse> GetHotFilesAsync(string period, string? race, string sortBy, int page, int pageSize)
    {
        if (!LeaderboardPeriods.IsValid(period))
        {
            period = LeaderboardPeriods.Weekly;
        }

        if (!string.IsNullOrEmpty(race) && !Races.IsValid(race))
        {
            race = null;
        }

        if (!LeaderboardSortBy.IsValid(sortBy))
        {
            sortBy = LeaderboardSortBy.Downloads;
        }

        var startDate = LeaderboardPeriods.GetStartDate(period);

        var query = _context.TacticsFiles
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.Status == FileStatus.Approved);

        if (startDate != DateTime.MinValue)
        {
            query = query.Where(f => f.CreatedAt >= startDate);
        }

        if (!string.IsNullOrEmpty(race))
        {
            query = query.Where(f => f.Race == race.ToUpper());
        }

        query = sortBy.ToLower() switch
        {
            LeaderboardSortBy.Likes => query.OrderByDescending(f => f.LikeCount).ThenByDescending(f => f.DownloadCount),
            _ => query.OrderByDescending(f => f.DownloadCount).ThenByDescending(f => f.LikeCount)
        };

        var totalCount = await query.CountAsync();

        var rankOffset = (page - 1) * pageSize;
        var files = await query
            .Skip(rankOffset)
            .Take(pageSize)
            .Select(f => new HotFileRankItem
            {
                Rank = 0,
                ShareCode = f.ShareCode,
                Name = f.Name,
                Author = f.Author,
                Race = f.Race,
                RaceDisplay = Races.GetDisplayName(f.Race),
                DownloadCount = f.DownloadCount,
                LikeCount = f.LikeCount,
                FavoriteCount = f.FavoriteCount,
                CreatedAt = f.CreatedAt,
                Uploader = f.Uploader != null ? new UserBriefResponse
                {
                    Id = f.Uploader.Id,
                    Nickname = f.Uploader.Nickname,
                    AvatarUrl = f.Uploader.AvatarUrl,
                    LevelCode = f.Uploader.LevelCode
                } : null
            })
            .ToListAsync();

        for (var i = 0; i < files.Count; i++)
        {
            files[i].Rank = rankOffset + i + 1;
        }

        return new LeaderboardResponse
        {
            Period = period,
            Race = race,
            SortBy = sortBy,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Files = files
        };
    }

    public async Task<UploaderLeaderboardResponse> GetTopUploadersAsync(string period, int page, int pageSize)
    {
        if (!LeaderboardPeriods.IsValid(period))
        {
            period = LeaderboardPeriods.Monthly;
        }

        var startDate = LeaderboardPeriods.GetStartDate(period);

        var baseQuery = _context.TacticsFiles
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.Status == FileStatus.Approved);

        if (startDate != DateTime.MinValue)
        {
            baseQuery = baseQuery.Where(f => f.CreatedAt >= startDate);
        }

        // 先获取总数（需要单独查询因为GroupBy后无法直接Count）
        var totalCount = await baseQuery
            .Select(f => f.UploaderId)
            .Distinct()
            .CountAsync();

        // 在数据库层面完成分组和分页
        var rankOffset = (page - 1) * pageSize;
        var uploaderStats = await baseQuery
            .GroupBy(f => f.UploaderId)
            .Select(g => new
            {
                UploaderId = g.Key,
                UploadCount = g.Count(),
                TotalDownloadCount = (uint)g.Sum(f => (int)f.DownloadCount),
                TotalLikeCount = (uint)g.Sum(f => (int)f.LikeCount)
            })
            .OrderByDescending(x => x.UploadCount)
            .ThenByDescending(x => x.TotalDownloadCount)
            .Skip(rankOffset)
            .Take(pageSize)
            .ToListAsync();

        // 获取用户详细信息
        var userIds = uploaderStats.Select(x => x.UploaderId).ToList();
        var users = await _context.TacticsUsers
            .AsNoTracking()
            .Include(u => u.LevelConfig)
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var items = uploaderStats.Select((x, i) =>
        {
            var user = users.GetValueOrDefault(x.UploaderId);
            var qualityScore = Math.Round(x.TotalDownloadCount * 0.3 + x.TotalLikeCount * 0.7, 2);

            return new UploaderRankItem
            {
                Rank = rankOffset + i + 1,
                UserId = x.UploaderId,
                Nickname = user?.Nickname,
                AvatarUrl = user?.AvatarUrl,
                LevelCode = user?.LevelCode,
                LevelName = user?.LevelConfig?.LevelName,
                UploadCount = x.UploadCount,
                TotalDownloadCount = x.TotalDownloadCount,
                TotalLikeCount = x.TotalLikeCount,
                QualityScore = qualityScore
            };
        }).ToList();

        return new UploaderLeaderboardResponse
        {
            Period = period,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Uploaders = items
        };
    }
}