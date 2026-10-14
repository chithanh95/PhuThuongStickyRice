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

## Wave 4 (items 101–120)

- **Cache sản phẩm (101)**: danh sách sản phẩm public (`IMemoryCache`, TTL 5 phút)
  bị vô hiệu hóa khi tạo/sửa/xóa/tắt-bật sản phẩm, topping, combo.
- **Nén response (102)**: middleware Brotli + Gzip đã bật cho WebAPI.
  Hướng dẫn bật nén ở tầng IIS nằm trong `deploy/iis/README.md`
  ("Bật nén HTTP trên IIS").
- **Giám sát sức khỏe (103)**: worker `SystemHealthWorker` chạy mỗi 5 phút,
  kiểm tra DB + dung lượng ổ đĩa uploads + tần suất lỗi gần đây; báo Telegram
  (token/chat id cấu hình trong `/admin/shop-settings`, để trống = tắt) và
  đẩy vào hàng đợi email. Đây là kiểm tra từ trong app — vẫn nên dùng dịch vụ
  giám sát ngoài (UptimeRobot, Better Uptime...) để giám sát thật sự từ internet.
- **Nhật ký hệ thống (104)**: entry Warning+ được lưu vào bảng `AppLog`
  (giữ tối đa 2000 dòng mới nhất); xem tại `/admin/nhat-ky-he-thong`.
- **Feature flags (105)**: bật/tắt `lucky-spin`, `gift-wrap`, `referrals`,
  `dish-voting`, `promo-popup` tại `/admin/tinh-nang`. Cả UI (Blazor) và
  backend (WebAPI) đều kiểm tra flag trước khi cho phép tính năng.
- **Cấu hình thanh toán (106)**: thông tin ngân hàng (tên NH, số TK, chủ TK,
  template QR) và MoMo **không bí mật** (partner code, endpoint) chỉnh tại
  `/admin/thanh-toan`, lưu vào ShopSetting; app ưu tiên đọc DB, fallback về
  appsettings. Secret (MoMo access/secret key, khóa ký VietQR...) vẫn nằm
  trong `appsettings.Production.json`.
- **API versioning (107)**: Orders/Products/Landing có thêm route versioned
  `api/v1/...` (route cũ không đổi, vẫn chạy song song):
  `GET /api/v1/products`, `POST /api/v1/orders`, `GET /api/v1/landing/content`.
- **Đa chi nhánh (109)**: CRUD tại `/admin/chi-nhanh`; `Product.BranchId` và
  `Order.BranchId` (nullable); public filter `?branch=` trên shop; chi nhánh
  hiển thị trên đơn hàng. **Giá vẫn dùng chung toàn hệ thống** — giá riêng
  theo chi nhánh là việc tương lai.
- **Webhooks (110)**: cấu hình tại `/admin/webhooks` (Name, Url, Secret,
  events `OrderCreated` / `OrderStatusChanged`, IsActive) + nút "Gửi thử".
  Khi có sự kiện, app POST JSON tới URL kèm header `X-Webhook-Signature`
  (HMAC-SHA256 của body với Secret). Lịch sử gửi xem ở cùng trang
  (`WebhookDelivery`: thời gian, endpoint, event, thành công, status code).
- **Maintenance mode (111)**: bật tại `/admin/shop-settings` ("Bảo trì &
  giám sát") — `GET /api/system/status` public cho biết trạng thái; app WASM
  kiểm tra khi khởi động, hiện trang bảo trì cho route public, các route
  `/login`, `/admin/*`, `/bep` vẫn vào được.
- **Dọn ảnh mồ côi (112)**: worker hàng tuần đối chiếu file trong
  `wwwroot/uploads/landing` với URL ảnh được tham chiếu trong DB
  (LandingPageContent, Article); chỉ xóa file **không được tham chiếu ở đâu
  cả và đã tồn tại > 7 ngày**.
- **Dung lượng đĩa (113)**: trang `/admin/he-thong` hiện kích thước + số file
  thư mục uploads, ước lượng dung lượng DB; cảnh báo khi uploads vượt ngưỡng
  cấu hình (mặc định 500 MB, chỉnh trong ShopSetting).
- **Sao lưu ảnh (114)**: interface `IImageBackupProvider`, mặc định là
  `NoneImageBackupProvider` (không làm gì). Cấu hình provider + settings JSON
  trong `/admin/shop-settings`. Muốn thêm provider thật (ví dụ S3): tạo class
  implement `IImageBackupProvider`, đăng ký trong DI (thay
  `NoneImageBackupProvider`), đọc settings JSON đã lưu.
- **PWA (115)**: `manifest.json` (tên "Xôi Phú Thượng", icons 192/512),
  service worker `sw.js` cache app shell + trang `offline.html` khi mất mạng,
  đăng ký tự động trong `index.html`.
- **Link rút gọn (116)**: `/admin/link-rut-gon` tạo link 6 ký tự;
  `GET /s/{code}` redirect 302. Trong quick-view sản phẩm có nút
  "🔗 Link ngắn" tạo link tới `/shop?tim={code}`.
- **Đặt món nhóm (117)**: tạo nhóm tại `/nhom`, chia sẻ link `/nhom/{code}`;
  người có link thêm món (tên + sản phẩm + số lượng); người tạo "Chốt đơn" →
  tạo đơn hàng COD bình thường và đóng nhóm.
- **Gợi ý theo giờ (118)**: trang shop tự động hiện "☀️ Gợi ý buổi sáng"
  (5h–10h, ưu tiên món có tag mặn) / "🌙 Gợi ý buổi tối" (17h–23h, 3 món)
  — tính phía client theo giờ máy.
- **Sổ địa chỉ (119)**: bảng `AddressBook` (SĐT, nhãn, địa chỉ); checkout hiện
  chip chọn nhanh + nút "💾 Lưu địa chỉ này"; quản lý trong phần hồ sơ của
  `/track-order`.
- **Voucher freeship (120)**: `DiscountCode.VoucherType` (Discount/Freeship);
  freeship đặt phí giao hàng = 0; tạo/chọn loại tại `/admin/discounts`;
  checkout hiện dòng "🚚 Miễn phí ship".

## Ghi chú

- Ảnh sản phẩm hiện là placeholder SVG trong `Blazor.Modules/wwwroot/images/`
  (theo mã sản phẩm: `XOI-GAC` → `xoi-gac.svg`...). Thay bằng ảnh thật khi deploy.
- Số liệu "khách vào xem" (page views) chưa có — dashboard hiện thống kê theo đơn hàng/doanh thu.
  Muốn có page views thì cần thêm bảng `PageViews` + middleware ghi log (có thể làm ở phase sau).
