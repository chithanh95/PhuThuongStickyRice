# Xôi Phú Thượng — Web đặt hàng

Web đặt hàng các món xôi làng nghề Phú Thượng (Hà Nội): khách xem menu, bỏ giỏ, đặt hàng online;
admin có dashboard thống kê, quản lý đơn hàng và sản phẩm.

## Kiến trúc

- **.NET 10 LTS** (Blazor WebAssembly + ASP.NET Core WebAPI)
- **Clean Architecture**: `Domain` → `Application` → `Persistence` → `WebAPI`
- **DDD**: `Product`, `Order` là aggregate root (private setters, factory `Create`, behavior methods, value objects `ProductCode`/`Money`)
- **ABP Framework 10.7**: Application Services thay cho CQRS thủ công — `ProductAppService`, `OrderAppService`, `DashboardAppService`
  (trả `PagedResultDto<T>`; UoW/Auditing/Validation qua interceptor). MediatR đã gỡ khỏi các luồng Product/Order/Dashboard.
  Host dùng **Autofac** (`Program.cs`: Generic Host + `UseAutofac`, `StickyRiceWebApiModule` phụ thuộc `AbpAutofacModule`) vì
  interception của ABP (Castle DynamicProxy) chỉ chạy với Autofac, không chạy với MS DI mặc định.
  Các module ABP: `StickyRiceDomainModule`, `StickyRiceApplicationModule` (AutoMapper), `StickyRiceEntityFrameworkCoreModule` (`AddAbpDbContext` + `EfCoreRepository`), `StickyRiceWebApiModule`.
  Các luồng legacy (Users, Roles, Files, AuditLogs...) vẫn dùng Dispatcher/`CrudService` như cũ.
- **SQL Server + EF Core (code-first)**, migration qua project `Migrator`

## Cấu trúc solution

```
src/
  Backend/
    PhuThuongStickyRice.Domain/        # Entities, ValueObjects, Repositories (interfaces)
    PhuThuongStickyRice.Application/  # ABP Application Services + DTOs + AutoMapper profiles
    PhuThuongStickyRice.Persistence/  # ABP EF Core module, DbContext (AbpDbContext), repositories
    PhuThuongStickyRice.WebAPI/       # Controllers
    PhuThuongStickyRice.Migrator/     # EF Core migrations + seed data
    ...
  Frontend/
    PhuThuongStickyRice.BlazorWebAssembly/  # Host
    PhuThuongStickyRice.Blazor.Modules/
      Shop/        # Web bán hàng: /shop, /cart, /checkout (không cần đăng nhập)
      Dashboard/   # Admin: /dashboard (cần đăng nhập)
      Orders/      # Admin: /orders — duyệt đơn, đổi trạng thái (cần đăng nhập)
      Products/    # Admin: quản lý sản phẩm (cần đăng nhập)
tests/
```

## Tính năng

### Web bán hàng (public)
- `/shop` — danh sách món xôi (Xôi gấc, Xôi đỗ, Xôi xéo, Xôi vò, Xôi ngũ sắc), thêm vào giỏ
- `/cart` — giỏ hàng: tăng/giảm số lượng, xóa món
- `/checkout` — nhập tên/SĐT/địa chỉ → đặt hàng, nhận mã đơn (vd `PX-20261014-4821`)

### Web admin (đăng nhập)
- `/dashboard` — thẻ thống kê: tổng đơn, doanh thu, món đã bán, đơn chờ xử lý;
  đơn theo trạng thái, doanh thu 7 ngày, đơn mới nhất
- `/orders` — tìm kiếm/lọc đơn, xem chi tiết, chuyển trạng thái
  (`Pending → Confirmed → Preparing → Delivering → Completed`, hoặc `Cancelled`)
- `/products` — thêm/sửa/xóa món, đổi giá, kích hoạt/ngừng bán

### API chính
| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/products?status=Active` | Danh sách sản phẩm (public) |
| POST | `/api/orders` | Đặt hàng (public) |
| GET | `/api/orders` | Danh sách đơn (admin) |
| PUT | `/api/orders/{id}/status` | Đổi trạng thái đơn (admin) |
| GET | `/api/dashboard/stats` | Số liệu dashboard (admin) |

## Chạy local

1. Cập nhật connection string SQL Server trong `appsettings.json` của `WebAPI` và `Migrator`.
2. Chạy migration + seed 5 món xôi:
   ```bash
   cd src/Backend/PhuThuongStickyRice.Migrator
   dotnet run
   ```
3. Chạy WebAPI:
   ```bash
   cd ../PhuThuongStickyRice.WebAPI
   dotnet run
   ```
4. Chạy Blazor WASM:
   ```bash
   cd ../../../Frontend/PhuThuongStickyRice.BlazorWebAssembly
   dotnet run
   ```

## Ghi chú

- Ảnh sản phẩm hiện là placeholder SVG trong `Blazor.Modules/wwwroot/images/`
  (theo mã sản phẩm: `XOI-GAC` → `xoi-gac.svg`...). Thay bằng ảnh thật khi deploy.
- Số liệu "khách vào xem" (page views) chưa có — dashboard hiện thống kê theo đơn hàng/doanh thu.
  Muốn có page views thì cần thêm bảng `PageViews` + middleware ghi log (có thể làm ở phase sau).
