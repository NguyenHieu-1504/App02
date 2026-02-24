namespace InternalApkDistribution.Core.Entities;

/// <summary>
/// Một phiên bản APK đã upload, map với document trong MongoDB collection apk_releases.
/// </summary>
public class ApkRelease
{
    public string Id { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int VersionCode { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? OriginalFileName { get; set; }
}
