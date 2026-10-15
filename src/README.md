# PhuThuongStickyRice — Cấu trúc solution & quy ước phát triển

Tài liệu kỹ thuật cho developer. Hướng dẫn deploy IIS xem `../deploy/iis/README.md`.

## Bố cục solution (`src/PhuThuongStickyRice.sln`)

### Backend (`src/Backend/`)

| Project | Mục đích |
|---|---|
| `PhuThuongStickyRice.Domain` | Entities (aggregate roots `Product`, `Order`...), ValueObjects (`ProductCode`, `Money`), repository interfaces, domain behaviors |
| `PhuThuongStickyRice.Application` | ABP Application Services + DTOs + AutoMapper profiles (luồng nghiệp vụ chính) |
| `PhuThuongStickyRice.CrossCuttingConcerns` | Helpers dùng chung phía backend |
| `PhuThuongStickyRice.Infrastructure` | Tích hợp bên ngoài: Zalo OA, Messenger, webhook, export Excel... |
| `PhuThuongStickyRice.Persistence` | EF Core module (`StickyRiceEntityFrameworkCoreModule`), `StickyRiceDbContext` (kế thừa `AbpDbContext`), repository implementations |
| `PhuThuongStickyRice.WebAPI` | Controllers: API public (`/api/products`, `/api/orders`...) + API admin (`/api/admin/...`) |
| `PhuThuongStickyRice.Migrator` | EF Core migrations + seed data. Chạy 1 lần mỗi khi có migration mới |
| `PhuThuongStickyRice.BackgroundServer` | Host chạy worker định kỳ (báo cáo tuần Zalo 8h sáng T2, voucher sinh nhật, auto-cancel đơn...) |

DI dùng **Autofac** (`UseAutofac` trong `Program.cs`): interception của ABP (UoW, Validation,
Authorization — Castle DynamicProxy) chỉ hoạt động với Autofac, không hoạt động với MS DI mặc định.

### Frontend (`src/Frontend/`)

| Project | Mục đích |
|---|---|
| `PhuThuongStickyRice.Web` | **Host** Blazor WebAssembly: `Program.cs` (DI, HttpClient), `App.razor` (router), `wwwroot/index.html`. Không chứa UI nghiệp vụ |
| `PhuThuongStickyRice.Web.Components` | **Razor class library** — toàn bộ UI, tổ chức theo area: |

```
Web.Components/
  Shop/        # Web bán hàng public: /shop, /cart, /checkout, /track-order, /dat-tiec...
  Core/        # Layouts (EmptyLayout, AdminLayout), trang chủ Index.razor (+ CSS)
  Dashboard/   # /dashboard — thẻ thống kê, biểu đồ
  Orders/      # /orders — duyệt đơn, chi tiết đơn
  Products/    # /products — CRUD món, upload ảnh, kéo thả sắp xếp
  Admin/       # Trang admin còn lại: tài chính, marketing, hệ thống, cài đặt...
  Landing/     # Models/services phục vụ trang chủ
  wwwroot/     # css/admin-lexa.css (theme admin), scripts/interop.js, images placeholder
```

### Tests (`tests/`)

`PhuThuongStickyRice.UnitTests`, `PhuThuongStickyRice.IntegrationTests`,
`PhuThuongStickyRice.ContractTests`, `PhuThuongStickyRice.ArchTests`.

## Build

```bash
dotnet build src/PhuThuongStickyRice.sln
```

Kỳ vọng: **0 errors**. (Warnings analyzer/style có sẵn thì bỏ qua.)

## Migration (EF Core code-first)

Migration chạy qua project `Migrator` (không `Update-Database` thủ công):

```bash
dotnet run --project src/Backend/PhuThuongStickyRice.Migrator
```

Tạo migration mới (khi đổi entity):

```bash
cd src/Backend/PhuThuongStickyRice.Migrator
dotnet ef migrations add <TenMigration> --context StickyRiceDbContext -o Migrations/StickyRiceDb
```

### 3 quy ước rút ra từ thực tế (bắt buộc nhớ)

1. **Entity mới BẮT BUỘC phải có `DbSet<>` public trong `StickyRiceDbContext`.**
   ABP `AddDefaultRepositories` chỉ đăng ký repository cho entity có `DbSet` —
   thiếu thì compile vẫn qua nhưng runtime báo không resolve được repository.
2. **Seed `InsertData` phải gồm mọi cột non-nullable** (vd `CreatedDateTime`).
   Thiếu cột → migration chạy lỗi giữa chừng.
3. **Kiểm tra tay `Up()` của migration sau khi scaffold.** Model snapshot đồng bộ
   không đảm bảo `Up()` đúng — đã từng lọt `AlterColumn`/`DropColumn` cho cột không tồn tại.
   Quy tắc: không bao giờ emit `AlterColumn`/`DropColumn` cho cột mà không có migration nào tạo ra;
   quét toàn bộ chain khi nghi ngờ trùng `AddColumn`/`CreateTable`.

## Chạy local

Thứ tự bắt buộc:

1. **Migrator** — tạo DB + seed (lần đầu), áp migration mới (các lần sau)
2. **WebAPI** — `dotnet run --project src/Backend/PhuThuongStickyRice.WebAPI`
3. **Web** — `dotnet run --project src/Frontend/PhuThuongStickyRice.Web`
4. (Tùy chọn) **BackgroundServer** — nếu cần job nền (báo cáo tuần, voucher sinh nhật)

Connection string: key `ConnectionStrings:PhuThuongStickyRice` trong `appsettings.json`
của WebAPI, Migrator, BackgroundServer.

## Quy ước commit

- Làm trực tiếp trên `main`, **không force-push**.
- Commit nhỏ theo nhóm tính năng, message tiếng Việt.
- Chỉ stage đúng file của mình (`git add <path>`), không `git add -A`
  (nhiều người/agent làm chung 1 checkout).
