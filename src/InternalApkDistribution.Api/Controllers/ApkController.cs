using InternalApkDistribution.Core.Entities;
using InternalApkDistribution.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternalApkDistribution.Api.Controllers;

[ApiController]
[Route("api/apk")]
public class ApkController : ControllerBase
{
    private readonly IApkReleaseService _service;

    public ApkController(IApkReleaseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Upload file APK. Kiểm tra định dạng, trích xuất metadata, không cho phép trùng packageName + versionCode.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(500_000_000)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<ActionResult<ApkRelease>> Upload([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null)
            return BadRequest(new { error = "Vui lòng chọn file APK." });

        var release = await _service.UploadAsync(file, cancellationToken);
        return CreatedAtAction(nameof(GetByPackageAndVersion), new { packageName = release.PackageName, versionCode = release.VersionCode }, release);
    }

    /// <summary>
    /// Lấy metadata một phiên bản.
    /// </summary>
    [HttpGet("{packageName}/{versionCode:int}")]
    public async Task<ActionResult<ApkRelease>> GetByPackageAndVersion(string packageName, int versionCode, CancellationToken cancellationToken)
    {
        var release = await _service.GetAsync(packageName, versionCode, cancellationToken);
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
        var (stream, downloadName) = await _service.OpenDownloadAsync(packageName, versionCode, cancellationToken);
        return File(stream, "application/vnd.android.package-archive", downloadName);
    }

    /// <summary>
    /// Xóa một phiên bản APK (metadata + file trên đĩa nếu có).
    /// </summary>
    [HttpDelete("{packageName}/{versionCode:int}")]
    public async Task<IActionResult> Delete(string packageName, int versionCode, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(packageName, versionCode, cancellationToken);
        if (!deleted)
            return NotFound(new { error = "Không tìm thấy phiên bản để xóa." });
        return NoContent();
    }
}
