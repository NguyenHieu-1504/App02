using InternalApkDistribution.Core.Entities;

namespace InternalApkDistribution.Api.Services;

public interface IApkReleaseService
{
    Task<ApkRelease> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<ApkRelease?> GetAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
    Task<(Stream stream, string downloadName)> OpenDownloadAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
}

