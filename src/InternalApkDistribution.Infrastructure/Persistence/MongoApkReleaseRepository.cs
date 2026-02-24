using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Entities;
using InternalApkDistribution.Core.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InternalApkDistribution.Infrastructure.Persistence;

public sealed class MongoApkReleaseRepository : IApkReleaseRepository
{
    private readonly IMongoCollection<ApkReleaseDocument> _collection;

    public MongoApkReleaseRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ApkReleaseDocument>("apk_releases");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var indexKeys = Builders<ApkReleaseDocument>.IndexKeys
            .Ascending(x => x.PackageName)
            .Ascending(x => x.VersionCode);
        var indexModel = new CreateIndexModel<ApkReleaseDocument>(indexKeys, new CreateIndexOptions { Unique = true });
        try { _collection.Indexes.CreateOne(indexModel); } catch { /* index may already exist */ }
    }

    public async Task<ApkRelease?> FindByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
    {
        var doc = await _collection.Find(Filter(packageName, versionCode)).FirstOrDefaultAsync(cancellationToken);
        return doc == null ? null : ToEntity(doc);
    }

    public async Task<bool> ExistsByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(Filter(packageName, versionCode), cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task<ApkRelease> InsertAsync(ApkRelease release, CancellationToken cancellationToken = default)
    {
        var doc = ToDocument(release);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        release.Id = doc.Id;
        return release;
    }

    public async Task<IReadOnlyList<ApkRelease>> GetVersionsAsync(string packageName, string sortBy = "versionCode", bool descending = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ApkReleaseDocument>.Filter.Eq(x => x.PackageName, packageName);
        var sort = sortBy.Equals("uploadedAt", StringComparison.OrdinalIgnoreCase)
            ? (descending ? Builders<ApkReleaseDocument>.Sort.Descending(x => x.UploadedAt) : Builders<ApkReleaseDocument>.Sort.Ascending(x => x.UploadedAt))
            : (descending ? Builders<ApkReleaseDocument>.Sort.Descending(x => x.VersionCode) : Builders<ApkReleaseDocument>.Sort.Ascending(x => x.VersionCode));

        var list = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
        return list.Select(ToEntity).ToList();
    }

    public async Task<IReadOnlyList<AppSummaryDto>> GetAppSummariesAsync(CancellationToken cancellationToken = default)
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$packageName" },
                { "appName", new BsonDocument("$first", "$appName") },
                { "count", new BsonDocument("$sum", 1) },
                { "latestVersionCode", new BsonDocument("$max", "$versionCode") },
                { "lastUploadedAt", new BsonDocument("$max", "$uploadedAt") }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "packageName", "$_id" },
                { "appName", 1 },
                { "versionCount", "$count" },
                { "latestVersionCode", 1 },
                { "lastUploadedAt", 1 },
                { "_id", 0 }
            })
        };

        var cursor = await _collection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
        var groups = await cursor.ToListAsync(cancellationToken);

        var result = new List<AppSummaryDto>();
        foreach (var g in groups)
        {
            var packageName = g["packageName"].AsString;
            var latestCode = g.GetValue("latestVersionCode", BsonNull.Value);
            var latestDoc = latestCode != BsonNull.Value
                ? await _collection.Find(Filter(packageName, latestCode.AsInt32)).FirstOrDefaultAsync(cancellationToken)
                : null;
            result.Add(new AppSummaryDto
            {
                PackageName = packageName,
                AppName = g.GetValue("appName", "?").AsString,
                VersionCount = g.GetValue("versionCount", 0).AsInt32,
                LatestVersionCode = latestCode != BsonNull.Value ? latestCode.AsInt32 : null,
                LatestVersionName = latestDoc?.VersionName,
                LastUploadedAt = g.Contains("lastUploadedAt") ? g["lastUploadedAt"].ToUniversalTime() : null
            });
        }
        return result;
    }

    public async Task<ApkRelease?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var doc = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return doc == null ? null : ToEntity(doc);
    }

    public async Task<ApkRelease?> GetByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
        => await FindByPackageAndVersionAsync(packageName, versionCode, cancellationToken);

    public async Task<bool> DeleteByPackageAndVersionAsync(string packageName, int versionCode, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(Filter(packageName, versionCode), cancellationToken);
        return result.DeletedCount > 0;
    }

    private static FilterDefinition<ApkReleaseDocument> Filter(string packageName, int versionCode)
        => Builders<ApkReleaseDocument>.Filter.And(
            Builders<ApkReleaseDocument>.Filter.Eq(x => x.PackageName, packageName),
            Builders<ApkReleaseDocument>.Filter.Eq(x => x.VersionCode, versionCode));

    private static ApkRelease ToEntity(ApkReleaseDocument d)
        => new()
        {
            Id = d.Id,
            AppName = d.AppName,
            PackageName = d.PackageName,
            VersionCode = d.VersionCode,
            VersionName = d.VersionName,
            UploadedAt = d.UploadedAt,
            FilePath = d.FilePath,
            FileSizeBytes = d.FileSizeBytes,
            OriginalFileName = d.OriginalFileName
        };

    private static ApkReleaseDocument ToDocument(ApkRelease e)
        => new()
        {
            Id = string.IsNullOrEmpty(e.Id) ? ObjectId.GenerateNewId().ToString() : e.Id,
            AppName = e.AppName,
            PackageName = e.PackageName,
            VersionCode = e.VersionCode,
            VersionName = e.VersionName,
            UploadedAt = e.UploadedAt,
            FilePath = e.FilePath,
            FileSizeBytes = e.FileSizeBytes,
            OriginalFileName = e.OriginalFileName
        };
}
