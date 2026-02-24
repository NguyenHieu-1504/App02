using InternalApkDistribution.Core.Interfaces;

namespace InternalApkDistribution.Infrastructure.Storage;

public sealed class ApkFileStorage : IApkFileStorage
{
    private readonly string _basePath;

    public ApkFileStorage(string basePath)
    {
        _basePath = Path.GetFullPath(basePath);
    }

    public string GetFilePath(string packageName, int versionCode, string fileName)
    {
        var dir = Path.Combine(_basePath, SanitizePathSegment(packageName), versionCode.ToString());
        return Path.Combine(dir, SanitizeFileName(fileName));
    }

    public bool Exists(string packageName, int versionCode, string fileName)
    {
        var path = GetFilePath(packageName, versionCode, fileName);
        return File.Exists(path);
    }

    public async Task<string> SaveAsync(Stream sourceStream, string packageName, int versionCode, string fileName, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFilePath(packageName, versionCode, fileName);
        var dir = Path.GetDirectoryName(fullPath)!;
        if (File.Exists(fullPath))
            throw new InvalidOperationException($"File already exists: {fullPath}. Cannot overwrite.");

        Directory.CreateDirectory(dir);

        await using var dest = File.Create(fullPath);
        await sourceStream.CopyToAsync(dest, cancellationToken);
        return fullPath;
    }

    public Task<Stream?> OpenReadByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        try
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(stream);
        }
        catch
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Nuốt lỗi xóa file để không làm hỏng logic xóa metadata
        }

        return Task.CompletedTask;
    }

    private static string SanitizePathSegment(string segment)
    {
        var invalid = Path.GetInvalidPathChars();
        var sanitized = string.Join("_", segment.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(sanitized) ? "unknown" : sanitized;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(sanitized) ? "app.apk" : sanitized;
    }
}
