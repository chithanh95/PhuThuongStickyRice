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
- `/checkout` — nhập tên/SĐT/địa chỉ → đặt hàng, nhận mã đơn (vd `PX-20261014-4821`);
  hóa đơn VAT (tùy chọn), gói quà, đặt cho người khác, đổi điểm tích lũy, mã giới thiệu,
  vòng quay may mắn sau khi đặt thành công
- `/track-order` — tra cứu đơn: tự hủy đơn (khi đang chờ), sửa số lượng, đánh giá sao khi đã giao,
  xem điểm tích lũy + hạng thành viên, lưu ngày sinh
- `/dat-dinh-ky` — đăng ký giao xôi mỗi ngày (tự tạo đơn COD mỗi sáng)
- `/faq` — câu hỏi thường gặp (có tìm kiếm); `/ho-tro` — gửi yêu cầu hỗ trợ

### Web admin (đăng nhập)
- `/dashboard` — thẻ thống kê: tổng đơn, doanh thu, món đã bán, đơn chờ xử lý;
  đơn theo trạng thái, doanh thu 7 ngày, đơn mới nhất;
  tự động làm mới mỗi 30s (có nút tắt/mở + nút làm mới thủ công)
- `/orders` — tìm kiếm/lọc đơn, xem chi tiết, chuyển trạng thái
  (`Pending → Confirmed → Preparing → Delivering → Completed`, hoặc `Cancelled`)
- `/products` — thêm/sửa/xóa món, đổi giá, kích hoạt/ngừng bán
- `/admin/loyalty` — điểm tích lũy, điều chỉnh điểm, sinh nhật sắp tới (voucher tự động)
- `/admin/danh-gia` — đánh giá sao của khách; `/admin/ho-tro` — yêu cầu hỗ trợ (trả lời/đổi trạng thái)
- `/admin/faq` — quản lý câu hỏi thường gặp; `/admin/dinh-ky` — đăng ký giao định kỳ (tạm dừng/kích hoạt)
- `/admin/integrations` — trạng thái + test tích hợp Zalo OA
- `/admin/shop-settings` — phí ship, ngưỡng miễn ship, đơn tối thiểu, hết hạn chuyển khoản,
  đặt cọc, điểm tích lũy, hạng thành viên (Đồng/Bạc/Vàng), phí gói quà

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

## Zalo OA (gửi hóa đơn điện tử qua Zalo)

Tích hợp hiện ở chế độ **stub**: khi chưa cấu hình, mỗi lần đặt hàng service chỉ ghi log
`[Zalo OA stub] would send Zalo message...` — không gọi API thật, không làm hỏng checkout.

Các bước bật gửi tin nhắn thật:

1. Tạo Official Account tại `https://oa.zalo.me` và chờ duyệt.
2. Vào OA → *Cài đặt → Thông tin OA* để lấy **OA ID**.
3. Tạo ứng dụng tại `https://developers.zalo.me`, liên kết với OA, lấy **access token**
   (token hết hạn định kỳ — cần cơ chế refresh, xem tài liệu Zalo).
4. Điền vào `appsettings.json` của WebAPI:
   ```json
   "ZaloOa": { "AccessToken": "<token-thật>", "OaId": "<oa-id-thật>" }
   ```
5. Restart WebAPI. Vào `/admin/integrations` bấm **Gửi test** để kiểm tra.
6. Để gửi thật, implement thêm trong `ZaloNotificationService.SendTextMessageAsync`
   (Zalo định danh người nhận bằng `userId`, không phải SĐT — cần map SĐT → userId qua API
   `GET /v2.0/oa/getfollowers` hoặc ZNS template theo tài liệu
   `https://developers.zalo.me/docs/api/official-api`).

## Background workers (WebAPI)

- `OrderAutoCancelWorker` — mỗi 5 phút hủy đơn chuyển khoản `Pending`+`Unpaid` quá
  `ShopSetting.BankTransferExpiryMinutes` (mặc định 30 phút).
- `BirthdayVoucherWorker` — mỗi 24h tạo voucher sinh nhật giảm 20% (dùng 1 lần, HSD 30 ngày,
  gắn với SĐT) cho tài khoản có sinh nhật hôm nay.
- `SubscriptionOrderWorker` — mỗi 6h tạo đơn COD cho các đăng ký định kỳ đến hạn
  (chống trùng bằng `LastOrderDate`).
- Các worker mới chỉ được kiểm tra biên dịch (compile) trong môi trường dev —
  cần kiểm tra lại hành vi runtime khi deploy lên IIS (ở đó có SQL Server thật).

## Ghi chú

- Ảnh sản phẩm hiện là placeholder SVG trong `Blazor.Modules/wwwroot/images/`
  (theo mã sản phẩm: `XOI-GAC` → `xoi-gac.svg`...). Thay bằng ảnh thật khi deploy.
- Số liệu "khách vào xem" (page views) chưa có — dashboard hiện thống kê theo đơn hàng/doanh thu.
  Muốn có page views thì cần thêm bảng `PageViews` + middleware ghi log (có thể làm ở phase sau).
