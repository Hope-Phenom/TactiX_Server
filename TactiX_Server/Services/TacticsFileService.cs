using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TactiX_Server.Data;
using TactiX_Server.Models.Config;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 上传结果
/// </summary>
public class UploadResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ShareCode { get; set; }
    public long FileId { get; set; }
    public int VersionNumber { get; set; }

    public static UploadResult SucceededResult(string shareCode, long fileId, int versionNumber) =>
        new() { Succeeded = true, ShareCode = shareCode, FileId = fileId, VersionNumber = versionNumber };
    public static UploadResult Failed(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 文件下载结果
/// </summary>
public class FileDownloadResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// 删除结果
/// </summary>
public class DeleteResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static DeleteResult SucceededResult() => new() { Succeeded = true };
    public static DeleteResult Failed(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 战术文件服务接口
/// </summary>
public interface ITacticsFileService
{
    /// <summary>上传战术文件</summary>
    Task<UploadResult> UploadFileAsync(long userId, Stream fileStream, string fileName, string? changelog = null);

    /// <summary>上传战术文件新版本</summary>
    Task<UploadResult> UploadVersionAsync(long userId, string shareCode, Stream fileStream, string? changelog = null);

    /// <summary>获取文件详情</summary>
    Task<TacticsFileModel?> GetFileByShareCodeAsync(string shareCode);

    /// <summary>获取文件版本列表</summary>
    Task<List<TacticsFileVersionModel>> GetFileVersionsAsync(string shareCode);

    /// <summary>下载文件</summary>
    Task<FileDownloadResult?> DownloadFileAsync(string shareCode, int? versionNumber = null);

    /// <summary>删除文件</summary>
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

    public async Task<UploadResult> UploadFileAsync(long userId, Stream fileStream, string fileName, string? changelog = null)
    {
        var tempFilePath = string.Empty;

        try
        {
            // 1. 权限检查
            var permissionResult = await _permissionService.CanUploadAsync(userId, fileStream.Length);
            if (!permissionResult.Succeeded)
            {
                return UploadResult.Failed(permissionResult.ErrorMessage!);
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
            var fileContent = await File.ReadAllBytesAsync(tempFilePath);
            var validationResult = await _securityValidator.ValidateAsync(fileContent, fileName);
            if (!validationResult.IsValid)
            {
                // 将文件移动到隔离区
                await QuarantineFileAsync(tempFilePath, fileName, validationResult.Errors);
                return UploadResult.Failed($"文件验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 4. 检查重复文件
            if (_config.Security.EnableDuplicateCheck)
            {
                var existingFile = await _context.TacticsFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FileHash == validationResult.FileHash && !f.IsDeleted);

                if (existingFile != null)
                {
                    File.Delete(tempFilePath);
                    return UploadResult.Failed($"该文件已存在，配装码: {existingFile.ShareCode}");
                }
            }

            // 5. 创建数据库记录
            var now = DateTime.UtcNow;

            var tacticsFile = new TacticsFileModel
            {
                UploaderId = userId,
                Name = validationResult.Name,
                Author = validationResult.Author,
                Race = validationResult.DetectedRace,
                FilePath = string.Empty, // 临时，稍后更新
                FileHash = validationResult.FileHash!,
                FileSize = new FileInfo(tempFilePath).Length,
                Version = 1,
                Status = FileStatus.Pending,
                IsPublic = true,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.TacticsFiles.Add(tacticsFile);
            await _context.SaveChangesAsync();

            // 6. 生成配装码
            var shareCode = ShareCodeUtil.Encode(tacticsFile.Id);
            if (shareCode == null)
            {
                // ID超出范围，删除记录
                _context.TacticsFiles.Remove(tacticsFile);
                await _context.SaveChangesAsync();
                File.Delete(tempFilePath);
                return UploadResult.Failed("文件ID超出配装码范围");
            }
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
                FilePath = finalFilePath,
                FileHash = validationResult.FileHash!,
                FileSize = tacticsFile.FileSize,
                Changelog = changelog ?? "初始版本",
                CreatedAt = now
            };

            _context.TacticsFileVersions.Add(version);
            await _context.SaveChangesAsync();

            tacticsFile.LatestVersionId = version.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "用户 {UserId} 上传了战术文件，配装码: {ShareCode}",
                userId, shareCode);

            return UploadResult.SucceededResult(shareCode, tacticsFile.Id, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件时发生异常");

            // 清理临时文件
            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return UploadResult.Failed("上传失败，请稍后重试");
        }
    }

    public async Task<UploadResult> UploadVersionAsync(long userId, string shareCode, Stream fileStream, string? changelog = null)
    {
        var tempFilePath = string.Empty;

        try
        {
            // 1. 解析配装码获取文件ID
            var fileId = ShareCodeUtil.Decode(shareCode);
            if (fileId == null)
            {
                return UploadResult.Failed("无效的配装码格式");
            }

            // 2. 权限检查
            var permissionResult = await _permissionService.CanUploadVersionAsync(userId, fileId.Value);
            if (!permissionResult.Succeeded)
            {
                return UploadResult.Failed(permissionResult.ErrorMessage!);
            }

            // 3. 获取原文件
            var existingFile = await _context.TacticsFiles
                .FirstOrDefaultAsync(f => f.Id == fileId.Value && !f.IsDeleted);

            if (existingFile == null)
            {
                return UploadResult.Failed("战术文件不存在");
            }

            // 4. 保存到临时文件
            var tempFileName = $"{Guid.NewGuid()}.tmp";
            tempFilePath = Path.Combine(_config.TempPath, tempFileName);

            Directory.CreateDirectory(_config.TempPath);

            using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(tempStream);
            }

            // 5. 安全验证
            var fileContent = await File.ReadAllBytesAsync(tempFilePath);
            var validationResult = await _securityValidator.ValidateAsync(fileContent, shareCode + ".tactix");
            if (!validationResult.IsValid)
            {
                await QuarantineFileAsync(tempFilePath, shareCode + ".tactix", validationResult.Errors);
                return UploadResult.Failed($"文件验证失败: {string.Join(", ", validationResult.Errors)}");
            }

            // 6. 获取当前最大版本号
            var currentMaxVersion = await _context.TacticsFileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId.Value)
                .MaxAsync(v => v.VersionNumber);

            var newVersionNumber = currentMaxVersion + 1;

            // 7. 移动文件到正式存储目录
            var finalFilePath = GetStoragePath(shareCode, newVersionNumber);
            Directory.CreateDirectory(Path.GetDirectoryName(finalFilePath)!);
            File.Move(tempFilePath, finalFilePath);

            // 8. 创建版本记录
            var now = DateTime.UtcNow;
            var version = new TacticsFileVersionModel
            {
                FileId = fileId.Value,
                VersionNumber = newVersionNumber,
                FilePath = finalFilePath,
                FileHash = validationResult.FileHash!,
                FileSize = new FileInfo(finalFilePath).Length,
                Changelog = changelog ?? $"版本 {newVersionNumber}",
                CreatedAt = now
            };

            _context.TacticsFileVersions.Add(version);
            await _context.SaveChangesAsync();

            // 9. 更新文件记录
            existingFile.LatestVersionId = version.Id;
            existingFile.Version = newVersionNumber;
            existingFile.FilePath = finalFilePath;
            existingFile.FileHash = validationResult.FileHash!;
            existingFile.FileSize = version.FileSize;
            existingFile.Name = validationResult.Name ?? existingFile.Name;
            existingFile.Author = validationResult.Author ?? existingFile.Author;
            existingFile.Race = validationResult.DetectedRace ?? existingFile.Race;
            existingFile.UpdatedAt = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "用户 {UserId} 上传了战术文件新版本，配装码: {ShareCode}, 版本: {Version}",
                userId, shareCode, newVersionNumber);

            return UploadResult.SucceededResult(shareCode, fileId.Value, newVersionNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传版本时发生异常");

            if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            return UploadResult.Failed("上传失败，请稍后重试");
        }
    }

    public async Task<TacticsFileModel?> GetFileByShareCodeAsync(string shareCode)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return null;
        }

        return await _context.TacticsFiles
            .Include(f => f.Uploader)
            .Include(f => f.LatestVersion)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId.Value && !f.IsDeleted);
    }

    public async Task<List<TacticsFileVersionModel>> GetFileVersionsAsync(string shareCode)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return new List<TacticsFileVersionModel>();
        }

        return await _context.TacticsFileVersions
            .AsNoTracking()
            .Where(v => v.FileId == fileId.Value)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<FileDownloadResult?> DownloadFileAsync(string shareCode, int? versionNumber = null)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return null;
        }

        var file = await _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Id == fileId.Value && !f.IsDeleted && f.Status == FileStatus.Approved)
            .Select(f => new { f.FilePath, f.Id })
            .FirstOrDefaultAsync();

        if (file == null)
        {
            return null;
        }

        // 确定要下载的版本
        string filePath;
        if (versionNumber.HasValue)
        {
            var versionPath = await _context.TacticsFileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId.Value && v.VersionNumber == versionNumber.Value)
                .Select(v => v.FilePath)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(versionPath))
            {
                return null;
            }
            filePath = versionPath;
        }
        else
        {
            filePath = file.FilePath;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("文件不存在: {FilePath}", filePath);
            return null;
        }

        // 更新下载计数（使用ExecuteUpdateAsync直接更新，单条SQL语句，不阻塞）
        try
        {
            await _context.TacticsFiles
                .Where(f => f.Id == file.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.DownloadCount, f => f.DownloadCount + 1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新下载计数失败: FileId={FileId}", file.Id);
        }

        return new FileDownloadResult
        {
            FilePath = filePath,
            FileName = $"{shareCode}.tactix"
        };
    }

    public async Task<DeleteResult> DeleteFileAsync(long userId, string shareCode)
    {
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return DeleteResult.Failed("无效的配装码格式");
        }

        // 权限检查
        var permissionResult = await _permissionService.CanDeleteFileAsync(userId, fileId.Value);
        if (!permissionResult.Succeeded)
        {
            return DeleteResult.Failed(permissionResult.ErrorMessage!);
        }

        var file = await _context.TacticsFiles
            .FirstOrDefaultAsync(f => f.Id == fileId.Value && !f.IsDeleted);

        if (file == null)
        {
            return DeleteResult.Failed("战术文件不存在");
        }

        // 软删除
        file.IsDeleted = true;
        file.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "用户 {UserId} 删除了战术文件，配装码: {ShareCode}",
            userId, shareCode);

        return DeleteResult.SucceededResult();
    }

    /// <summary>
    /// 获取文件存储路径
    /// </summary>
    private string GetStoragePath(string shareCode, int versionNumber)
    {
        // 路径格式: {StoragePath}/{shareCode前2字符}/{shareCode}/v{version}.tactix
        var prefix = shareCode.Substring(0, 2);
        return Path.Combine(_config.StoragePath, prefix, shareCode, $"v{versionNumber}.tactix");
    }

    /// <summary>
    /// 将文件移动到隔离区
    /// </summary>
    private async Task QuarantineFileAsync(string tempFilePath, string fileName, List<string> errors)
    {
        try
        {
            Directory.CreateDirectory(_config.QuarantinePath);
            var quarantineFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{fileName}";
            var quarantinePath = Path.Combine(_config.QuarantinePath, quarantineFileName);
            File.Move(tempFilePath, quarantinePath);

            _logger.LogWarning(
                "文件被隔离: {FileName}, 原因: {Errors}",
                fileName, string.Join(", ", errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "隔离文件失败: {TempFilePath}", tempFilePath);
            // 确保临时文件被删除
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}