namespace InternalApkDistribution.Core.DTOs;

/// <summary>
/// Tóm tắt một ứng dụng (nhóm theo packageName) cho danh sách apps.
/// </summary>
public class AppSummaryDto
{
    public string PackageName { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public int? LatestVersionCode { get; set; }
    public string? LatestVersionName { get; set; }
    public DateTime? LastUploadedAt { get; set; }
}
