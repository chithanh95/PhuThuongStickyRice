using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// codequality-team 2026-10-20 run-0245 Agent1-Naming

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Product aggregate root. All state changes go through intention-revealing
    /// behaviors so domain invariants are enforced in one place.
    /// </summary>
    public class Product : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 2000;
        public const int TagsMaxLength = 200;
        public const int CategoryMaxLength = 100;
        public const int AllergenInfoMaxLength = 500;
        public const int UnitMaxLength = 20;

        /// <summary>Selling unit: "suất" (per portion, default) or "kg" (per kilogram).</summary>
        public const string UnitPortion = "suất";

        public const string UnitKg = "kg";

        public ProductCode Code { get; private set; }

        /// <summary>Tên món hiển thị cho khách (vd "Xôi gấc").</summary>
        public string Name { get; private set; }

        /// <summary>Mô tả ngắn về món (nguyên liệu đặc trưng, khẩu vị) hiện ở card shop.</summary>
        public string Description { get; private set; }

        /// <summary>Giá bán niêm yết của món (VND). Khi có PromoPrice thấp hơn thì card hiện giá KM.</summary>
        public Money Price { get; private set; }

        /// <summary>
        /// Item 256: giá khuyến mãi (VND). Null = không có KM. Khi có giá trị
        /// và nhỏ hơn <see cref="Price"/>, card món hiện "giá gốc → giá KM".
        /// </summary>
        public decimal? PromoPrice { get; private set; }

        /// <summary>
        /// Cost price (giá vốn) in VND, admin-editable. Powers the
        /// /admin/loi-nhuan profit report (revenue − cost).
        /// </summary>
        public decimal CostPrice { get; private set; }

        /// <summary>
        /// Selling unit: "suất" (per portion) or "kg" (per kilogram).
        /// For "kg" products the price is per 1 kg; the shop sells
        /// 500g / 1kg packs through size options.
        /// </summary>
        public string Unit { get; private set; } = UnitPortion;

        /// <summary>
        /// Manual display order (item 146): the admin drags products into
        /// order on /products; the shop lists by SortOrder. 0 = default.
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null/empty = sold all day.
        /// </summary>
        public string AvailableHoursJson { get; private set; }

        public const int AvailableHoursJsonMaxLength = 1000;

        // [codequality-smell 2026-10-20] Magic number 3 trong
        // ChangeAvailableHoursJson → hằng số có tên, đồng bộ với
        // MaxImageUrlsPerProduct ở dưới (trước đây 1 chỗ dùng const, 1 chỗ
        // hardcode 3 — đổi giới hạn mà quên sửa 1 chỗ sẽ lệch message).
        public const int MaxSaleWindowsPerDay = 3;

        /// <summary>
        /// Độ lệch giờ Việt Nam so với UTC (+7, không có giờ mùa hè).
        /// Khung giờ bán trong ngày (AvailableHoursJson) luôn tính theo giờ VN.
        /// </summary>
        private const int VietnamUtcOffsetHours = 7;

        /// <summary>
        /// Item 165: max portions sold per day. Null = unlimited.
        /// </summary>
        public int? DailyLimit { get; private set; }

        /// <summary>
        /// Item 166: product photo gallery as a JSON string array of image
        /// URLs. First item = primary image. Null/empty = placeholder SVG.
        /// </summary>
        public string ImageUrlsJson { get; private set; }

        public const int ImageUrlsJsonMaxLength = 4000;

        // Giới hạn số ảnh trong gallery mỗi món: quá nhiều ảnh làm trang
        // shop tải chậm và JSON dài hơn cột cho phép (ImageUrlsJsonMaxLength).
        public const int MaxImageUrlsPerProduct = 10;

        /// <summary>Trạng thái bán của món (Đang bán / Hết hàng / Ngừng bán) — shop chỉ hiện món Đang bán.</summary>
        public ProductStatus Status { get; private set; }

        /// <summary>
        /// Dietary tags as a comma-separated list, e.g. "chay,man" or "cay".
        /// Supported tags: "chay" (vegetarian), "man" (savory), "cay" (spicy).
        /// </summary>
        public string Tags { get; private set; }

        /// <summary>Menu category, e.g. "Xôi mặn", "Xôi ngọt", "Món ăn kèm".</summary>
        public string Category { get; private set; }

        /// <summary>Allergen information shown in the quick-view, e.g. "đậu phộng, tôm".</summary>
        public string AllergenInfo { get; private set; }

        /// <summary>Shows a "Món mới" badge on the shop.</summary>
        public bool IsNew { get; private set; }

        /// <summary>Shows a "Signature" badge on the shop.</summary>
        public bool IsSignature { get; private set; }

        /// <summary>
        /// Seasonal availability window. Null = no restriction. The public
        /// shop hides products outside the window; the admin list still
        /// shows them with an "out of season" badge.
        /// </summary>
        public DateTime? AvailableFrom { get; private set; }

        public DateTime? AvailableTo { get; private set; }

        /// <summary>
        /// Branch the product belongs to (nullable). Null = available at
        /// all branches. Pricing stays global (item 109).
        /// </summary>
        public Guid? BranchId { get; private set; }

        /// <summary>Assigns or clears the branch.</summary>
        public void ChangeBranch(Guid? branchId)
        {
            BranchId = branchId;
        }

        // EF Core
        private Product()
        {
        }

        private Product(ProductCode code, string name, Money price, string description)
        {
            Code = code;
            Name = name;
            Price = price;
            Description = description;
            Status = ProductStatus.Active;
        }

        /// <summary>
        /// Factory khởi tạo món mới: validate mã/tên/giá trước khi tạo,
        /// tự trim khoảng trắng; trạng thái mặc định là Active.
        /// </summary>
        /// <param name="code">Mã món (bắt buộc).</param>
        /// <param name="name">Tên món (bắt buộc, tối đa 200 ký tự).</param>
        /// <param name="price">Giá bán (bắt buộc).</param>
        /// <param name="description">Mô tả; null được phép.</param>
        /// <returns>Món mới ở trạng thái Active.</returns>
        /// <exception cref="DomainException">Ném khi thiếu mã/tên/giá hoặc vượt độ dài cho phép.</exception>
        public static Product Create(ProductCode code, string name, Money price, string description = null)
        {
            if (code == null)
            {
                throw new DomainException("Product code is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Product name must not be empty.");
            }

            if (name.Trim().Length > NameMaxLength)
            {
                throw new DomainException($"Product name must not exceed {NameMaxLength} characters.");
            }

            if (price == null)
            {
                throw new DomainException("Product price is required.");
            }

            if (description != null && description.Length > DescriptionMaxLength)
            {
                throw new DomainException($"Product description must not exceed {DescriptionMaxLength} characters.");
            }

            return new Product(code, name.Trim(), price, description?.Trim());
        }

        /// <summary>
        /// Cập nhật tên + mô tả món. Không cho sửa món đã ngừng bán
        /// (Discontinued) để giữ nguyên dữ liệu lịch sử đơn hàng.
        /// </summary>
        /// <exception cref="DomainException">Ném khi món đã Discontinued hoặc tên/mô tả không hợp lệ.</exception>
        public void UpdateDetails(string name, string description)
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot update a discontinued product.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Product name must not be empty.");
            }

            if (name.Trim().Length > NameMaxLength)
            {
                throw new DomainException($"Product name must not exceed {NameMaxLength} characters.");
            }

            if (description != null && description.Length > DescriptionMaxLength)
            {
                throw new DomainException($"Product description must not exceed {DescriptionMaxLength} characters.");
            }

            Name = name.Trim();
            Description = description?.Trim();
        }

        /// <summary>
        /// Đổi giá bán món. Món đã ngừng bán (Discontinued) không được đổi
        /// giá để dữ liệu lịch sử và báo cáo lãi gộp không bị sai lệch.
        /// </summary>
        /// <param name="newPrice">Giá bán mới (bắt buộc).</param>
        /// <exception cref="DomainException">Ném khi thiếu giá hoặc món đã Discontinued.</exception>
        public void ChangePrice(Money newPrice)
        {
            if (newPrice == null)
            {
                throw new DomainException("Product price is required.");
            }

            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot change the price of a discontinued product.");
            }

            Price = newPrice;
        }

        /// <summary>Sets the cost price (giá vốn) in VND. Must not be negative.</summary>
        public void ChangeCostPrice(decimal costPrice)
        {
            if (costPrice < 0)
            {
                throw new DomainException("Cost price must not be negative.");
            }

            CostPrice = costPrice;
        }

        /// <summary>
        /// Item 256: đặt/xóa giá khuyến mãi. Null hoặc ≤ 0 = xóa KM.
        /// Giá KM phải nhỏ hơn giá bán thì card mới hiện (không validate cứng
        /// ở đây để admin linh hoạt — UI chỉ hiện khi PromoPrice &lt; Price).
        /// </summary>
        public void ChangePromoPrice(decimal? promoPrice)
        {
            if (promoPrice.HasValue && promoPrice.Value < 0)
            {
                throw new DomainException("Giá khuyến mãi không được âm.");
            }

            PromoPrice = promoPrice is > 0 ? promoPrice : null;
        }

        /// <summary>
        /// Item 256 fix (bug-hunt 2026-10-19): giá bán hiệu dụng của món.
        /// Giá KM chỉ được áp dụng khi có giá trị, dương, và rẻ hơn giá đang
        /// xét (giá gốc hoặc giá size đã chọn); ngược lại dùng giá gốc.
        /// Admin đặt nhầm giá KM cao hơn giá gốc thì hệ thống không bao giờ
        /// tính đắt hơn giá gốc cho khách — khớp đúng điều kiện hiển thị
        /// trên card (UI chỉ hiện PromoPrice khi rẻ hơn giá đang hiện).
        /// </summary>
        /// <param name="basePrice">Giá đang xét (giá gốc hoặc giá size).</param>
        /// <returns>Giá KM nếu hợp lệ và rẻ hơn, ngược lại giá gốc.</returns>
        public decimal ResolveEffectivePrice(decimal basePrice)
        {
            return PromoPrice.HasValue && PromoPrice.Value > 0 && PromoPrice.Value < basePrice
                ? PromoPrice.Value
                : basePrice;
        }

        /// <summary>
        /// Sets the selling unit: "suất" (per portion) or "kg" (per kilogram).
        /// Null/empty normalizes to "suất".
        /// </summary>
        public void ChangeUnit(string unit)
        {
            var normalized = string.IsNullOrWhiteSpace(unit)
                ? UnitPortion
                : unit.Trim().ToLowerInvariant();

            if (normalized != UnitPortion && normalized != UnitKg)
            {
                throw new DomainException("Unit must be 'suất' or 'kg'.");
            }

            Unit = normalized;
        }

        /// <summary>
        /// Sets the manual display order (item 146). Must not be negative.
        /// </summary>
        public void ChangeSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
            {
                throw new DomainException("Sort order must not be negative.");
            }

            SortOrder = sortOrder;
        }

        /// <summary>
        /// Item 164: sets the daily sale time windows. Null/empty = sold all
        /// day. Otherwise a JSON array of {"from":"HH:mm","to":"HH:mm"}
        /// windows (tối đa <see cref="MaxSaleWindowsPerDay"/>), each with from &lt; to.
        /// </summary>
        public void ChangeAvailableHoursJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                AvailableHoursJson = null;
                return;
            }

            var trimmed = json.Trim();
            if (trimmed.Length > AvailableHoursJsonMaxLength)
            {
                throw new DomainException("Khung giờ bán quá dài.");
            }

            var windows = ParseHourWindows(trimmed);
            if (windows.Count == 0)
            {
                throw new DomainException("Khung giờ bán không hợp lệ.");
            }

            if (windows.Count > MaxSaleWindowsPerDay)
            {
                throw new DomainException($"Tối đa {MaxSaleWindowsPerDay} khung giờ bán mỗi ngày.");
            }

            AvailableHoursJson = trimmed;
        }

        /// <summary>
        /// Item 165: sets the daily portion limit. Null = unlimited.
        /// </summary>
        public void ChangeDailyLimit(int? limit)
        {
            if (limit.HasValue && limit.Value < 0)
            {
                throw new DomainException("Giới hạn suất/ngày không được âm.");
            }

            DailyLimit = limit;
        }

        /// <summary>
        /// Item 166: sets the photo gallery. Null/empty clears it. Otherwise
        /// a JSON string array of image URLs (max 10).
        /// </summary>
        public void ChangeImageUrlsJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                ImageUrlsJson = null;
                return;
            }

            var trimmed = json.Trim();
            if (trimmed.Length > ImageUrlsJsonMaxLength)
            {
                throw new DomainException("Danh sách ảnh quá dài.");
            }

            var urls = ParseImageUrls(trimmed);
            if (urls.Count == 0)
            {
                throw new DomainException("Danh sách ảnh không hợp lệ.");
            }

            if (urls.Count > MaxImageUrlsPerProduct)
            {
                throw new DomainException("Tối đa 10 ảnh cho mỗi món.");
            }

            ImageUrlsJson = trimmed;
        }

        /// <summary>Item 166: parsed gallery URLs; empty when none or invalid.</summary>
        public List<string> GetImageUrls()
        {
            return ParseImageUrls(ImageUrlsJson);
        }

        private static List<string> ParseImageUrls(string json)
        {
            var urls = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return urls;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return urls;
                }

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var url = element.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(url) && url.Length <= 500)
                        {
                            urls.Add(url);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fail-open: bad data never hides the gallery logic's fallback.
            }

            return urls;
        }

        private static List<(TimeSpan From, TimeSpan To)> ParseHourWindows(string json)
        {
            var windows = new List<(TimeSpan From, TimeSpan To)>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return windows;
                }

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object)
                    {
                        return new List<(TimeSpan From, TimeSpan To)>();
                    }

                    if (!element.TryGetProperty("from", out var fromElement)
                        || !element.TryGetProperty("to", out var toElement))
                    {
                        return new List<(TimeSpan From, TimeSpan To)>();
                    }

                    var fromStr = fromElement.GetString();
                    var toStr = toElement.GetString();
                    if (!TimeSpan.TryParse(fromStr, out var from)
                        || !TimeSpan.TryParse(toStr, out var to)
                        || from >= to)
                    {
                        return new List<(TimeSpan From, TimeSpan To)>();
                    }

                    windows.Add((from, to));
                }
            }
            catch (JsonException)
            {
                return new List<(TimeSpan From, TimeSpan To)>();
            }

            return windows;
        }

        /// <summary>
        /// Mở bán lại món đang tạm ngưng (Inactive). Món đã Discontinued
        /// không được kích hoạt lại — đó là trạng thái cuối để dữ liệu
        /// lịch sử đơn hàng luôn nhất quán.
        /// </summary>
        /// <exception cref="DomainException">Ném khi món đã Discontinued.</exception>
        public void Activate()
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("A discontinued product cannot be re-activated.");
            }

            Status = ProductStatus.Active;
        }

        /// <summary>
        /// Tạm ngưng bán (Inactive): món biến mất khỏi shop nhưng admin vẫn
        /// mở lại được sau. Khác <see cref="Discontinue"/> là trạng thái cuối.
        /// </summary>
        /// <exception cref="DomainException">Ném khi món đã Discontinued.</exception>
        public void Deactivate()
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Product is already discontinued.");
            }

            Status = ProductStatus.Inactive;
        }

        /// <summary>
        /// Ngừng bán vĩnh viễn: trạng thái cuối, không thể <see cref="Activate"/>
        /// lại. Đơn cũ đã bán vẫn giữ nguyên thông tin món trong lịch sử.
        /// </summary>
        public void Discontinue()
        {
            Status = ProductStatus.Discontinued;
        }

        /// <summary>
        /// Updates the marketing/catalog fields: dietary tags, category,
        /// allergen info, badges and the seasonal availability window.
        /// </summary>
        public void UpdateCatalogInfo(
            string tags,
            string category,
            string allergenInfo,
            bool isNew,
            bool isSignature,
            DateTime? availableFrom,
            DateTime? availableTo)
        {
            if (Status == ProductStatus.Discontinued)
            {
                throw new DomainException("Cannot update a discontinued product.");
            }

            var normalizedTags = NormalizeTags(tags);
            if (normalizedTags != null && normalizedTags.Length > TagsMaxLength)
            {
                throw new DomainException($"Product tags must not exceed {TagsMaxLength} characters.");
            }

            if (category != null && category.Trim().Length > CategoryMaxLength)
            {
                throw new DomainException($"Product category must not exceed {CategoryMaxLength} characters.");
            }

            if (allergenInfo != null && allergenInfo.Trim().Length > AllergenInfoMaxLength)
            {
                throw new DomainException($"Allergen info must not exceed {AllergenInfoMaxLength} characters.");
            }

            if (availableFrom.HasValue && availableTo.HasValue && availableFrom.Value.Date > availableTo.Value.Date)
            {
                throw new DomainException("AvailableFrom must not be after AvailableTo.");
            }

            Tags = normalizedTags;
            Category = TextValidation.TrimToNull(category);
            AllergenInfo = TextValidation.TrimToNull(allergenInfo);
            IsNew = isNew;
            IsSignature = isSignature;
            AvailableFrom = availableFrom?.Date;
            AvailableTo = availableTo?.Date;
        }

        /// <summary>
        /// True when the product may be shown on the public shop right now:
        /// active status, inside the seasonal window (when one is set) and
        /// inside a daily sale time window (item 164; null = all day).
        /// Hour windows are evaluated in Vietnam time (UTC+7).
        /// </summary>
        public bool IsAvailableNow(DateTime utcNow)
        {
            if (Status != ProductStatus.Active)
            {
                return false;
            }

            var today = utcNow.Date;
            if (AvailableFrom.HasValue && today < AvailableFrom.Value.Date)
            {
                return false;
            }

            if (AvailableTo.HasValue && today > AvailableTo.Value.Date)
            {
                return false;
            }

            // Item 164: daily sale time windows (Vietnam time).
            if (!string.IsNullOrWhiteSpace(AvailableHoursJson))
            {
                var windows = ParseHourWindows(AvailableHoursJson.Trim());
                if (windows.Count > 0)
                {
                    var vnNow = utcNow.AddHours(VietnamUtcOffsetHours).TimeOfDay;
                    var inWindow = false;
                    foreach (var window in windows)
                    {
                        if (vnNow >= window.From && vnNow < window.To)
                        {
                            inWindow = true;
                            break;
                        }
                    }

                    if (!inWindow)
                    {
                        return false;
                    }
                }
                // Invalid JSON fails open (write-time validation prevents it).
            }

            return true;
        }

        /// <summary>
        /// Item 164: human-readable sale windows for badges, e.g. "6h–10h,
        /// 17h–20h". Empty when sold all day.
        /// </summary>
        public string GetSaleWindowsLabel()
        {
            if (string.IsNullOrWhiteSpace(AvailableHoursJson))
            {
                return string.Empty;
            }

            var windows = ParseHourWindows(AvailableHoursJson.Trim());
            var parts = new List<string>();
            foreach (var window in windows)
            {
                parts.Add($"{FormatHour(window.From)}–{FormatHour(window.To)}");
            }

            return string.Join(", ", parts);
        }

        private static string FormatHour(TimeSpan t) =>
            t.Minutes == 0 ? $"{t.Hours}h" : $"{t.Hours}:{t.Minutes:D2}";

        private static readonly char[] TagSeparators = [',', ';'];

        private static string NormalizeTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return null;
            }

            // Dùng field static readonly thay vì new[] mỗi lần gọi — tránh cấp phát
            // mảng mới cho mỗi lần chuẩn hóa tags (CA1861).
            var parts = tags.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();

            return parts.Count == 0 ? null : string.Join(",", parts);
        }

        /// <summary>Ngày tạo (UTC). Null với dữ liệu cũ.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Ngày sửa gần nhất (UTC). Null với dữ liệu cũ.</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
