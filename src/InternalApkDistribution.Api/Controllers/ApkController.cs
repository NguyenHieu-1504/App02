using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Entities;
using InternalApkDistribution.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternalApkDistribution.Api.Controllers;

[ApiController]
[Route("api/apk")]
public class ApkController : ControllerBase
{
    private readonly IApkMetadataReader _metadataReader;
    private readonly IApkReleaseRepository _repository;
    private readonly IApkFileStorage _storage;

    public ApkController(IApkMetadataReader metadataReader, IApkReleaseRepository repository, IApkFileStorage storage)
    {
        _metadataReader = metadataReader;
        _repository = repository;
        _storage = storage;
    }

    /// <summary>
    /// Upload file APK. Kiểm tra định dạng, trích xuất metadata, không cho phép trùng packageName + versionCode.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(500_000_000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<ActionResult<ApkRelease>> Upload([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Vui lòng chọn file APK." });

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".apk", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Chỉ chấp nhận file .apk." });

        ApkMetadataDto metadata;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                metadata = await _metadataReader.ReadFromStreamAsync(stream, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        if (await _repository.ExistsByPackageAndVersionAsync(metadata.PackageName, metadata.VersionCode, cancellationToken))
            return Conflict(new { error = "Phiên bản này đã tồn tại (trùng packageName + versionCode)." });

        var fileName = $"{metadata.AppName}-{metadata.VersionName}.apk";
        if (_storage.Exists(metadata.PackageName, metadata.VersionCode, fileName))
            return Conflict(new { error = "File đã tồn tại trên đĩa. Không ghi đè." });

        string savedPath;
        await using (var uploadStream = file.OpenReadStream())
        {
            savedPath = await _storage.SaveAsync(uploadStream, metadata.PackageName, metadata.VersionCode, fileName, cancellationToken);
        }

        var release = new ApkRelease
        {
            AppName = metadata.AppName,
            PackageName = metadata.PackageName,
            VersionCode = metadata.VersionCode,
            VersionName = metadata.VersionName,
            UploadedAt = DateTime.UtcNow,
            FilePath = savedPath,
            FileSizeBytes = file.Length,
            OriginalFileName = file.FileName
        };
        await _repository.InsertAsync(release, cancellationToken);

        return CreatedAtAction(nameof(GetByPackageAndVersion), new { packageName = release.PackageName, versionCode = release.VersionCode }, release);
    }

    /// <summary>
    /// Lấy metadata một phiên bản.
    /// </summary>
    [HttpGet("{packageName}/{versionCode:int}")]
    public async Task<ActionResult<ApkRelease>> GetByPackageAndVersion(string packageName, int versionCode, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (release == null)
            return NotFound(new { error = "Không tìm thấy phiên bản." });
        return Ok(release);
    }

    /// <summary>
    /// Tải file APK.
    /// </summary>
    [HttpGet("download/{packageName}/{versionCode:int}")]
    public async Task<IActionResult> Download(string packageName, int versionCode, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (release == null)
            return NotFound(new { error = "Không tìm thấy phiên bản." });

        var stream = await _storage.OpenReadByPathAsync(release.FilePath, cancellationToken);
        if (stream == null)
            return NotFound(new { error = "File không tồn tại trên đĩa." });

        var downloadName = $"{release.AppName}-{release.VersionName}.apk";
        return File(stream, "application/vnd.android.package-archive", downloadName);
    }

    /// <summary>
    /// Xóa một phiên bản APK (metadata + file trên đĩa nếu có).
    /// </summary>
    [HttpDelete("{packageName}/{versionCode:int}")]
    public async Task<IActionResult> Delete(string packageName, int versionCode, CancellationToken cancellationToken)
    {
        var release = await _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (release == null)
            return NotFound(new { error = "Không tìm thấy phiên bản để xóa." });

        // Xóa document trước, sau đó cố gắng xóa file (không hard-fail nếu lỗi IO)
        var deleted = await _repository.DeleteByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (!deleted)
            return NotFound(new { error = "Không tìm thấy phiên bản để xóa." });

        await _storage.DeleteByPathAsync(release.FilePath, cancellationToken);

        return NoContent();
    }
}
