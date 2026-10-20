using PhuThuongStickyRice.Web.Components.Core.Services;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Từ điển VI/EN/ZH/JA gọn nhẹ chỉ cho phần khung các trang SHOP (tiêu đề/nút của
    /// trang shop, giỏ hàng, thanh toán — không dịch tên hay mô tả món ăn).
    /// Ngôn ngữ đang dùng được lưu trong localStorage.
    /// </summary>
    public class ShopLocalizationService
    {
        private const string StorageKey = "phuthuong.lang";

        private readonly IJSRuntime _jsRuntime;
        private string _language = "vi";
        private bool _loaded;

        public ShopLocalizationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action OnChange;

        /// <summary>Mã ngôn ngữ đang dùng ("vi", "en", "zh" hoặc "ja").</summary>
        public string Language => _language;

        /// <summary>true khi ngôn ngữ hiện tại là tiếng Anh.</summary>
        public bool IsEnglish => _language == "en";

        /// <summary>
        /// Đọc ngôn ngữ đã lưu trong localStorage (gọi 1 lần lúc khởi động).
        /// Ưu tiên khóa dùng chung toàn site ("pt-lang" của
        /// <see cref="AppLocalizationService"/>) để khách chọn 中文/日本語 ở bộ
        /// chọn ngôn ngữ là khung shop cũng đổi theo; khóa riêng cũ
        /// "phuthuong.lang" chỉ còn là dự phòng cho trình duyệt đã lưu từ trước.
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
                var saved = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AppLocalizationService.StorageKey);
                if (!IsSupported(saved))
                {
                    saved = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                }

                if (IsSupported(saved))
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
        /// Đổi ngôn ngữ và lưu vào localStorage; mã không thuộc nhóm hỗ trợ
        /// (vi/en/zh/ja của <see cref="AppLocalizationService"/>) → về tiếng Việt.
        /// </summary>
        public async Task SetLanguageAsync(string language)
        {
            var next = IsSupported(language) ? language : "vi";
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

        /// <summary>true khi mã thuộc nhóm ngôn ngữ site hỗ trợ (dùng chung danh sách với AppLocalizationService).</summary>
        private static bool IsSupported(string language)
        {
            return language != null && AppLocalizationService.SupportedLanguages.Contains(language);
        }

        /// <summary>
        /// Dịch một khóa theo ngôn ngữ hiện tại; thiếu bản dịch thì trả về chính khóa.
        /// </summary>
        public string Translate(string key)
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
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["shop_title"] = "订糯米饭",
                    ["shop_subtitle"] = "富上糯米饭——每日清晨软糯飘香",
                    ["add_to_cart"] = "加入购物车",
                    ["cart"] = "购物车",
                    ["cart_empty"] = "购物车是空的。",
                    ["cart_browse"] = "查看菜单",
                    ["checkout"] = "下单",
                    ["continue_shopping"] = "继续购物",
                    ["total"] = "总计",
                    ["subtotal"] = "小计",
                    ["delivery_fee"] = "配送费",
                    ["discount"] = "折扣",
                    ["order_now"] = "立即下单",
                    ["your_info"] = "您的信息",
                    ["full_name"] = "姓名",
                    ["phone"] = "电话号码",
                    ["address"] = "收货地址",
                    ["note"] = "备注",
                    ["payment_method"] = "付款方式",
                    ["pay_cod"] = "货到付款（现金）",
                    ["pay_bank"] = "VietQR 银行转账",
                    ["pay_momo"] = "MoMo 钱包",
                    ["pay_wallet"] = "用钱包付款",
                    ["order_success"] = "下单成功！",
                    ["track_order"] = "查询订单",
                    ["search"] = "搜索",
                    ["all"] = "全部",
                    ["quantity"] = "数量",
                    ["remove"] = "删除",
                    ["update"] = "更新",
                    ["close"] = "关闭",
                    ["confirm"] = "确认",
                    ["back"] = "返回",
                    ["sold_today"] = "今日已售",
                    ["best_sellers"] = "热销菜品",
                    ["table_order"] = "正在餐桌点餐",
                    ["checkin"] = "📅 今日签到",
                    ["checkin_done"] = "今天已签到",
                    ["streak"] = "连续天数",
                    ["gallery"] = "顾客照片",
                    ["share_photo"] = "分享您收到的菜品照片",
                    ["loading"] = "加载中...",
                },
                ["ja"] = new Dictionary<string, string>
                {
                    ["shop_title"] = "もち米ごはんを注文",
                    ["shop_subtitle"] = "フートゥオンのもち米ごはん — 毎朝もちもち",
                    ["add_to_cart"] = "カートに追加",
                    ["cart"] = "カート",
                    ["cart_empty"] = "カートは空です。",
                    ["cart_browse"] = "メニューを見る",
                    ["checkout"] = "注文する",
                    ["continue_shopping"] = "買い物を続ける",
                    ["total"] = "合計",
                    ["subtotal"] = "小計",
                    ["delivery_fee"] = "配送料",
                    ["discount"] = "割引",
                    ["order_now"] = "今すぐ注文",
                    ["your_info"] = "お客様情報",
                    ["full_name"] = "氏名",
                    ["phone"] = "電話番号",
                    ["address"] = "お届け先住所",
                    ["note"] = "備考",
                    ["payment_method"] = "お支払い方法",
                    ["pay_cod"] = "代金引換（現金）",
                    ["pay_bank"] = "VietQR 銀行振込",
                    ["pay_momo"] = "MoMoウォレット",
                    ["pay_wallet"] = "ウォレットで支払う",
                    ["order_success"] = "ご注文が完了しました！",
                    ["track_order"] = "注文を追跡",
                    ["search"] = "検索",
                    ["all"] = "すべて",
                    ["quantity"] = "数量",
                    ["remove"] = "削除",
                    ["update"] = "更新",
                    ["close"] = "閉じる",
                    ["confirm"] = "確認",
                    ["back"] = "戻る",
                    ["sold_today"] = "本日の販売数",
                    ["best_sellers"] = "人気メニュー",
                    ["table_order"] = "テーブルで注文中",
                    ["checkin"] = "📅 本日のチェックイン",
                    ["checkin_done"] = "本日チェックイン済み",
                    ["streak"] = "連続日数",
                    ["gallery"] = "お客様の写真",
                    ["share_photo"] = "受け取った料理の写真を共有",
                    ["loading"] = "読み込み中...",
                }
            };
    }
}
