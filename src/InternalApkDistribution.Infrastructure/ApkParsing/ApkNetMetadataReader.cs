using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Interfaces;
using ApkReaderLib = ApkReader;

namespace InternalApkDistribution.Infrastructure.ApkParsing;

/// <summary>
/// Read APK metadata via ApkReader (manifest/resources parsing handled by package).
/// </summary>
public sealed class ApkNetMetadataReader : IApkMetadataReader
{
    public async Task<ApkMetadataDto> ReadFromStreamAsync(Stream apkStream, CancellationToken cancellationToken = default)
    {
        await using var seekable = apkStream.CanSeek ? apkStream : await CopyToMemoryAsync(apkStream, cancellationToken);
        seekable.Position = 0;

        var apkReader = new ApkReaderLib.ApkReader<ApkReaderLib.ApkInfo>();
        var info = apkReader.Read(seekable);

        if (info == null || string.IsNullOrWhiteSpace(info.PackageName))
            throw new InvalidOperationException("Invalid APK: could not read package name from manifest.");

        var appName = !string.IsNullOrWhiteSpace(info.Label) ? info.Label.Trim() : info.PackageName;
        if (!int.TryParse(info.VersionCode, out var versionCode))
            throw new InvalidOperationException("Invalid APK: could not read versionCode from manifest.");

        return new ApkMetadataDto
        {
            AppName = SanitizeFileName(appName),
            PackageName = info.PackageName,
            VersionCode = versionCode,
            VersionName = info.VersionName ?? info.VersionCode ?? versionCode.ToString()
        };
    }

    private static async Task<MemoryStream> CopyToMemoryAsync(Stream source, CancellationToken ct)
    {
        var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(sanitized) ? "App" : sanitized;
    }
}
