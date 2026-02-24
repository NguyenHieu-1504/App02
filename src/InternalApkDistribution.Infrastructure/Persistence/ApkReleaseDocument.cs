using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InternalApkDistribution.Infrastructure.Persistence;

/// <summary>
/// Document MongoDB collection "apk_releases".
/// </summary>
internal sealed class ApkReleaseDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("appName")]
    public string AppName { get; set; } = string.Empty;

    [BsonElement("packageName")]
    public string PackageName { get; set; } = string.Empty;

    [BsonElement("versionCode")]
    public int VersionCode { get; set; }

    [BsonElement("versionName")]
    public string VersionName { get; set; } = string.Empty;

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    [BsonElement("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [BsonElement("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [BsonElement("originalFileName")]
    public string? OriginalFileName { get; set; }
}
