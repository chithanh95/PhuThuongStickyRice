using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Item 134: tiny VI/EN dictionary for the SHOP pages' chrome only
    /// (shop / cart / checkout headings and buttons — never product names
    /// or descriptions). The language persists in localStorage.
    /// </summary>
    public class ShopI18n
    {
        private const string StorageKey = "phuthuong.lang";

        private readonly IJSRuntime _jsRuntime;
        private string _language = "vi";
        private bool _loaded;

        public ShopI18n(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action OnChange;

        /// <summary>Mã ngôn ngữ đang dùng ("vi" hoặc "en").</summary>
        public string Language => _language;

        /// <summary>true khi ngôn ngữ hiện tại là tiếng Anh.</summary>
        public bool IsEnglish => _language == "en";

        /// <summary>
        /// Đọc ngôn ngữ đã lưu trong localStorage (gọi 1 lần lúc khởi động).
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            try
            {
                var saved = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                if (saved == "en" || saved == "vi")
                {
                    _language = saved;
                }
            }
            catch (Exception)
            {
                // JS interop unavailable (prerender) — stay Vietnamese.
            }
        }

        /// <summary>
        /// Đổi ngôn ngữ và lưu vào localStorage; mã không phải "en" → về tiếng Việt.
        /// </summary>
        public async Task SetLanguageAsync(string language)
        {
            var next = language == "en" ? "en" : "vi";
            if (next == _language)
            {
                return;
            }

            _language = next;
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, _language);
            }
            catch (Exception)
            {
                // localStorage unavailable (prerender/private mode) — language applies in-memory only.
            }

            OnChange?.Invoke();
        }

        /// <summary>Translate a chrome key; falls back to the key itself.</summary>
        public string T(string key)
        {
            if (_strings.TryGetValue(_language, out var dict)
                && dict.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> _strings =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["vi"] = new Dictionary<string, string>
                {
                    ["shop_title"] = "Đặt xôi",
                    ["shop_subtitle"] = "Xôi Phú Thượng — dẻo thơm mỗi sáng",
                    ["add_to_cart"] = "Thêm vào giỏ",
                    ["cart"] = "Giỏ hàng",
                    ["cart_empty"] = "Giỏ hàng đang trống.",
                    ["cart_browse"] = "Xem thực đơn",
                    ["checkout"] = "Đặt hàng",
                    ["continue_shopping"] = "Tiếp tục mua",
                    ["total"] = "Tổng cộng",
                    ["subtotal"] = "Tạm tính",
                    ["delivery_fee"] = "Phí giao hàng",
                    ["discount"] = "Giảm giá",
                    ["order_now"] = "Đặt hàng ngay",
                    ["your_info"] = "Thông tin của bạn",
                    ["full_name"] = "Họ tên",
                    ["phone"] = "Số điện thoại",
                    ["address"] = "Địa chỉ giao hàng",
                    ["note"] = "Ghi chú",
                    ["payment_method"] = "Hình thức thanh toán",
                    ["pay_cod"] = "Tiền mặt khi nhận hàng",
                    ["pay_bank"] = "Chuyển khoản QR (VietQR)",
                    ["pay_momo"] = "Ví MoMo",
                    ["pay_wallet"] = "Thanh toán bằng ví",
                    ["order_success"] = "Đặt hàng thành công!",
                    ["track_order"] = "Tra cứu đơn hàng",
                    ["search"] = "Tìm kiếm",
                    ["all"] = "Tất cả",
                    ["quantity"] = "Số lượng",
                    ["remove"] = "Xóa",
                    ["update"] = "Cập nhật",
                    ["close"] = "Đóng",
                    ["confirm"] = "Xác nhận",
                    ["back"] = "Quay lại",
                    ["sold_today"] = "Đã bán hôm nay",
                    ["best_sellers"] = "Món bán chạy",
                    ["table_order"] = "Đang đặt tại bàn",
                    ["checkin"] = "📅 Điểm danh hôm nay",
                    ["checkin_done"] = "Đã điểm danh hôm nay",
                    ["streak"] = "Chuỗi ngày liên tiếp",
                    ["gallery"] = "Ảnh khách hàng",
                    ["share_photo"] = "Chia sẻ ảnh món bạn đã nhận",
                    ["loading"] = "Đang tải...",
                },
                ["en"] = new Dictionary<string, string>
                {
                    ["shop_title"] = "Order sticky rice",
                    ["shop_subtitle"] = "Phú Thượng sticky rice — fresh every morning",
                    ["add_to_cart"] = "Add to cart",
                    ["cart"] = "Cart",
                    ["cart_empty"] = "Your cart is empty.",
                    ["cart_browse"] = "Browse menu",
                    ["checkout"] = "Checkout",
                    ["continue_shopping"] = "Continue shopping",
                    ["total"] = "Total",
                    ["subtotal"] = "Subtotal",
                    ["delivery_fee"] = "Delivery fee",
                    ["discount"] = "Discount",
                    ["order_now"] = "Place order",
                    ["your_info"] = "Your information",
                    ["full_name"] = "Full name",
                    ["phone"] = "Phone number",
                    ["address"] = "Delivery address",
                    ["note"] = "Note",
                    ["payment_method"] = "Payment method",
                    ["pay_cod"] = "Cash on delivery",
                    ["pay_bank"] = "VietQR bank transfer",
                    ["pay_momo"] = "MoMo wallet",
                    ["pay_wallet"] = "Pay with wallet",
                    ["order_success"] = "Order placed!",
                    ["track_order"] = "Track order",
                    ["search"] = "Search",
                    ["all"] = "All",
                    ["quantity"] = "Quantity",
                    ["remove"] = "Remove",
                    ["update"] = "Update",
                    ["close"] = "Close",
                    ["confirm"] = "Confirm",
                    ["back"] = "Back",
                    ["sold_today"] = "Sold today",
                    ["best_sellers"] = "Best sellers",
                    ["table_order"] = "Ordering at table",
                    ["checkin"] = "📅 Check in today",
                    ["checkin_done"] = "Checked in today",
                    ["streak"] = "Day streak",
                    ["gallery"] = "Customer photos",
                    ["share_photo"] = "Share a photo of your dish",
                    ["loading"] = "Loading...",
                }
            };
    }
}
