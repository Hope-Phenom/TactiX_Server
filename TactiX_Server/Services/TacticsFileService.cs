using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using TactiX_Server.Data;
using TactiX_Server.Models.Config;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 战术文件服务接口
/// </summary>
public interface ITacticsFileService
{
    /// <summary>
    /// 上传战术文件
    /// </summary>
    Task<UploadResult> UploadFileAsync(long userId, Stream fileStream, string fileName, string? changelog = null);

    /// <summary>
    /// 上传战术文件新版本
    /// </summary>
    Task<UploadResult> UploadVersionAsync(long userId, string shareCode, Stream fileStream, string? changelog = null);

    /// <summary>
    /// 获取文件详情
    /// </summary>
    Task<TacticsFileModel?> GetFileByShareCodeAsync(string shareCode);

    /// <summary>
    /// 获取文件版本列表
    /// </summary>
    Task<List<TacticsFileVersionModel>> GetFileVersionsAsync(string shareCode);

    /// <summary>
    /// 下载文件
    /// </summary>
    Task<FileDownloadResult?> DownloadFileAsync(string shareCode, int? versionNumber = null);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<DeleteResult> DeleteFileAsync(long userId, string shareCode);
}

/// <summary>
/// 战术文件服务实现
/// </summary>
public class TacticsFileService : ITacticsFileService
{
    private readonly TacticsDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IFileSecurityValidator _securityValidator;
    private readonly TacticsHallConfig _config;
    private readonly ILogger<TacticsFileService> _logger;

    public TacticsFileService(
        TacticsDbContext context,
        IPermissionService permissionService,
        IFileSecurityValidator securityValidator,
        IOptions<TacticsHallConfig> config,
        ILogger<TacticsFileService> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _securityValidator = securityValidator;
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadFileAsync(long userId, Stream fileStream, string fileName, string? changelog = null)
    {
        var tempFilePath = string.Empty;

        try
        {
            // 1. 权限检查
            var permissionResult = await _permissionService.CanUpload(userId, fileStream.Length);
            if (!permissionResult.Success)
            {
                return UploadResult.Fail(permissionResult.ErrorMessage);
            }

            // 2. 保存到临时文件
            var tempFileName = $"{Guid.NewGuid()}.tmp";
            tempFilePath = Path.Combine(_config.TempPath, tempFileName);

            // 确保临时目录存在
            Directory.CreateDirectory(_config.TempPath);

            using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(tempStream);
            }

            // 3. 安全验证 (5层)
            var validationResult = await _securityValidator.ValidateAsync(tempFilePath, fileName);
            if (!validationResult.IsValid)
            {
                // 将文件移动到隔离区
                await QuarantineFileAsync(tempFilePath, fileName, validationResult.Errors);
                return UploadResult.Fail($"文件验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 4. 检查重复文件
            if (_config.Security.EnableDuplicateCheck)
            {
                var existingFile = await _context.TacticsFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FileHash == validationResult.FileHash);

                if (existingFile != null)
                {
                    File.Delete(tempFilePath);
                    return UploadResult.Fail($"该文件已存在，配装码: {existingFile.ShareCode}");
                }
            }

            // 5. 创建数据库记录
            var parsedFile = validationResult.ParsedFile!;
            var now = DateTime.UtcNow;

            var tacticsFile = new TacticsFileModel
            {
                UploaderId = userId,
                Name = parsedFile.Name,
                Description = parsedFile.Description,
                AuthorName = parsedFile.Author,
                ModName = parsedFile.ModName,
                TacticType = (uint)parsedFile.TacticType,
                RacePlayed = parsedFile.RacePlayed switch
                {
                    "Protoss" => "P",
                    "Terran" => "T",
                    "Zerg" => "Z",
                    _ => "Unknown"
                },
                FilePath = string.Empty, // 临时，稍后更新
                FileSize = (uint)new FileInfo(tempFilePath).Length,
                FileHash = validationResult.FileHash,
                Status = "pending", // 所有文件都需要审核
                IsLatestVersion = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.TacticsFiles.Add(tacticsFile);
            await _context.SaveChangesAsync();

            // 6. 生成配装码
            var shareCode = ShareCodeUtil.ToShareCode(tacticsFile.Id);
            tacticsFile.ShareCode = shareCode;

            // 7. 移动文件到正式存储目录
            var finalFilePath = GetStoragePath(shareCode, 1);
            Directory.CreateDirectory(Path.GetDirectoryName(finalFilePath)!);
            File.Move(tempFilePath, finalFilePath);

            tacticsFile.FilePath = finalFilePath;
            await _context.SaveChangesAsync();

            // 8. 创建版本记录
            var version = new TacticsFileVersionModel
            {
                FileId = tacticsFile.Id,
                VersionNumber = 1,
                TacVersion = (uint)parsedFile.TacVersion,
                FilePath = finalFilePath,
                FileSize = tacticsFile.FileSize,
                FileHash = validationResult.FileHash,
                Changelog = changelog ?? "初始版本",
                CreatedAt = now
            };

            _context.TacticsFileVersions.Add(version);
            await _context.SaveChangesAsync();

            tacticsFile.LatestVersionId = version.Id;
            await _context.SaveChangesAsync();

            // 9. 更新用户上传计数
            await UpdateUserUploadCount(userId);

            _logger.LogInformation(
                "用户 {UserId} 上传了战术文件，配装码: {ShareCode}",
                userId, shareCode);

            return UploadResult.Success(shareCode, tacticsFile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件时发生异常");

            // 清理临时文件
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return UploadResult.Fail($"上传失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadVersionAsync(long userId, string shareCode, Stream fileStream, string? changelog = null)
    {
        var tempFilePath = string.Empty;

        try
        {
            // 1. 查找原文件
            var existingFile = await _context.TacticsFiles
                .FirstOrDefaultAsync(f => f.ShareCode == shareCode);

            if (existingFile == null)
            {
                return UploadResult.Fail("战术文件不存在");
            }

            // 2. 权限检查
            var permissionResult = await _permissionService.CanUploadVersion(userId, existingFile.Id);
            if (!permissionResult.Success)
            {
                return UploadResult.Fail(permissionResult.ErrorMessage);
            }

            // 3. 保存到临时文件
            var tempFileName = $"{Guid.NewGuid()}.tmp";
            tempFilePath = Path.Combine(_config.TempPath, tempFileName);

            Directory.CreateDirectory(_config.TempPath);

            using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(tempStream);
            }

            // 4. 安全验证
            var validationResult = await _securityValidator.ValidateAsync(tempFilePath, "version.tactix");
            if (!validationResult.IsValid)
            {
                await QuarantineFileAsync(tempFilePath, "version.tactix", validationResult.Errors);
                return UploadResult.Fail($"文件验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 5. 检查文件是否与原版本相同
            if (validationResult.FileHash == existingFile.FileHash)
            {
                File.Delete(tempFilePath);
                return UploadResult.Fail("新版本与当前版本内容相同");
            }

            // 6. 获取下一个版本号
            var lastVersion = await _context.TacticsFileVersions
                .Where(v => v.FileId == existingFile.Id)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            var newVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;
            var parsedFile = validationResult.ParsedFile!;
            var now = DateTime.UtcNow;

            // 7. 移动文件到正式存储
            var finalFilePath = GetStoragePath(shareCode, newVersionNumber);
            Directory.CreateDirectory(Path.GetDirectoryName(finalFilePath)!);
            File.Move(tempFilePath, finalFilePath);

            // 8. 创建版本记录
            var version = new TacticsFileVersionModel
            {
                FileId = existingFile.Id,
                VersionNumber = newVersionNumber,
                TacVersion = (uint)parsedFile.TacVersion,
                FilePath = finalFilePath,
                FileSize = (uint)new FileInfo(finalFilePath).Length,
                FileHash = validationResult.FileHash,
                Changelog = changelog ?? $"版本 {newVersionNumber}",
                CreatedAt = now
            };

            _context.TacticsFileVersions.Add(version);
            await _context.SaveChangesAsync();

            // 9. 更新主文件记录
            existingFile.LatestVersionId = version.Id;
            existingFile.FilePath = finalFilePath;
            existingFile.FileSize = version.FileSize;
            existingFile.FileHash = version.FileHash;
            existingFile.Status = "pending"; // 新版本也需要审核
            existingFile.UpdatedAt = now;

            await _context.SaveChangesAsync();

            // 10. 更新用户上传计数
            await UpdateUserUploadCount(userId);

            _logger.LogInformation(
                "用户 {UserId} 上传了战术文件 {ShareCode} 的新版本 {Version}",
                userId, shareCode, newVersionNumber);

            return UploadResult.Success(shareCode, existingFile.Id, newVersionNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传版本时发生异常");

            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return UploadResult.Fail($"上传失败: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TacticsFileModel?> GetFileByShareCodeAsync(string shareCode)
    {
        return await _context.TacticsFiles
            .AsNoTracking()
            .Include(f => f.Uploader)
            .FirstOrDefaultAsync(f => f.ShareCode == shareCode && f.Status == "approved");
    }

    /// <inheritdoc />
    public async Task<List<TacticsFileVersionModel>> GetFileVersionsAsync(string shareCode)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ShareCode == shareCode);

        if (file == null)
            return new List<TacticsFileVersionModel>();

        return await _context.TacticsFileVersions
            .AsNoTracking()
            .Where(v => v.FileId == file.Id && v.IsDeleted == 0)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FileDownloadResult?> DownloadFileAsync(string shareCode, int? versionNumber = null)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ShareCode == shareCode);

        if (file == null)
            return null;

        // 只有审核通过的才能下载（管理员除外，但这里暂不处理）
        if (file.Status != "approved")
            return null;

        string filePath;
        int actualVersionNumber;

        if (versionNumber.HasValue)
        {
            var version = await _context.TacticsFileVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.FileId == file.Id && v.VersionNumber == versionNumber.Value && v.IsDeleted == 0);

            if (version == null)
                return null;

            filePath = version.FilePath;
            actualVersionNumber = version.VersionNumber;
        }
        else
        {
            filePath = file.FilePath;
            actualVersionNumber = 1; // 默认最新版本
        }

        if (!File.Exists(filePath))
            return null;

        // 更新下载计数
        await IncrementDownloadCount(file.Id);

        return new FileDownloadResult
        {
            FilePath = filePath,
            FileName = $"{shareCode}_v{actualVersionNumber}.tactix",
            ContentType = "application/json"
        };
    }

    /// <inheritdoc />
    public async Task<DeleteResult> DeleteFileAsync(long userId, string shareCode)
    {
        var file = await _context.TacticsFiles
            .FirstOrDefaultAsync(f => f.ShareCode == shareCode);

        if (file == null)
            return DeleteResult.Fail("战术文件不存在");

        // 权限检查
        var permissionResult = await _permissionService.CanDeleteOwnFile(userId, file.Id);
        if (!permissionResult.Success)
        {
            return DeleteResult.Fail(permissionResult.ErrorMessage);
        }

        // 软删除所有版本
        var versions = await _context.TacticsFileVersions
            .Where(v => v.FileId == file.Id)
            .ToListAsync();

        foreach (var version in versions)
        {
            version.IsDeleted = 1;

            // 物理删除文件（可选，这里选择保留）
            // if (File.Exists(version.FilePath))
            //     File.Delete(version.FilePath);
        }

        // 从数据库中移除主记录
        _context.TacticsFiles.Remove(file);
        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {UserId} 删除了战术文件 {ShareCode}", userId, shareCode);

        return DeleteResult.Success();
    }

    /// <summary>
    /// 获取存储路径
    /// </summary>
    private string GetStoragePath(string shareCode, int version)
    {
        var now = DateTime.UtcNow;
        var fileName = $"{shareCode}_v{version}.tactix";

        return Path.Combine(
            _config.StoragePath,
            now.Year.ToString(),
            now.Month.ToString("D2"),
            now.Day.ToString("D2"),
            fileName);
    }

    /// <summary>
    /// 将文件移动到隔离区
    /// </summary>
    private async Task QuarantineFileAsync(string tempFilePath, string originalFileName, List<string> errors)
    {
        try
        {
            Directory.CreateDirectory(_config.QuarantinePath);

            var quarantineFileName = $"{Guid.NewGuid()}_{originalFileName}";
            var quarantinePath = Path.Combine(_config.QuarantinePath, quarantineFileName);

            File.Move(tempFilePath, quarantinePath);

            // 记录隔离信息
            var infoPath = $"{quarantinePath}.info.txt";
            await File.WriteAllTextAsync(infoPath,
                $"OriginalName: {originalFileName}\n" +
                $"QuarantineTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                $"Errors: {string.Join(", ", errors)}");

            _logger.LogWarning("文件已隔离: {FileName} -> {QuarantinePath}", originalFileName, quarantinePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "隔离文件失败");
            // 如果隔离失败，直接删除
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 更新用户上传计数
    /// </summary>
    private async Task UpdateUserUploadCount(long userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.UploadCount++;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 增加下载计数
    /// </summary>
    private async Task IncrementDownloadCount(long fileId)
    {
        var file = await _context.TacticsFiles.FindAsync(fileId);
        if (file != null)
        {
            file.DownloadCount++;
            await _context.SaveChangesAsync();
        }
    }
}

/// <summary>
/// 上传结果
/// </summary>
public class UploadResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public long FileId { get; set; }
    public int VersionNumber { get; set; }

    public static UploadResult Success(string shareCode, long fileId, int versionNumber = 1) => new()
    {
        Success = true,
        ShareCode = shareCode,
        FileId = fileId,
        VersionNumber = versionNumber
    };

    public static UploadResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// 文件下载结果
/// </summary>
public class FileDownloadResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// 删除结果
/// </summary>
public class DeleteResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static DeleteResult Success() => new() { Success = true };
    public static DeleteResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
