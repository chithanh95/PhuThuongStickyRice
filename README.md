# Xôi Phú Thượng — Web đặt hàng & quản lý quán

Web bán hàng các món xôi làng nghề Phú Thượng (Hà Nội) + hệ thống admin quản lý toàn diện:
khách xem menu, bỏ giỏ, đặt hàng online (khách tự đến lấy hoặc tự book shipper đến lấy — quán không giao hàng);
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
  (vd `PX-20261015-4821`); thanh toán COD / chuyển khoản (VietQR) / ví điểm.
  MoMo đang **TẮT** (chưa có tài khoản doanh nghiệp MoMo) — xem mục Ghi chú.
- **`/track-order`** — tra cứu đơn bằng SĐT: tự hủy khi đang chờ, sửa số lượng, đánh giá sao,
  xem điểm tích lũy + hạng thành viên.
- **Nhận ngay** — checkout không còn hẹn giờ/đặt trước; đơn số lượng lớn qua `/dat-cung` phải đặt
  trước tối thiểu 3 ngày. **Đặt hàng 24/7**: ngoài giờ mở cửa vẫn đặt được (mô hình Shopee) —
  đơn chờ quán xác nhận và chuẩn bị khi mở cửa; chỉ chặn khi quán bật bảo trì, tạm ngưng nhận
  đơn hoặc nghỉ lễ.
- **`/dat-tiec`, `/cung-gio`, `/dat-cung`, `/nhuong-quyen`** — đặt tiệc, mâm cúng giỗ/Rằm/mùng 1,
  đặt số lượng lớn, đăng ký nhượng quyền.
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

- **MoMo đang TẮT** bằng cờ tính năng `momo-payments` (mặc định tắt, từ 2026-10-21): khách không
  thấy lựa chọn MoMo ở checkout/tra cứu đơn, API tạo thanh toán + callback MoMo đều bị backend chặn.
  Khi có tài khoản doanh nghiệp MoMo: điền cấu hình thật (PartnerCode/key, `TestMode=false`, endpoint
  production) rồi bật cờ "Thanh toán MoMo" ở `/admin/tinh-nang` — không cần sửa code.
- **OTP chưa gửi được thật**: mã OTP (quên mật khẩu, xác thực SĐT...) hiện đi qua `LoggingSmsSender`
  — chỉ ghi log server. Cần tích hợp nhà cung cấp SMS thật trước khi mở bán công khai.
- Ảnh món/ảnh landing hiện là **placeholder SVG** — upload ảnh thật khi deploy.
- Không gửi email thật (tính năng mail đã loại khỏi phạm vi).
- Muốn chạy job nền (báo cáo tuần Zalo, voucher sinh nhật...): chạy thêm project `BackgroundServer`.

## Nhật ký thay đổi

- **2026-10-21** — Ba sửa theo phản hồi vận hành: (1) Kanban đơn hàng bỏ nút lùi trạng thái
  (domain chỉ cho đi tiến từng bước — nút lùi bấm luôn bị server từ chối); `GlobalExceptionFilter`
  nay map `DomainException` thành HTTP 400 kèm message thật thay vì 500, thông điệp chặn chuyển
  trạng thái dịch sang tiếng Việt. (2) Bỏ nút "Tạo mới" trên topbar admin (tàn dư template Lexa,
  không có chức năng) và dọn CSS liên quan. (3) Tab lọc ở trang báo giá đặt tiệc
  (`/admin/dat-tiec`) làm lại dạng pill theo màu chủ đạo Lexa, có dark mode.
- **2026-10-21** — Đóng dấu ngày giờ tập trung tại `StickyRiceDbContext` khi lưu: entity
  `ITrackable` mới (Added) chưa có `CreatedDateTime` sẽ được gán thời gian hiện tại, entity sửa
  (Modified) luôn gán `UpdatedDateTime`. Trước đây chỉ Repository tự chế mới stamp, các luồng
  CRUD dùng repository ABP mặc định để `CreatedDateTime` xuống DB thành 0001-01-01 (phát hiện
  qua Orders; model có 80 bảng có cột ngày giờ dùng chung cơ chế).
- **2026-10-21** — Sửa lỗi bấm Xác nhận đơn bị `DbUpdateConcurrencyException` ("expected 1 row,
  affected 0"): các UPDATE nguyên tử trong `OrderRepository` (`TryTransitionStatusAsync`,
  `TryMarkSpinUsedAsync`) ghi thẳng xuống DB khiến RowVersion của dòng tăng ngay, entity đang
  track thành cũ → lần lưu entity ngay sau đó khớp 0 dòng. Repository nay đồng bộ RowVersion mới
  vào entity đang track ngay sau mỗi UPDATE thô thành công; bản ghi lịch sử trạng thái cũng được
  đóng dấu `CreatedDateTime`.
- **2026-10-21** — Sửa lỗi không đọc lại được đơn hàng (`SqlNullValueException` ở tra cứu đơn +
  dashboard ngay sau khi tạo đơn đầu tiên): 19 trường văn bản tùy chọn của `Order` (địa chỉ, ghi
  chú, mã giảm giá, lý do hủy...) khai báo lại đúng `string?` — khớp cột DB vốn cho phép NULL;
  trước đây EF coi là bắt buộc nên đọc phải NULL là nổ. Không cần migration (schema DB không đổi).
  Hàm tạo `Order`/`OrderLine` nay đóng dấu `CreatedDateTime` thật thay vì để mặc định 0001-01-01.
- **2026-10-21** — Trang tra cứu đơn: lỗi server không còn bị báo sai thành "Không tìm thấy đơn
  hàng" / "SĐT không đúng định dạng" — `TrackOrderAsync` trả null chỉ khi thật sự không thấy đơn,
  lỗi khác hiện message thật của server; mục lịch sử kiểm tra định dạng SĐT ngay tại máy khách
  bằng validator dùng chung trước khi gọi API.
- **2026-10-21** — Sửa lỗi crash khi đặt đơn nhiều món (`OrderLine` trùng key `Guid.Empty` lúc
  lưu — factory nay gán Guid ngay khi tạo dòng). Checkout: hình thức nhận hàng mặc định đổi thành
  "Tôi tự đến quán lấy"; mã giới thiệu được kiểm tra ngay trên màn hình (endpoint
  `GET api/loyalty/referral-check`, sai là báo đỏ dưới ô nhập, không chờ tạo đơn mới lỗi);
  dòng "Đặt hộ" dùng style giống dòng "Nhớ tên & SĐT"; giãn cách các option thanh toán;
  ô mã giảm giá cao bằng nút "Áp dụng".
- **2026-10-21** — Bỏ chặn đặt đơn ngoài giờ mở cửa: khách đặt 24/7, quán xác nhận và làm đơn khi
  mở cửa (trước đây cả checkout lẫn server đều từ chối ngoài khung 6:00–10:30). Checkout/banner
  ngoài giờ chỉ hiện thông báo "vẫn đặt được"; bảo trì + tạm ngưng + nghỉ lễ vẫn chặn như cũ.
  Kèm fix: checkbox/radio ở checkout thống nhất 20px trên mọi màn hình (trước đây trên PC
  checkbox "Đặt hộ" nhỏ hơn checkbox "Nhớ tên & SĐT").
- **2026-10-21** — Tắt thanh toán MoMo bằng cờ `momo-payments` (mặc định tắt): ẩn khỏi checkout,
  tra cứu đơn và trang giả lập; backend chặn tạo thanh toán + callback. Bật lại ở `/admin/tinh-nang`
  sau khi có tài khoản MoMo doanh nghiệp + cấu hình thật.
- **2026-10-21** — Gia cố giao diện: trang `/shop` tải thông tin phụ fail-soft theo từng nguồn
  (1 API phụ lỗi không còn làm mất các phần còn lại); menu điều hướng chống lỗi khởi tạo ngôn ngữ.
