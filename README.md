# Xôi Huyền — Web đặt hàng & quản lý quán

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

### 2026-10-24 — Sửa lỗi giỏ hàng server + checkout tự điền đủ từ tài khoản + logo đầy đủ trên PC
- **Sửa lỗi "Invalid column name 'CreatedDateTime' / 'RowVersion' / 'UpdatedDateTime'" khi chọn món/bấm thanh toán**: migration viết tay `20261019070000_AddShoppingCart` tạo bảng `ShoppingCarts` đã thiếu 3 cột gốc mà mọi entity đều có (kế thừa `Entity<TKey>`), trong khi EF luôn truy vấn cả 3 cột — giỏ hàng server của khách đã đăng nhập lỗi từ lúc bảng được tạo. Thêm migration tay `20261024093000_AddShoppingCartBaseColumns` (raw SQL có guard `COL_LENGTH`, chạy lại an toàn) bổ sung đúng 3 cột. **Cần chạy `Update-Database` sau khi pull.**
- **Checkout tự điền đủ thông tin người đặt từ tài khoản khi đã đăng nhập**: hồ sơ khách (`api/customer/profile`) nay trả thêm **email** của tài khoản và trang thanh toán tự điền email bên cạnh họ tên + SĐT như trước (các ô vẫn sửa được khi đặt hộ người khác). Tài khoản tạo trước đợt lưu họ tên (23/10) chưa có tên nên không tự điền được — trang tài khoản nay có **ô "Họ tên" sửa được** ngay đầu tab Hồ sơ (lưu dạng claim `fullName` qua cùng nút "Lưu thay đổi"; khách bổ sung 1 lần, các lần đặt sau tự điền hết).
- **Trang tài khoản**: mục "Địa chỉ đã lưu" chuyển lên ngay dưới mục Ngày sinh (trên mục Khẩu vị) để tiện sửa cùng lúc.
- **Header trên PC (≥992px, cả shop lẫn landing)**: hiện **logo đầy đủ** có sẵn chữ trong ảnh (file `wwwroot/images/logo-header.png` — bản thu nhỏ 320px của `logo.png` chính thức) thay cho logo tròn + chữ HTML bên cạnh; mobile/tablet giữ nguyên logo tròn như thiết kế đã chốt. Ảnh og:image và các icon không đổi.

### 2026-10-23 — Tiếng Việt toàn hệ thống + tối gọn header + trang tài khoản chia tab (theo rà soát ngôn ngữ & UI/UX)
- **Mọi thông báo lỗi nay là tiếng Việt, không còn dính dấu ngoặc kép**: (1) toàn bộ lỗi của `AuthController` (đăng nhập sai, trùng SĐT, OTP/2FA, đổi mật khẩu, quên mật khẩu...) trả dạng `{ message }` tiếng Việt — trước đây trả chuỗi trần, frontend bọc thêm ngoặc kép nên khách thấy nguyên `"Invalid username/email or password."`; thông báo sai đăng nhập dùng 1 câu chung, không lộ tài khoản có tồn tại hay không. (2) Thêm `VietnameseIdentityErrorDescriber` — mọi lỗi ASP.NET Identity (mật khẩu ngắn/trùng tài khoản...) đều tiếng Việt. (3) Frontend đọc được lỗi dạng JSON string (hết hiện ngoặc kép thừa). (4) Chuyển qua lại tab Đăng nhập ↔ Đăng ký nay **xóa lỗi cũ**. (5) Dịch nốt: lỗi nghiệp vụ đặt hàng/thanh toán/gói tuần/ảnh khách, toàn bộ thông điệp validation ở Domain (đơn hàng, món, tiền tệ), `[Required]` của các form auth, thông điệp lỗi 500 mặc định, màn "Đang tải..." + thanh báo lỗi Blazor, badge trạng thái đơn ở khu nhân viên, và dòng giới thiệu mang tên cũ "Phú Thượng" trong từ điển EN/中文/日本語. (Thông điệp MoMo giữ nguyên tiếng Anh có chủ đích — cổng MoMo đang TẮT.)
- **Header shop tối gọn theo trạng thái đăng nhập**: bỏ số hotline (chỉ còn icon 📞 bấm gọi — menu đã có mục Liên hệ, số đầy đủ ở footer), bỏ nút "Đặt hàng ngay" (trùng icon giỏ hàng), cụm "Xin chào + Đăng xuất" thay bằng **1 nút Tài khoản duy nhất** bấm mở menu con (Thông tin tài khoản / Đăng xuất; tài khoản nhân viên hiện "Quản trị" + thêm lối vào trang quản trị). Menu hamburger khi đã đăng nhập hiện "Tài khoản của tôi" (nhân viên: "Vào trang quản trị") thay vì vẫn hiện "Đăng nhập". Menu chính thêm mục **Tra cứu đơn hàng** (khóa `nav.track_order` đủ 4 ngôn ngữ). Footer khu khách **bỏ link "Đăng nhập quản trị"** (nhân viên gõ thẳng `/login`).
- **Trang tài khoản (`/tai-khoan`) chia 3 tab** — Hồ sơ / Đơn hàng / Bảo mật: tab Hồ sơ gộp **1 form duy nhất, 1 nút "Lưu thay đổi"** cho cả thông tin + ngày sinh (đưa lên đầu form) + khẩu vị + địa chỉ (form sửa địa chỉ hết nút Lưu riêng). Nhãn **"Vai trò" dịch tiếng Việt** (Khách hàng/Quản trị viên/Thu ngân/Nhân viên bếp). Mục điểm tích lũy/điểm khả dụng/tổng tiền đã mua đổi sang hàng nhãn–giá trị 2 cột nên hết cảnh số "thò ra ngoài" trên mobile; nút "Thêm" khẩu vị cao bằng ô nhập kề bên.
- **Landing**: các thanh thông báo phụ (khuyến mãi/lễ/giờ vắng/thời tiết/nhắc đặt lại) nay **chỉ hiện tối đa 1 thanh** theo thứ tự ưu tiên thay vì xếp chồng; header bỏ số hotline (giữ icon 📞) và **thêm nút chữ lớn A+** như shop; dòng phụ dưới logo thống nhất "Làng nghề Hà Nội"; bỏ nút Facebook nổi (trỏ viết cứng về trang chủ facebook.com — chưa có link fanpage trong cấu hình) và nút gọi nổi (trùng thanh "Gọi quán" dính dưới mobile); footer bỏ 1 link `/shop` bị trùng.
- **Trang /shop**: bỏ badge giờ mở cửa + alert tạm ngưng ở khối hero (đã có banner trạng thái ở đầu trang hiện đủ cả lý do); bảng "Xôi nào hợp bạn?" **bỏ cột giá viết cứng** (giá thật nằm ở lưới món lấy từ DB); sidebar danh mục mobile (≤480px) dính ở `top: 68px` khớp header 1 hàng (trước 120px theo header 2 hàng đã bỏ, hở ~60px).
- **Trang 404, /error và /doi-mat-khau nay dùng `ShopLayout`** như mọi trang khách (trước rơi vào `MainLayout` — giao diện template cũ, lệch hẳn với shop).
- **Nhóm trang báo giá hết chồng tên + có link chéo**: `/dat-tiec` đổi tiêu đề thành "Đặt tiệc"; cả 3 trang Đặt tiệc / Đặt số lượng lớn / Đặt cúng đều có dòng link sang 2 trang còn lại.

### 2026-10-23 — Đợt sửa trang thanh toán + tra cứu đơn theo phản hồi khách dùng thử
- **Đăng ký nay LƯU họ tên khách nhập** (trước đây form đăng ký có ô "Họ tên" nhưng server bỏ mất vì bảng tài khoản không có cột tên): họ tên được lưu dạng claim `fullName` trên bảng `UserClaims` có sẵn — **không cần chạy migration**. Hồ sơ khách (`api/customer/profile`) nay trả kèm họ tên + SĐT tài khoản.
- **Checkout tự điền họ tên + SĐT cho khách đã đăng nhập**: lấy từ hồ sơ tài khoản, sau đó là địa chỉ mặc định, và ngay khi mở trang cũng tải luôn điểm thưởng/ví + tên từ đơn cũ của SĐT đó (trước đây phải chạm vào ô SĐT mới tải). Ô họ tên nay nhận chữ ngay khi gõ (`oninput` — trước đây một lần cập nhật giao diện chen ngang có thể xóa chữ đang gõ dở khiến khách tưởng ô bị khóa). Ô SĐT có thêm **nút ✕** bấm để xóa nhanh số nhập sai (đồng thời gỡ điểm/ví đã tải theo số cũ).
- **Đặt hàng xong ở lại màn hình thành công**: bỏ hành vi tự nhảy thẳng sang trang tra cứu với đơn COD/ví. Panel "Đặt hàng thành công!" hiện đầy đủ thông tin đơn và cho khách tự chọn **"🧾 Xem lại đơn"** / **"📦 Tra cứu đơn hàng"** / xem thực đơn / về trang chủ. Đơn chuyển khoản/MoMo vẫn hiện hướng dẫn thanh toán ngay tại panel như trước.
- **Trang tra cứu đơn nhận diện khách đã đăng nhập**: tự điền SĐT tài khoản vào các ô tra cứu và tự tải lịch sử đơn; server trả **bản đầy đủ** cho chính chủ SĐT đã đăng nhập (danh tính đã xác thực bằng mật khẩu — dùng chung cơ chế `IsOwnerRequest` với tra cứu đơn gần nhất) nên không cần nhập lại SĐT + OTP nữa. Khách chưa đăng nhập giữ nguyên luồng cũ: danh sách tối thiểu + OTP để mở chi tiết (bảo mật chống xem trộm lịch sử của người khác không đổi).
- **Trạng thái đơn trên trang tra cứu nay dịch theo ngôn ngữ đang chọn** (khóa `order_status.*` trong `AppLocalizationService`, đủ VI/EN/中文/日本語): trước đây badge in nguyên chữ tiếng Anh "Completed/Pending..." kể cả khi khách chọn tiếng Việt.
- **Responsive + khoảng cách trang thanh toán**: bảng tóm tắt đơn trên mobile khóa `table-layout: fixed` + chia sẵn bề rộng cột — trước đây bề rộng tối thiểu của bảng vượt card nên bảng bị cuộn ngang, nhìn lệch trái và cột ✕ xóa món bị đẩy khỏi màn hình. Nhịp dọc trong card tóm tắt thống nhất 16px giữa các khối (bảng/mã giảm giá/điểm thưởng/nút đặt hàng).

### 2026-10-23 — Mở trang POS bán tại quầy (/admin/pos)
- Backend POS (item 127: `PosController` + `PosAppService`) đã có sẵn từ trước — nay bổ sung **giao diện trang POS** còn thiếu, thêm mục menu "POS bán tại quầy" trong sidebar admin (vai trò Admin + Cashier).
- Cách dùng: nhân viên chạm thẻ món để thêm vào đơn (món có nhiều kích cỡ/topping thì bấm "Tuỳ chọn" để chọn), tăng/giảm số lượng ngay trong giỏ, nhập tên/SĐT khách nếu có (bỏ trống = "Khách vãng lai"; có SĐT thì đơn tích điểm như đơn web), nhập số tiền khách đưa để máy tính tiền thối. Bấm **"Thu tiền mặt — Tạo đơn"**: server tính lại toàn bộ tiền, ghi nhận đã thanh toán tiền mặt và **hoàn tất đơn ngay** (nguồn đơn "POS"); màn hình xác nhận có nút in hóa đơn. POS chỉ áp dụng cho khách trả tiền mặt tại quầy — khách chuyển khoản vẫn đặt qua web/checkout như thường.
- Đợt rà soát thông luồng (đội test) phát hiện + đã sửa: `ApiClientBase.PostAsync` trước đây ném chuỗi lỗi mặc định khi server từ chối, nuốt mất lý do thật — nay đọc `message` của server cho mọi POST (áp dụng chung cho các màn hình admin dùng POST, gồm POS): nhân viên thấy đúng lý do tiếng Việt (quán tạm ngưng, món ngoài giờ bán, topping vừa tắt...).

### 2026-10-23 — Dọn 2 link chết trong NavMenu
- Bỏ mục **"Đặt món nhóm"** (`href="nhom"` — tính năng đặt món theo nhóm đã gỡ từ trước, link trỏ vào trang không còn tồn tại) và mục **"Đăng nhập quản trị"** (`href="login"`) khỏi `NavMenu.razor` của Web host: khu khách hàng không chứa lối vào quản trị (nhân viên đăng nhập qua trang `/login` riêng như thiết kế hiện hành). Mục "Đăng nhập / Đăng ký" của khách giữ nguyên.

### 2026-10-22 — Sửa hero shop tràn ngang mobile + lịch sử đơn hiện đúng lỗi server
- **Hero shop mobile**: badge trạng thái trong hero (vd "Quán đang nghỉ — vẫn nhận đặt hàng, mở lại lúc 06:00...") bị Bootstrap ép `white-space: nowrap` → tràn khỏi hero, đẩy cả khối chữ rộng hơn màn hình (dòng mô tả bị cắt mép phải). Nay badge được xuống dòng bình thường + cột chữ hero co được (`min-width: 0`).
- **Tra cứu lịch sử đơn theo SĐT**: khi server trả lỗi, frontend trước đây chỉ đọc thông điệp thật ở nhánh có OTP; nhánh thường ném nguyên chuỗi lỗi thô `net_http_message_not_success_statuscode_reason, 400, Bad Request` lên mặt khách. Nay `GetOrdersByPhoneAsync` đọc `message` của server cho mọi lỗi — khách thấy lý do thật bằng tiếng Việt.
- **Địa chỉ landing**: nội dung landing đọc từ DB (bảng `LandingPageContent`, seed 2026-10-14 còn địa chỉ/SĐT/email mẫu cũ) — cập nhật qua admin `/admin/landing` hoặc script SQL `CapNhat-ThongTin-Landing.sql` (Ngõ 277 Xuân Đỉnh · 0389 245 468 · Maihuyen280579@gmail.com).

### 2026-10-22 — Header mobile 1 dòng + sửa font tên quán + bỏ mục Gợi ý & Combo gia đình ở shop
- **Font tên quán (PC)**: CSS khai font 'Lora' cho chữ thương hiệu nhưng app.css chưa từng tải Lora → trình duyệt lùi về Georgia, lỗi dấu chồng tiếng Việt ("Huyề n"). Nay @import tải thêm Lora (có subset tiếng Việt) cùng Cormorant Garamond — "Xôi Huyền" hiển thị đúng dấu ở header shop + landing + footer.
- **Header mobile (shop + landing)**: chỉ còn logo hình tròn, ẩn chữ thương hiệu; header shop về **1 dòng duy nhất** (bỏ kiểu 2 hàng ở ≤480px) — ẩn nút "Đặt ngay", ẩn số hotline (giữ icon 📞 bấm gọi), ẩn khối tài khoản (đăng nhập/tài khoản vẫn có trong menu hamburger); landing ẩn chữ thương hiệu ở ≤768px và header không xuống dòng.
- **Trang shop bỏ 2 mục**: "Gợi ý buổi sáng / Gợi ý buổi tối" theo giờ và "Combo gia đình" (gỡ cả giao diện + code liên quan trong Shop.razor). Combo sẽ được tạo/quản lý ở web admin (admin đã có sẵn mục Combo); service/model combo phía khách GIỮ NGUYÊN vì trang tra cứu đơn vẫn cần để hiển thị các đơn cũ có combo.

- **2026-10-22** — **Đưa hình logo vào các vị trí thương hiệu trên web** (trước đây chỉ
  là chữ): header landing + header shop (desktop và mobile dùng chung một header), sidebar
  admin (thay icon đồng hồ mẫu Lexa), trang đăng nhập nhân viên/khách và quên mật khẩu
  (thay emoji 🍚), footer landing/shop và đầu hóa đơn in. File dùng chung:
  `wwwroot/images/logo-round.png` (bản tròn nền trong suốt, hiển thị 30–88px tùy vị trí,
  luôn kèm tên chữ "Xôi Huyền" bên cạnh để dễ đọc ở kích thước nhỏ).
- **2026-10-22** — **Logo chính thức mới của Xôi Huyền** (phong cách khắc di sản: ụ xôi
  Hoàng Phố vàng nghệ phủ ruốc + hành phi khô trên 1 lá dong mềm, nền giấy kem, viền vàng
  kép, chữ serif viết hoa): đã gắn vào web — `images/logo.png` (ảnh chia sẻ og:image /
  twitter:image + ảnh hiển thị ở trang chi tiết sản phẩm), icon PWA `icons/icon-192.png`
  và `icons/icon-512.png`, apple-touch-icon và `favicon.ico` đều dùng logo mới. Hai file
  logo chuẩn (bản vuông + bản tròn nền trong suốt) lưu ở thư mục logo của dự án. Nhân tiện
  bỏ nhắc MoMo trong twitter:description (MoMo đang tắt theo cờ `momo-payments`).
- **2026-10-22** — **Đổi tên thương hiệu "Xôi Phú Thượng" → "Xôi Huyền"** (logo mới do
  chủ quán chọn, bỏ dòng PHÚ THƯỢNG): mọi chuỗi hiển thị trong code (tiêu đề trang,
  header/footer, email xác nhận, thông báo, hóa đơn in, manifest PWA) dùng tên mới.
  Địa chỉ quán sửa thành **Ngõ 277 Xuân Đỉnh, Từ Liêm, Hà Nội**; hotline **0389 245 468**;
  email liên hệ **Maihuyen280579@gmail.com**. Manifest bỏ câu "giao tận nơi" (quán không
  giao hàng). Giữ nguyên có chủ đích: tên chủ tài khoản ngân hàng (tên pháp lý trên tài
  khoản), các migration lịch sử, domain/email hệ thống, và các nhắc tới làng nghề Phú
  Thượng (địa danh/nghề truyền thống — không phải tên thương hiệu). Nội dung landing và
  mẫu tin nhắn lưu trong database cần sửa tương ứng ở trang admin.
- **2026-10-22** — Rà soát tester vòng 2 (toàn bộ luồng shop + đối chiếu endpoint): sửa route
  backend `api/momo-reconciliation` và `api/voucher-campaigns` (trước lệch gạch nối với
  frontend nên 404 — trang Đối soát MoMo chết hẳn); xóa 16 method frontend gọi endpoint
  không tồn tại (corporate-clients, work-shifts, shift-closes, message-templates); nút "Đã
  nhận tiền" không còn hiện trên đơn đã hủy; form "Xác nhận đã lấy hàng" hiện đúng cả với
  đơn đặt trước cũ; Checkout có guard cứng chống bấm đúp đặt hàng ngay đầu handler; trang
  Tài khoản: hạng thành viên lỗi thì báo không tải được thay vì kẹt "Đang tải...", lỗi tải
  địa chỉ không còn báo sai "cần đăng nhập", đặt mặc định/xóa địa chỉ và đổi mật khẩu hết
  nuốt lỗi/thiếu guard; nhãn hình thức nhận hàng mặc định ở tra cứu đơn đổi thành "Nhận
  tại quán" (quán không giao hàng).
- **2026-10-22** — Sửa điều hướng đăng nhập theo khu vực + rà soát tester toàn bộ web admin:
  helper mới `LoginRoutes` chọn trang đăng nhập theo route — khu vực nhân viên (`/admin`,
  `/dashboard`, `/orders`, `/products`, `/customers`) về `/login` (email + mật khẩu), khách về
  `/dang-nhap` (số điện thoại); áp dụng ở cả 3 điểm điều hướng (`RedirectToLogin`, handler 401,
  `ApiClientBase`). Trước đây mọi trang đều về trang đăng nhập khách nên tài khoản admin không
  đăng nhập lại được khi mở thẳng link quản trị hoặc hết hạn token. Kèm các sửa từ đợt rà soát:
  Lãi gộp nạp dữ liệu tuần tự (2 loader dùng chung `OrderService`, hết race token) và báo lỗi khi
  tải giá vốn; Chi nhánh/Khuyến mãi/Nhật ký hiện message lỗi thật khi tải danh sách thay vì nuốt
  im lặng; nút "Hôm qua" ở Báo cáo giờ bấm là nạp luôn số liệu.
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
