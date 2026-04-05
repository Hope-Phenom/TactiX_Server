using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 互动操作结果
/// </summary>
public class InteractionResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static InteractionResult Success() => new() { Succeeded = true };
    public static InteractionResult Failed(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 战术互动服务接口
/// </summary>
public interface ITacticsInteractionService
{
    /// <summary>点赞/取消点赞</summary>
    Task<LikeToggleResponse?> ToggleLikeAsync(long userId, string shareCode);

    /// <summary>收藏/取消收藏</summary>
    Task<FavoriteToggleResponse?> ToggleFavoriteAsync(long userId, string shareCode);

    /// <summary>获取用户点赞的文件列表</summary>
    Task<UserInteractionListResponse> GetUserLikedFilesAsync(long userId, int page, int pageSize);

    /// <summary>获取用户收藏列表</summary>
    Task<UserInteractionListResponse> GetUserFavoritesAsync(long userId, int page, int pageSize);

    /// <summary>添加评论</summary>
    Task<AddCommentResponse?> AddCommentAsync(long userId, string shareCode, string content, long? parentCommentId);

    /// <summary>获取文件评论列表</summary>
    Task<CommentListResponse> GetCommentsAsync(string shareCode, int page, int pageSize);

    /// <summary>删除评论（软删除）</summary>
    Task<InteractionResult> DeleteCommentAsync(long userId, long commentId);
}

/// <summary>
/// 战术互动服务实现
/// </summary>
public class TacticsInteractionService : ITacticsInteractionService
{
    private readonly TacticsDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<TacticsInteractionService> _logger;

    public TacticsInteractionService(
        TacticsDbContext context,
        IPermissionService permissionService,
        ILogger<TacticsInteractionService> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<LikeToggleResponse?> ToggleLikeAsync(long userId, string shareCode)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null) return null;

        // 检查文件是否存在且已审核
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Id == fileId.Value && !f.IsDeleted)
            .Select(f => new { f.Id, f.Status })
            .FirstOrDefaultAsync();

        if (file == null) return null;
        if (file.Status != FileStatus.Approved)
            return new LikeToggleResponse { LikeCount = 0, IsLiked = false };

        // 点赞需要CanComment权限
        var permission = await _permissionService.CanCommentAsync(userId);
        if (!permission.Succeeded) return null;

        // 使用执行策略包装事务操作（解决MySqlRetryingExecutionStrategy不支持用户事务的问题）
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingLike = await _context.TacticsLikes
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.FileId == fileId.Value);

                bool isLiked;
                uint likeCount;
                if (existingLike != null)
                {
                    _context.TacticsLikes.Remove(existingLike);
                    await _context.TacticsFiles
                        .Where(f => f.Id == fileId.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.LikeCount, f => f.LikeCount - 1));
                    isLiked = false;
                }
                else
                {
                    _context.TacticsLikes.Add(new TacticsLikeModel
                    {
                        UserId = userId,
                        FileId = fileId.Value,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.TacticsFiles
                        .Where(f => f.Id == fileId.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.LikeCount, f => f.LikeCount + 1));
                    isLiked = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 获取更新后的计数
                likeCount = await _context.TacticsFiles
                    .Where(f => f.Id == fileId.Value)
                    .Select(f => f.LikeCount)
                    .FirstOrDefaultAsync();

                return new LikeToggleResponse { IsLiked = isLiked, LikeCount = likeCount };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "点赞操作失败: UserId={UserId}, FileId={FileId}", userId, fileId);
                return null;
            }
        });
    }

    public async Task<FavoriteToggleResponse?> ToggleFavoriteAsync(long userId, string shareCode)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null) return null;

        // 检查文件是否存在且已审核
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Id == fileId.Value && !f.IsDeleted)
            .Select(f => new { f.Id, f.Status })
            .FirstOrDefaultAsync();

        if (file == null) return null;
        if (file.Status != FileStatus.Approved)
            return new FavoriteToggleResponse { FavoriteCount = 0, IsFavorited = false };

        // 使用执行策略包装事务操作
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingFavorite = await _context.TacticsFavorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.FileId == fileId.Value);

                bool isFavorited;
                uint favoriteCount;
                if (existingFavorite != null)
                {
                    _context.TacticsFavorites.Remove(existingFavorite);
                    await _context.TacticsFiles
                        .Where(f => f.Id == fileId.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.FavoriteCount, f => f.FavoriteCount - 1));
                    isFavorited = false;
                }
                else
                {
                    _context.TacticsFavorites.Add(new TacticsFavoriteModel
                    {
                        UserId = userId,
                        FileId = fileId.Value,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.TacticsFiles
                        .Where(f => f.Id == fileId.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.FavoriteCount, f => f.FavoriteCount + 1));
                    isFavorited = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                favoriteCount = await _context.TacticsFiles
                    .Where(f => f.Id == fileId.Value)
                    .Select(f => f.FavoriteCount)
                    .FirstOrDefaultAsync();

                return new FavoriteToggleResponse { IsFavorited = isFavorited, FavoriteCount = favoriteCount };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "收藏操作失败: UserId={UserId}, FileId={FileId}", userId, fileId);
                return null;
            }
        });
    }

    public async Task<UserInteractionListResponse> GetUserLikedFilesAsync(long userId, int page, int pageSize)
    {
        var query = from like in _context.TacticsLikes
                    join file in _context.TacticsFiles on like.FileId equals file.Id
                    where like.UserId == userId && !file.IsDeleted && file.Status == FileStatus.Approved
                    orderby like.CreatedAt descending
                    select new { file, like.CreatedAt };

        var totalCount = await query.CountAsync();

        var files = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new TacticsBriefResponse
            {
                ShareCode = f.file.ShareCode,
                Name = f.file.Name,
                Author = f.file.Author,
                Race = f.file.Race,
                RaceDisplay = Races.GetDisplayName(f.file.Race),
                DownloadCount = f.file.DownloadCount,
                LikeCount = f.file.LikeCount,
                CurrentVersion = f.file.Version,
                CreatedAt = f.file.CreatedAt
            })
            .ToListAsync();

        return new UserInteractionListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Files = files
        };
    }

    public async Task<UserInteractionListResponse> GetUserFavoritesAsync(long userId, int page, int pageSize)
    {
        var query = from fav in _context.TacticsFavorites
                    join file in _context.TacticsFiles on fav.FileId equals file.Id
                    where fav.UserId == userId && !file.IsDeleted && file.Status == FileStatus.Approved
                    orderby fav.CreatedAt descending
                    select new { file, fav.CreatedAt };

        var totalCount = await query.CountAsync();

        var files = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new TacticsBriefResponse
            {
                ShareCode = f.file.ShareCode,
                Name = f.file.Name,
                Author = f.file.Author,
                Race = f.file.Race,
                RaceDisplay = Races.GetDisplayName(f.file.Race),
                DownloadCount = f.file.DownloadCount,
                LikeCount = f.file.LikeCount,
                CurrentVersion = f.file.Version,
                CreatedAt = f.file.CreatedAt
            })
            .ToListAsync();

        return new UserInteractionListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Files = files
        };
    }

    public async Task<AddCommentResponse?> AddCommentAsync(long userId, string shareCode, string content, long? parentCommentId)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null) return null;

        // 检查文件是否存在且已审核
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Id == fileId.Value && !f.IsDeleted)
            .Select(f => new { f.Id, f.Status })
            .FirstOrDefaultAsync();

        if (file == null) return null;
        if (file.Status != FileStatus.Approved) return null;

        // 检查评论权限
        var permission = await _permissionService.CanCommentAsync(userId);
        if (!permission.Succeeded) return null;

        // XSS过滤
        var sanitizedContent = System.Net.WebUtility.HtmlEncode(content);

        // 如果有父评论，检查父评论是否属于同一文件且存在
        if (parentCommentId.HasValue)
        {
            var parentComment = await _context.TacticsComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value && !c.IsDeleted);

            if (parentComment == null || parentComment.FileId != fileId.Value)
                return null;

            // 限制嵌套深度为2层
            if (parentComment.ParentCommentId.HasValue)
                return null;
        }

        var now = DateTime.UtcNow;
        var comment = new TacticsCommentModel
        {
            UserId = userId,
            FileId = fileId.Value,
            ParentCommentId = parentCommentId,
            Content = sanitizedContent,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.TacticsComments.Add(comment);
        await _context.SaveChangesAsync();

        return new AddCommentResponse
        {
            CommentId = comment.Id,
            Content = sanitizedContent,
            CreatedAt = now,
            ParentCommentId = parentCommentId
        };
    }

    public async Task<CommentListResponse> GetCommentsAsync(string shareCode, int page, int pageSize)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return new CommentListResponse();
        }

        // 只获取顶级评论（没有父评论的）
        var query = _context.TacticsComments
            .Where(c => c.FileId == fileId.Value && !c.ParentCommentId.HasValue)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                Content = c.IsDeleted ? "[已删除]" : c.Content,
                IsDeleted = c.IsDeleted,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ParentCommentId = null,
                Author = c.IsDeleted ? null : new UserBriefResponse
                {
                    Id = c.User!.Id,
                    Nickname = c.User.Nickname,
                    AvatarUrl = c.User.AvatarUrl,
                    LevelCode = c.User.LevelCode
                }
            })
            .ToListAsync();

        // 获取子评论
        var parentIds = comments.Select(c => c.Id).ToList();
        var replies = await _context.TacticsComments
            .Where(c => c.ParentCommentId.HasValue && parentIds.Contains(c.ParentCommentId.Value))
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                Content = c.IsDeleted ? "[已删除]" : c.Content,
                IsDeleted = c.IsDeleted,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                ParentCommentId = c.ParentCommentId,
                Author = c.IsDeleted ? null : new UserBriefResponse
                {
                    Id = c.User!.Id,
                    Nickname = c.User.Nickname,
                    AvatarUrl = c.User.AvatarUrl,
                    LevelCode = c.User.LevelCode
                }
            })
            .ToListAsync();

        // 将子评论分配到对应的父评论下
        foreach (var comment in comments)
        {
            comment.Replies = replies.Where(r => r.ParentCommentId == comment.Id).ToList();
        }

        return new CommentListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Comments = comments
        };
    }

    public async Task<InteractionResult> DeleteCommentAsync(long userId, long commentId)
    {
        var permission = await _permissionService.CanDeleteCommentAsync(userId, commentId);
        if (!permission.Succeeded)
        {
            return InteractionResult.Failed(permission.ErrorMessage!);
        }

        var comment = await _context.TacticsComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
        {
            return InteractionResult.Failed("评论不存在");
        }

        // 软删除
        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {UserId} 删除了评论 {CommentId}", userId, commentId);

        return InteractionResult.Success();
    }
}