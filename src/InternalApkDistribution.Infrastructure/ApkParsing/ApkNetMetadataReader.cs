using System.IO.Compression;
using InternalApkDistribution.Core.DTOs;
using InternalApkDistribution.Core.Interfaces;
using Iteedee.ApkReader;

namespace InternalApkDistribution.Infrastructure.ApkParsing;

/// <summary>
/// Đọc metadata từ APK bằng thư viện Iteedee.ApkReader (parse AndroidManifest.xml binary).
/// Không hard-code thông tin version; tất cả lấy từ file.
/// </summary>
public sealed class ApkNetMetadataReader : IApkMetadataReader
{
    public async Task<ApkMetadataDto> ReadFromStreamAsync(Stream apkStream, CancellationToken cancellationToken = default)
    {
        // APK là file ZIP; cần copy ra MemoryStream nếu không seekable để ZipArchive đọc được
        await using var seekable = apkStream.CanSeek ? apkStream : await CopyToMemoryAsync(apkStream, cancellationToken);

        using var zip = new ZipArchive(seekable, ZipArchiveMode.Read, leaveOpen: !apkStream.CanSeek);

        var manifestEntry = zip.GetEntry("AndroidManifest.xml") ?? zip.GetEntry("androidmanifest.xml");
        if (manifestEntry == null)
            throw new InvalidOperationException("Invalid APK: AndroidManifest.xml not found.");

        byte[] manifestData;
        await using (var manifestStream = manifestEntry.Open())
        {
            using var ms = new MemoryStream();
            await manifestStream.CopyToAsync(ms, cancellationToken);
            manifestData = ms.ToArray();
        }

        // Resources.arsc có thể cần cho ApkReader để resolve application label
        byte[]? resourcesData = null;
        var resEntry = zip.GetEntry("resources.arsc") ?? zip.GetEntry("Resources.arsc");
        if (resEntry != null)
        {
            await using var resStream = resEntry.Open();
            using var resMs = new MemoryStream();
            await resStream.CopyToAsync(resMs, cancellationToken);
            resourcesData = resMs.ToArray();
        }

        var apkReader = new ApkReader();
        var info = apkReader.extractInfo(manifestData, resourcesData ?? Array.Empty<byte>());

        if (info == null || string.IsNullOrWhiteSpace(info.packageName))
            throw new InvalidOperationException("Invalid APK: could not read package name from manifest.");

        var appName = !string.IsNullOrWhiteSpace(info.label) ? info.label!.Trim() : info.packageName;
        if (!int.TryParse(info.versionCode, out var versionCode))
            throw new InvalidOperationException("Invalid APK: could not read versionCode from manifest.");

        return new ApkMetadataDto
        {
            AppName = SanitizeFileName(appName),
            PackageName = info.packageName,
            VersionCode = versionCode,
            VersionName = info.versionName ?? info.versionCode ?? versionCode.ToString()
        };
    }

    private static async Task<MemoryStream> CopyToMemoryAsync(Stream source, CancellationToken ct)
    {
        var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Loại bỏ ký tự không hợp lệ cho tên file trên đĩa.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(sanitized) ? "App" : sanitized;
    }
}
