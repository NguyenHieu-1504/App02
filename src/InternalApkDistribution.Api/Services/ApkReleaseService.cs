using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Entities;
using InternalApkDistribution.Core.Interfaces;

namespace InternalApkDistribution.Api.Services;

public sealed class ApkReleaseService : IApkReleaseService
{
    private readonly IApkMetadataReader _metadataReader;
    private readonly IApkReleaseRepository _repository;
    private readonly IApkFileStorage _storage;

    public ApkReleaseService(IApkMetadataReader metadataReader, IApkReleaseRepository repository, IApkFileStorage storage)
    {
        _metadataReader = metadataReader;
        _repository = repository;
        _storage = storage;
    }

    public async Task<ApkRelease> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new ArgumentException("Vui lòng chọn file APK.");

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".apk", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chỉ chấp nhận file .apk.");

        ApkMetadataDto metadata;
        await using (var stream = file.OpenReadStream())
        {
            metadata = await _metadataReader.ReadFromStreamAsync(stream, cancellationToken);
        }

        if (await _repository.ExistsByPackageAndVersionAsync(metadata.PackageName, metadata.VersionCode, cancellationToken))
            throw new InvalidOperationException("Version already exists.");

        var fileName = $"{metadata.AppName}-{metadata.VersionName}.apk";
        if (_storage.Exists(metadata.PackageName, metadata.VersionCode, fileName))
        {
            // Handle orphan files when DB was reset but APK file remains on disk.
            var stalePath = _storage.GetFilePath(metadata.PackageName, metadata.VersionCode, fileName);
            await _storage.DeleteByPathAsync(stalePath, cancellationToken);
            if (_storage.Exists(metadata.PackageName, metadata.VersionCode, fileName))
                throw new InvalidOperationException("File already exists.");
        }

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

        try
        {
            await _repository.InsertAsync(release, cancellationToken);
            return release;
        }
        catch
        {
            await _storage.DeleteByPathAsync(savedPath, cancellationToken);
            throw;
        }
    }

    public Task<ApkRelease?> GetAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
        => _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);

    public async Task<(Stream stream, string downloadName)> OpenDownloadAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
    {
        var release = await _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (release == null)
            throw new FileNotFoundException("Không tìm thấy phiên bản.");

        var stream = await _storage.OpenReadByPathAsync(release.FilePath, cancellationToken);
        if (stream == null)
            throw new FileNotFoundException("File không tồn tại trên đĩa.");

        var downloadName = $"{release.AppName}-{release.VersionName}.apk";
        return (stream, downloadName);
    }

    public async Task<bool> DeleteAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
    {
        var release = await _repository.GetByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (release == null)
            return false;

        var deleted = await _repository.DeleteByPackageAndVersionAsync(packageName, versionCode, cancellationToken);
        if (!deleted)
            return false;

        await _storage.DeleteByPathAsync(release.FilePath, cancellationToken);
        return true;
    }
}

