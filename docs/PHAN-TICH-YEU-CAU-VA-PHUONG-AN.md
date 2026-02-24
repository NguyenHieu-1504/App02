# Phân tích yêu cầu và phương án kỹ thuật  
## Hệ thống quản lý và phân phối APK nội bộ (Internal App Distribution)

---

## 1. Tóm tắt yêu cầu

| Nhóm | Nội dung |
|------|----------|
| **Mục tiêu** | Web nội bộ: quản lý APK, lưu nhiều phiên bản, cung cấp link tải cho cài đặt/test/triển khai nội bộ |
| **Phạm vi** | Upload APK, lưu trữ file, metadata (DB), danh sách & tra cứu, Web UI đơn giản |
| **Backend** | C#, REST API, xử lý lỗi rõ ràng |
| **Database** | MongoDB, thiết kế collection rõ ràng |
| **APK** | Trích xuất packageName, versionCode, versionName từ file (aapt hoặc thư viện), không hard-code |

---

## 2. Phân tích chức năng

### 2.1 Upload APK

- **Input:** file `.apk` (multipart/form-data).
- **Xử lý:**
  1. Kiểm tra extension và magic number (PK/ZIP) để đảm bảo đúng định dạng APK.
  2. Dùng thư viện C# (ApkNet hoặc tương đương) đọc file APK và trích xuất:
     - `packageName`
     - `versionCode` (integer)
     - `versionName` (string)
     - `appName` (từ `application.label` trong manifest hoặc packageName nếu không có).
  3. Kiểm tra trùng: truy vấn DB theo `packageName` + `versionCode`; nếu đã tồn tại → trả lỗi 409 Conflict, không lưu file.
- **Output:** 201 Created + metadata của bản ghi vừa tạo, hoặc 4xx với message rõ ràng.

### 2.2 Lưu trữ file

- **Cấu trúc thư mục (trên đĩa):**
  ```
  /data/apk/
  └── {packageName}/           (ví dụ: vn.kztek.iparking.pos)
      └── {versionCode}/       (ví dụ: 1023)
          └── {appName}-{versionName}.apk   (ví dụ: iParking POS-1.2.3.apk)
  ```
- **Quy tắc:**
  - Tạo thư mục nếu chưa có.
  - Tên file: `{appName}-{versionName}.apk` (loại bỏ ký tự không hợp lệ cho filesystem).
  - **Không ghi đè:** trước khi ghi, kiểm tra file đích đã tồn tại → coi như trùng version (hoặc lỗi conflict), không ghi.

### 2.3 Metadata (MongoDB)

- Lưu mỗi phiên bản APK là một document.
- Các trường cần có (ví dụ):
  - `appName`, `packageName`, `versionCode`, `versionName`
  - `uploadedAt` (UTC)
  - `filePath` (đường dẫn đầy đủ hoặc tương đối theo base path)
  - (Tuỳ chọn) `fileSize`, `originalFileName` để hỗ trợ hiển thị và kiểm tra.

### 2.4 Danh sách & tra cứu

- **Danh sách ứng dụng:** nhóm theo `packageName`, mỗi app một mục (latest version hoặc count versions).
- **Danh sách version của một app:** filter theo `packageName`, sort theo `versionCode` desc hoặc `uploadedAt` desc.
- **Tải xuống:** API trả về file (stream) hoặc redirect đến URL phục vụ file tĩnh; client dùng link tải APK.

---

## 3. Thiết kế MongoDB

### 3.1 Collection: `apk_releases`

Mỗi document = một phiên bản APK đã upload.

```json
{
  "_id": ObjectId("..."),
  "appName": "iParking POS",
  "packageName": "vn.kztek.iparking.pos",
  "versionCode": 1023,
  "versionName": "1.2.3",
  "uploadedAt": ISODate("2025-01-01T10:00:00Z"),
  "filePath": "/data/apk/vn.kztek.iparking.pos/1023/iParking POS-1.2.3.apk",
  "fileSizeBytes": 15728640,
  "originalFileName": "app-release.apk"
}
```

**Index đề xuất:**

- `{ packageName: 1, versionCode: 1 }` — unique — đảm bảo không trùng packageName + versionCode, dùng cho kiểm tra trước khi upload và khi lưu.
- `{ packageName: 1, uploadedAt: -1 }` — cho truy vấn danh sách version theo app và sắp xếp theo thời gian.
- `{ packageName: 1, versionCode: -1 }` — cho sắp xếp theo versionCode.

---

## 4. Thiết kế REST API

Base path giả định: `/api`.

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/apk/upload` | Upload file APK (multipart). Trích xuất metadata, kiểm tra trùng, lưu file + DB. |
| GET | `/api/apps` | Danh sách ứng dụng (nhóm theo packageName, có thể kèm số version / version mới nhất). |
| GET | `/api/apps/{packageName}/versions` | Danh sách version của một app. Query: `sort=versionCode|uploadedAt`, `order=asc|desc`. |
| GET | `/api/apk/download/{packageName}/{versionCode}` | Tải file APK (trả về file stream hoặc 302 redirect). |
| GET | `/api/apk/{packageName}/{versionCode}` | Lấy metadata một phiên bản (optional). |

**Mã lỗi đề xuất:**

- 400: File không phải APK / không đọc được metadata.
- 409: Trùng packageName + versionCode.
- 404: App hoặc version không tồn tại.
- 500: Lỗi server (kèm message hoặc error code, không lộ path nội bộ).

---

## 5. Công nghệ APK parsing (C#)

- **Khuyến nghị:** dùng thư viện **ApkNet** (NuGet) hoặc **Iteedee.ApkReader**.
  - Đọc trực tiếp file APK (ZIP + binary AndroidManifest.xml), không cần cài aapt.
  - Trích xuất packageName, versionCode, versionName (và label cho appName) từ manifest, **không hard-code** thông tin version.
- **Alternative:** gọi **aapt** (hoặc aapt2) qua process nếu đã có sẵn trên server; parse stdout để lấy package, versionCode, versionName. Cách này phụ thuộc môi trường cài aapt.

**Luồng xử lý đề xuất:**

1. Nhận file upload → lưu tạm (temp).
2. Mở file tạm bằng ApkNet/ApkReader → đọc manifest → lấy packageName, versionCode, versionName, appName.
3. Nếu đọc lỗi → 400 "Invalid APK or could not read manifest".
4. Query MongoDB theo packageName + versionCode → nếu có bản ghi → 409 "Version already exists".
5. Tính đường dẫn lưu file và tên file; kiểm tra file đích đã tồn tại → 409 hoặc 500 tùy chính sách.
6. Copy/move từ temp sang đường dẫn cuối; tạo document trong `apk_releases`; xóa file tạm.
7. Trả 201 + metadata.

---

## 6. Cấu trúc solution (C#)

Đề xuất cấu trúc thư mục và project:

```
App02/
├── src/
│   ├── InternalApkDistribution.Api/          # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middleware/                       # global error handling
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── InternalApkDistribution.Core/         # Domain + interfaces
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── DTOs/
│   ├── InternalApkDistribution.Infrastructure/  # DB, file, APK parsing
│   │   ├── Persistence/                      # MongoDB repository
│   │   ├── Storage/                          # APK file save/read
│   │   └── ApkParsing/                       # ApkNet wrapper
│   └── InternalApkDistribution.Web/          # Optional: simple static UI hoặc Razor
├── docs/
├── tests/
└── InternalApkDistribution.sln
```

- **Api:** REST endpoints, validation, gọi Core/Infrastructure.
- **Core:** Entity (map với MongoDB document), DTO, interface (IAppRepository, IApkStorageService, IApkMetadataReader).
- **Infrastructure:** implementation MongoDB (MongoDB.Driver), lưu/đọc file theo cấu trúc thư mục, service đọc APK bằng ApkNet.

---

## 7. Giao diện Web (UI) tối thiểu

- **Trang 1 – Upload APK:** form chọn file .apk, nút Upload; hiển thị thông báo thành công/lỗi (từ API).
- **Trang 2 – Danh sách ứng dụng:** bảng/card danh sách app (packageName, tên hiển thị, số version); click vào app → chuyển trang version.
- **Trang 3 – Danh sách version:** bảng version (versionCode, versionName, ngày upload, nút Tải xuống); sort theo versionCode hoặc ngày.

Có thể triển khai bằng:
- Static HTML/JS gọi REST API (phục vụ từ wwwroot hoặc nginx), hoặc
- Razor Pages / minimal MVC trong cùng solution để đơn giản hóa triển khai nội bộ.

---

## 8. Cấu hình và bảo mật

- **Base path lưu APK:** cấu hình trong `appsettings.json` (ví dụ `"ApkStorage:BasePath": "/data/apk"`), không hard-code.
- **MongoDB:** connection string trong config hoặc biến môi trường.
- **Nội bộ:** có thể bảo vệ bằng firewall/VPN; nếu cần đăng nhập thì thêm auth (JWT hoặc Basic) sau.

---

## 9. Thứ tự triển khai đề xuất

1. Tạo solution + project (Api, Core, Infrastructure).
2. Định nghĩa entity/DTO và interface (Core).
3. Implement APK parsing (Infrastructure) với ApkNet.
4. Implement lưu file theo cấu trúc thư mục (Infrastructure).
5. Implement repository MongoDB + index (Infrastructure).
6. Implement API: upload, list apps, list versions, download.
7. Global exception handling và trả lỗi thống nhất.
8. Web UI đơn giản (3 màn hình).
9. Cấu hình (base path, MongoDB) và README hướng dẫn chạy.

Bạn có thể dùng tài liệu này làm spec để triển khai từng bước; nếu cần, bước tiếp theo là tạo solution và code mẫu cho từng phần (upload, parsing, MongoDB, API).