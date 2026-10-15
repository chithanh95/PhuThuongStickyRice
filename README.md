# Xôi Phú Thượng — Web đặt hàng & quản lý quán

Web bán hàng các món xôi làng nghề Phú Thượng (Hà Nội) + hệ thống admin quản lý toàn diện:
khách xem menu, bỏ giỏ, đặt hàng online (shipper tự đến lấy hoặc giao tận nơi);
admin có dashboard, duyệt đơn, bếp, POS bán tại quầy, quản lý món/khách/tài chính/marketing.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Runtime | .NET 10 (LTS) |
| Kiến trúc | Clean Architecture + DDD (`Domain` → `Application` → `Persistence` → `WebAPI`) |
| Framework | ABP Framework 10.7 (Application Services, UoW, Validation, AutoMapper) |
| DI | Autofac (ABP interception chỉ chạy với Autofac, không chạy với MS DI mặc định) |
| Frontend | Blazor WebAssembly (host) + Razor class library (components) |
| Database | SQL Server + EF Core code-first, migration qua project `Migrator` |

`Product` và `Order` là aggregate root (private setters, factory `Create`, behavior methods,
value objects `ProductCode`/`Money`). Các module ABP: `StickyRiceDomainModule`,
`StickyRiceApplicationModule`, `StickyRiceEntityFrameworkCoreModule` (`AddAbpDbContext` + `EfCoreRepository`),
`StickyRiceWebApiModule`.

## Cấu trúc solution

```
src/
  Backend/
    PhuThuongStickyRice.Domain/              # Entities, ValueObjects, repository interfaces
    PhuThuongStickyRice.Application/        # ABP Application Services + DTOs + AutoMapper
    PhuThuongStickyRice.CrossCuttingConcerns/ # Shared helpers dùng chung backend
    PhuThuongStickyRice.Infrastructure/     # Tích hợp ngoài (Zalo, Messenger, webhook...)
    PhuThuongStickyRice.Persistence/        # EF Core module, StickyRiceDbContext, repositories
    PhuThuongStickyRice.WebAPI/             # Controllers (public + admin)
    PhuThuongStickyRice.Migrator/           # EF Core migrations + seed data
    PhuThuongStickyRice.BackgroundServer/   # Worker định kỳ (báo cáo tuần, voucher sinh nhật...)
  Frontend/
    PhuThuongStickyRice.Web/                # Host Blazor WASM (Program.cs, App.razor)
    PhuThuongStickyRice.Web.Components/    # Razor class library — toàn bộ UI
      Shop/        # /shop, /cart, /checkout (public, không cần đăng nhập)
      Core/        # Layouts (EmptyLayout, AdminLayout...), trang chủ Index.razor
      Dashboard/   # /dashboard (admin)
      Orders/      # /orders — duyệt đơn (admin/thu ngân)
      Products/    # /products — quản lý món (admin)
      Admin/       # Các trang admin còn lại (tài chính, marketing, hệ thống...)
      Landing/     # Models/services cho trang chủ
tests/
  PhuThuongStickyRice.UnitTests/
  PhuThuongStickyRice.IntegrationTests/
  PhuThuongStickyRice.ContractTests/
  PhuThuongStickyRice.ArchTests/
deploy/
  iis/             # Hướng dẫn deploy IIS + script publish (xem deploy/iis/README.md)
  seed-admin.sql   # Script tạo tài khoản admin thủ công (dự phòng)
```

## Tính năng

### Web bán hàng (public, không cần đăng nhập)

- **Trang chủ** (`/`) — rebuild theo template Simple Garden: hero fullscreen, giới thiệu làng nghề,
  lưới món đặc trưng, câu chuyện thương hiệu, banner "Đặt xôi ngay", thực đơn theo tab, footer.
- **`/shop`, `/cart`, `/checkout`** — xem món, giỏ hàng (lưu local), đặt hàng nhận mã đơn
  (vd `PX-20261015-4821`); thanh toán COD / chuyển khoản (VietQR) / MoMo / ví điểm.
- **`/track-order`** — tra cứu đơn bằng SĐT: tự hủy khi đang chờ, sửa số lượng, đánh giá sao,
  xem điểm tích lũy + hạng thành viên.
- **Đặt trước** — hẹn giờ lấy/giao; hệ thống chặn đơn trùng ngày lễ và ngoài khung giờ bán của món.
- **`/dat-tiec`, `/cung-gio`, `/dat-cung`, `/nhuong-quyen`** — đặt tiệc, mâm cúng giỗ/Rằm/mùng 1,
  gom đơn văn phòng, đăng ký nhượng quyền.
- **Tài khoản khách** (`/dang-nhap`, `/tai-khoan`) — đăng ký bằng SĐT + mật khẩu, lưu nhiều địa chỉ,
  khẩu vị yêu thích, lịch sử mua hàng.
- **Đánh giá** — đánh giá sao sau khi nhận hàng; review có ảnh được tặng mã giảm giá.

### Admin (đăng nhập, theme Lexa)

- **Dashboard** — thẻ thống kê, doanh thu 7 ngày, tự làm mới 30s; **dự báo doanh thu** 7/30 ngày;
  nút đọc báo cáo bằng giọng nói.
- **Đơn hàng** (`/orders`) — phím tắt (J/K/C/P/X), phát hiện & gộp đơn trùng SĐT,
  mẫu tin nhắn theo trạng thái (copy gửi Zalo), đánh dấu đã thu tiền mặt.
- **Bếp** (`/bep`) — đơn đặt trước hiện trước + đếm ngược; đơn khách VIP có banner ưu tiên.
- **POS** (`/ban-hang`) — bán tại quầy, tự lưu đơn chờ khi mất mạng và đồng bộ lại sau.
- **Sản phẩm** (`/products`) — kéo thả sắp xếp, bán theo suất/kg, khung giờ bán,
  giới hạn suất mỗi ngày, upload nhiều ảnh, lịch sử ai sửa món.
- **Khách hàng** (`/customers`) — tag khách (VIP/ưu tiên/nợ...), đánh dấu VIP được ưu tiên,
  xuất Excel, nhắc khách lâu chưa quay lại, quà sinh nhật tự động.
- **Tài chính** — báo cáo doanh thu, **lãi gộp theo món**, doanh thu theo giờ,
  so sánh cùng kỳ, đối soát MoMo/ngày, kết ca cuối ngày, kiểm kê.
- **Marketing** — mã giảm giá, chiến dịch voucher, popup khuyến mãi, điểm danh,
  vòng quay may mắn, check-in tặng quà.
- **Hệ thống** — phân quyền chi tiết tới từng nút (Admin/Cashier/Kitchen),
  nhật ký hoạt động nhân viên, chế độ bảo trì, lịch nghỉ lễ (chặn đặt đơn),
  webhook báo đơn mới, màn hình số thứ tự chờ (`/cho`), tìm kiếm toàn cục, trung tâm thông báo.

### Tích hợp

| Tích hợp | Trạng thái |
|---|---|
| Zalo OA | **Stub** — chưa có token thật thì chỉ ghi log, không gọi API, không ảnh hưởng checkout |
| Chatbot Messenger | Cần Page Access Token + Verify Token thật mới chạy (cấu hình ở Cài đặt) |
| Báo cáo tuần Zalo | Worker chạy **8h sáng thứ Hai** (cần chạy `BackgroundServer`), gửi doanh thu/tuần cho chủ quán |
| Webhook đơn mới | POST JSON về URL cấu hình mỗi khi có đơn (timeout 5s, lỗi không ảnh hưởng đơn) |

## Chạy local

1. Sửa connection string SQL Server trong `appsettings.json` của **WebAPI** và **Migrator**
   (key `ConnectionStrings:PhuThuongStickyRice`).
2. Chạy migration + seed:
   ```bash
   dotnet run --project src/Backend/PhuThuongStickyRice.Migrator/PhuThuongStickyRice.Migrator.csproj
   ```
3. Chạy WebAPI, rồi chạy Blazor WASM:
   ```bash
   dotnet run --project src/Backend/PhuThuongStickyRice.WebAPI
   dotnet run --project src/Frontend/PhuThuongStickyRice.Web
   ```

> **Quan trọng:** mỗi lần `git pull` mà có migration mới thì chạy lại **Migrator** rồi **restart WebAPI**
> (frontend đổi thì rebuild/republish project Web). Không chạy Migrator = app lỗi vì thiếu cột/bảng.

## Deploy

Xem chi tiết tại [`deploy/iis/README.md`](deploy/iis/README.md) + script [`deploy/publish.ps1`](deploy/iis/publish.ps1).

Lưu ý trên IIS: cấp quyền **Modify** cho app pool trên thư mục `wwwroot/uploads` (ảnh món, ảnh landing
do admin upload). DataProtection keys lưu trong database nên nhiều instance vẫn chung key.

## Ghi chú

- Ảnh món/ảnh landing hiện là **placeholder SVG** — upload ảnh thật khi deploy.
- Không gửi email thật (tính năng mail đã loại khỏi phạm vi).
- Muốn chạy job nền (báo cáo tuần Zalo, voucher sinh nhật...): chạy thêm project `BackgroundServer`.
