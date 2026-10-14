# Deploy lên IIS (PC local)

Bộ file deploy cho PC Windows chạy IIS. Chạy 2 script là xong phần lớn việc.

## 1. Cần cài trước trên PC

- **IIS**: Turn Windows features on/off → Internet Information Services (gồm cả ASP.NET 4.x nếu máy chưa có).
- **.NET 10 ASP.NET Core Hosting Bundle** (bắt buộc — để IIS chạy được app .NET): tải ở trang download .NET 10, mục "ASP.NET Core Runtime / Hosting Bundle". Cài xong thì **restart IIS** (`iisreset`).
- **IIS URL Rewrite Module** (bắt buộc — để link sâu như `/shop`, `/orders` không bị 404 khi refresh).
- **SQL Server** (bản đầy đủ hoặc Express) + tài khoản SQL đăng nhập được.
- **.NET 10 SDK** (để chạy `publish.ps1`).

## 2. Publish

Mở PowerShell **thường** (không cần admin) tại thư mục gốc repo:

```powershell
cd deploy\iis
.\publish.ps1 -ApiUrl https://localhost:5001 -ConnectionString "Server=127.0.0.1;Database=PhuThuongStickyRice;User Id=sa;Password=MAT_KHAU_THAT;MultipleActiveResultSets=true;Encrypt=False"
```

Script sẽ:

1. Publish WebAPI → `deploy\iis\publish\api`
2. Publish Blazor WASM → `deploy\iis\publish\web`
3. Copy `web-spa.web.config` → `web\web.config` (SPA fallback + MIME cho `.wasm`/`.dll`)
4. Sửa `web\wwwroot\appsettings.json` để frontend gọi API ở `-ApiUrl`
5. Publish + chạy Migrator 1 lần (chạy migration + seed 5 món xôi + tài khoản admin), rồi tự dừng nó sau 60 giây

Muốn bỏ bước Migrator: thêm `-SkipMigrator`. Muốn Migrator dùng connection string trong file của nó thì bỏ `-ConnectionString`.

## 3. Tạo site IIS

Mở PowerShell **Run as Administrator**:

```powershell
cd deploy\iis
.\setup-iis.ps1 -ApiPath "C:\duong\dan\deploy\iis\publish\api" -WebPath "C:\duong\dan\deploy\iis\publish\web"
```

Mặc định: app pool `PhuThuong` (No Managed Code), site API port `5001`, site Web port `5000`, và site API được set `ASPNETCORE_ENVIRONMENT=Production`. Đổi port/tên bằng tham số `-ApiPort`, `-WebPort`, `-ApiSiteName`, `-WebSiteName`.

Muốn tạo tay trong IIS Manager cũng được: 2 site → cùng 1 app pool No Managed Code → site API thêm biến môi trường `ASPNETCORE_ENVIRONMENT=Production` (Configuration Editor → `system.webServer/aspNetCore/environmentVariables`).

## 4. Sửa config trước khi chạy (bắt buộc)

| File | Chỗ sửa | Giá trị |
|---|---|---|
| `publish\api\appsettings.Production.json` | `ConnectionStrings` | Connection string SQL thật của PC |
| `publish\api\appsettings.Production.json` | `Jwt:Key` | **Đổi ngay** — key mẫu đang public trong repo, ai có nó cũng giả được token admin |
| `publish\api\appsettings.Production.json` | `CORS:AllowedOrigins` | Thêm URL site Web trên IIS (ví dụ `http://localhost:5000`), không thì trình duyệt chặn gọi API |
| `publish\web\wwwroot\appsettings.json` | `ResourceServer.Endpoint` | Đã được `publish.ps1` sửa theo `-ApiUrl`; kiểm tra lại nếu đổi port sau đó |

`MoMo` và `BankTransfer` trong `appsettings.json` vẫn đang để `xxx` (chế độ test). Điền thông tin thật sau, theo hướng dẫn trong README chính.

## 5. Kiểm tra

- Mở `http://localhost:5000` → trang shop hiện danh sách xôi.
- Đặt thử 1 đơn (COD) → vào `/track-order` tra cứu bằng mã đơn + SĐT.
- Mở `http://localhost:5001/swagger` → API chạy (trang Swagger).
- Đăng nhập admin: `admin@phuthuong.vn` / `Admin@123` → **đổi mật khẩu ngay** (trang đổi mật khẩu ở API: `POST /api/auth/change-password`).

## 6. Sự cố thường gặp

| Hiện tượng | Nguyên nhân / cách sửa |
|---|---|
| HTTP 500.19 (lỗi `web.config`) | Chưa cài Hosting Bundle, hoặc sai version. Cài Hosting Bundle .NET 10 rồi `iisreset`. |
| HTTP 500.30 / 500.31 (ANCM) | App crash khi khởi động. Xem Event Viewer → Windows Logs → Application, hoặc chạy `dotnet PhuThuongStickyRice.WebAPI.dll` trực tiếp trong thư mục `api` để xem lỗi. |
| Refresh ở `/shop`, `/orders` bị 404 | Chưa cài URL Rewrite Module, hoặc thiếu `web.config` trong thư mục `web`. |
| Gọi API bị CORS chặn (lỗi trong Console trình duyệt) | Chưa thêm URL site Web vào `CORS:AllowedOrigins` trong `appsettings.Production.json`. |
| 401 khi vào Dashboard/Đơn hàng | Chưa đăng nhập, hoặc token ký bằng key khác với key trong `appsettings.Production.json`. Đăng nhập lại ở `/login`. |
| `Login failed for user 'sa'` | Sai connection string / SQL Server chưa bật Mixed Mode / sai mật khẩu `sa`. |
| Migrator báo lỗi DbUp | Xem log console lúc chạy Migrator; thường do DB chưa tạo được (sai connection string) hoặc bảng đã tồn tại do chạy migration 2 lần. |

## Ghi chú

- Không cần chạy BackgroundServer khi test local (mail đang ở chế độ Fake).
- File `appsettings.Production.json` mẫu nằm ở `src/Backend/PhuThuongStickyRice.WebAPI/` — `publish.ps1` copy nó vào thư mục `api` cùng app. Sửa file **trong thư mục publish** sau mỗi lần publish lại, hoặc sửa file mẫu rồi publish lại.
