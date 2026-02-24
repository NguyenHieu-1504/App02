using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Entities;

namespace InternalApkDistribution.Core.Interfaces;

public interface IApkReleaseRepository
{
    Task<ApkRelease?> FindByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
    Task<ApkRelease> InsertAsync(ApkRelease release, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApkRelease>> GetVersionsAsync(string packageName, string sortBy = "versionCode", bool descending = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppSummaryDto>> GetAppSummariesAsync(CancellationToken cancellationToken = default);
    Task<ApkRelease?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApkRelease?> GetByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
    Task<bool> DeleteByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default);
}
