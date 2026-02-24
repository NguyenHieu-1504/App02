# Hệ thống quản lý và phân phối APK nội bộ (Internal App Distribution)

Hệ thống web nội bộ cho phép quản lý file APK, lưu nhiều phiên bản ứng dụng Android và cung cấp link tải cho cài đặt/test/triển khai nội bộ.

## Yêu cầu

- .NET 8 SDK
- MongoDB (chạy local hoặc remote)
- Windows/Linux/macOS

## Cấu hình

Chỉnh trong `src/InternalApkDistribution.Api/appsettings.json` (hoặc `appsettings.Development.json`):

- **ConnectionStrings:MongoDb** – Chuỗi kết nối MongoDB (mặc định: `mongodb://localhost:27017`).
- **MongoDb:DatabaseName** – Tên database (mặc định: `InternalApkDistribution`).
- **ApkStorage:BasePath** – Thư mục gốc lưu file APK (ví dụ: `C:\data\apk` hoặc `/data/apk`). Trên Windows dev có thể dùng `./data/apk`.

## Chạy ứng dụng

1. Đảm bảo MongoDB đang chạy.
2. Tạo thư mục lưu APK nếu cần (hoặc dùng `./data/apk` trong Development).
3. Chạy API:

```bash
cd src/InternalApkDistribution.Api
dotnet run
```

4. Mở trình duyệt:
   - Giao diện web: http://localhost:5000 (hoặc port được in ra)
   - Swagger: http://localhost:5000/swagger

## Cấu trúc thư mục APK trên đĩa

```
{BasePath}/
└── {packageName}/
    └── {versionCode}/
        └── {appName}-{versionName}.apk
```

Ví dụ: `C:\data\apk\vn.kztek.iparking.pos\1023\iParking POS-1.2.3.apk`

## API chính

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/apk/upload` | Upload file APK (form-data, field: `file`) |
| GET | `/api/apps` | Danh sách ứng dụng (nhóm theo package) |
| GET | `/api/apps/{packageName}/versions?sort=versionCode&order=desc` | Danh sách version của một app |
| GET | `/api/apk/download/{packageName}/{versionCode}` | Tải file APK |
| GET | `/api/apk/{packageName}/{versionCode}` | Lấy metadata một phiên bản |

## Giao diện web

- **Trang chủ** (`/index.html`) – Giới thiệu và điều hướng.
- **Upload APK** (`/upload.html`) – Form chọn file .apk và tải lên.
- **Danh sách ứng dụng** (`/apps.html`) – Bảng app, link “Xem phiên bản”.
- **Phiên bản** (`/versions.html?package=...`) – Bảng version, sắp xếp, nút tải APK.

## Tài liệu chi tiết

Xem [docs/PHAN-TICH-YEU-CAU-VA-PHUONG-AN.md](docs/PHAN-TICH-YEU-CAU-VA-PHUONG-AN.md) để biết phân tích yêu cầu, thiết kế MongoDB, REST API và hướng triển khai.
