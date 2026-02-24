using InternalApkDistribution.Core.DTOs;

namespace InternalApkDistribution.Core.Interfaces;

/// <summary>
/// Đọc metadata từ file APK (packageName, versionCode, versionName, appName).
/// Implementation dùng ApkNet hoặc aapt, không hard-code giá trị.
/// </summary>
public interface IApkMetadataReader
{
    /// <summary>
    /// Trích xuất metadata từ stream file APK.
    /// </summary>
    /// <param name="apkStream">Stream đọc file APK (phải seekable hoặc copy ra temp).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Metadata hoặc throw nếu file không hợp lệ.</returns>
    Task<ApkMetadataDto> ReadFromStreamAsync(Stream apkStream, CancellationToken cancellationToken = default);
}
