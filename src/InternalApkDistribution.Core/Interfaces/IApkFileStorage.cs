namespace InternalApkDistribution.Core.Interfaces;

/// <summary>
/// Lưu và đọc file APK theo cấu trúc: BasePath / packageName / versionCode / fileName.apk
/// Không ghi đè file đã tồn tại.
/// </summary>
public interface IApkFileStorage
{
    /// <summary>
    /// Lưu stream APK vào đường dẫn chuẩn. Tạo thư mục nếu cần. Không overwrite.
    /// </summary>
    /// <param name="sourceStream">Nội dung file APK.</param>
    /// <param name="packageName">Package name của app.</param>
    /// <param name="versionCode">Version code.</param>
    /// <param name="fileName">Tên file (ví dụ: AppName-1.2.3.apk).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Đường dẫn đầy đủ file đã lưu.</returns>
    /// <exception cref="InvalidOperationException">Khi file đích đã tồn tại.</exception>
    Task<string> SaveAsync(Stream sourceStream, string packageName, int versionCode, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra file tại đường dẫn chuẩn đã tồn tại chưa.
    /// </summary>
    bool Exists(string packageName, int versionCode, string fileName);

    /// <summary>
    /// Mở stream đọc file APK theo đường dẫn đã lưu trong DB (FilePath).
    /// </summary>
    Task<Stream?> OpenReadByPathAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file theo đường dẫn (nếu tồn tại). Không throw nếu file không tồn tại.
    /// </summary>
    Task DeleteByPathAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về đường dẫn đầy đủ cho file (để ghi hoặc kiểm tra tồn tại).
    /// </summary>
    string GetFilePath(string packageName, int versionCode, string fileName);
}
