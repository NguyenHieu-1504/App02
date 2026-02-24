namespace InternalApkDistribution.Core.DTOs;

/// <summary>
/// Thông tin trích xuất từ file APK (manifest).
/// </summary>
public class ApkMetadataDto
{
    public string AppName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int VersionCode { get; set; }
    public string VersionName { get; set; } = string.Empty;
}
