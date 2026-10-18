using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// App-wide multilingual service for the PUBLIC web chrome (landing header/nav,
    /// shared footer, common labels). Default language is Vietnamese; supports
    /// English, Chinese (Simplified) and Japanese. Persists in localStorage under
    /// "pt-lang". Full page-body content translation is a separate follow-up —
    /// this service covers chrome keys only.
    /// </summary>
    public class AppI18n
    {
        public const string StorageKey = "pt-lang";

        public static readonly string[] SupportedLangs = { "vi", "en", "zh", "ja" };
        public const string DefaultLang = "vi";

        private readonly IJSRuntime _jsRuntime;
        private string _lang = DefaultLang;
        private bool _loaded;

        public AppI18n(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action OnChange;

        public string CurrentLang => _lang;

        public bool IsVietnamese => _lang == "vi";

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
                if (IsSupported(saved))
                {
                    _lang = saved;
                }
            }
            catch (Exception)
            {
                // JS interop unavailable (prerender) — stay Vietnamese.
            }
        }

        public async Task SetLangAsync(string lang)
        {
            var next = IsSupported(lang) ? lang : DefaultLang;
            if (next == _lang)
            {
                return;
            }

            _lang = next;
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, _lang);
            }
            catch (Exception)
            {
            }

            OnChange?.Invoke();
        }

        public static bool IsSupported(string lang)
        {
            return lang == "vi" || lang == "en" || lang == "zh" || lang == "ja";
        }

        /// <summary>
        /// Translate a chrome key. Falls back to Vietnamese, then to the key itself.
        /// </summary>
        public string T(string key)
        {
            if (_strings.TryGetValue(_lang, out var dict)
                && dict.TryGetValue(key, out var value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (_strings["vi"].TryGetValue(key, out var viValue))
            {
                return viValue;
            }

            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> _strings =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["vi"] = new Dictionary<string, string>
                {
                    // Navigation
                    ["nav.home"] = "Trang chủ",
                    ["nav.about"] = "Giới thiệu",
                    ["nav.menu"] = "Thực đơn",
                    ["nav.signature"] = "Món đặc trưng",
                    ["nav.bulk_order"] = "Đặt số lượng lớn",
                    ["nav.party_order"] = "Đặt tiệc",
                    ["nav.franchise"] = "Nhượng quyền",
                    ["nav.contact"] = "Liên hệ",
                    ["nav.order_now"] = "Đặt xôi ngay",
                    ["nav.view_menu"] = "Xem thực đơn",
                    // Common
                    ["common.login"] = "Đăng nhập",
                    ["common.account"] = "Tài khoản",
                    ["common.cart"] = "Giỏ hàng",
                    ["common.checkout"] = "Thanh toán",
                    ["common.search"] = "Tìm kiếm",
                    ["common.loading"] = "Đang tải...",
                    ["common.close"] = "Đóng",
                    ["common.cancel"] = "Hủy",
                    ["common.confirm"] = "Xác nhận",
                    ["common.save"] = "Lưu",
                    ["common.delete"] = "Xóa",
                    ["common.edit"] = "Sửa",
                    ["common.add"] = "Thêm",
                    ["common.phone"] = "Số điện thoại",
                    ["common.name"] = "Họ tên",
                    ["common.address"] = "Địa chỉ",
                    ["common.email"] = "Email",
                    ["common.price"] = "Giá",
                    ["common.quantity"] = "Số lượng",
                    ["common.total"] = "Tổng cộng",
                    ["common.order"] = "Đơn hàng",
                    ["common.status"] = "Trạng thái",
                    // Footer
                    ["footer.about_text"] = "Gói xôi nóng hổi mỗi sáng từ làng nghề Phú Thượng — dẻo thơm, đúng vị Hà Nội.",
                    ["footer.explore"] = "Khám phá",
                    ["footer.contact_us"] = "Liên hệ",
                    ["footer.faq"] = "Câu hỏi thường gặp",
                    ["footer.copyright"] = "© 2026 Xôi Phú Thượng — Thổi hồn vào từng gói xôi",
                    // Shop
                    ["shop.add_to_cart"] = "Thêm vào giỏ",
                    ["shop.sold_out"] = "Tạm hết",
                    ["shop.best_seller"] = "Bán chạy",
                    ["shop.combo"] = "Combo",
                    ["shop.order_online"] = "Đặt xôi online",
                },
                ["en"] = new Dictionary<string, string>
                {
                    ["nav.home"] = "Home",
                    ["nav.about"] = "About",
                    ["nav.menu"] = "Menu",
                    ["nav.signature"] = "Signature",
                    ["nav.bulk_order"] = "Bulk order",
                    ["nav.party_order"] = "Party order",
                    ["nav.franchise"] = "Franchise",
                    ["nav.contact"] = "Contact",
                    ["nav.order_now"] = "Order now",
                    ["nav.view_menu"] = "View menu",
                    ["common.login"] = "Log in",
                    ["common.account"] = "Account",
                    ["common.cart"] = "Cart",
                    ["common.checkout"] = "Checkout",
                    ["common.search"] = "Search",
                    ["common.loading"] = "Loading...",
                    ["common.close"] = "Close",
                    ["common.cancel"] = "Cancel",
                    ["common.confirm"] = "Confirm",
                    ["common.save"] = "Save",
                    ["common.delete"] = "Delete",
                    ["common.edit"] = "Edit",
                    ["common.add"] = "Add",
                    ["common.phone"] = "Phone number",
                    ["common.name"] = "Full name",
                    ["common.address"] = "Address",
                    ["common.email"] = "Email",
                    ["common.price"] = "Price",
                    ["common.quantity"] = "Quantity",
                    ["common.total"] = "Total",
                    ["common.order"] = "Order",
                    ["common.status"] = "Status",
                    ["footer.about_text"] = "Hot sticky rice every morning from Phú Thượng craft village — fragrant, authentic Hanoi taste.",
                    ["footer.explore"] = "Explore",
                    ["footer.contact_us"] = "Contact us",
                    ["footer.faq"] = "FAQ",
                    ["footer.copyright"] = "© 2026 Phu Thuong Sticky Rice — Soul in every pack",
                    ["shop.add_to_cart"] = "Add to cart",
                    ["shop.sold_out"] = "Sold out",
                    ["shop.best_seller"] = "Best seller",
                    ["shop.combo"] = "Combo",
                    ["shop.order_online"] = "Order online",
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["nav.home"] = "首页",
                    ["nav.about"] = "关于我们",
                    ["nav.menu"] = "菜单",
                    ["nav.signature"] = "招牌菜",
                    ["nav.bulk_order"] = "大量订购",
                    ["nav.party_order"] = "宴席预订",
                    ["nav.franchise"] = "加盟合作",
                    ["nav.contact"] = "联系我们",
                    ["nav.order_now"] = "立即下单",
                    ["nav.view_menu"] = "查看菜单",
                    ["common.login"] = "登录",
                    ["common.account"] = "账户",
                    ["common.cart"] = "购物车",
                    ["common.checkout"] = "结算",
                    ["common.search"] = "搜索",
                    ["common.loading"] = "加载中...",
                    ["common.close"] = "关闭",
                    ["common.cancel"] = "取消",
                    ["common.confirm"] = "确认",
                    ["common.save"] = "保存",
                    ["common.delete"] = "删除",
                    ["common.edit"] = "编辑",
                    ["common.add"] = "添加",
                    ["common.phone"] = "电话",
                    ["common.name"] = "姓名",
                    ["common.address"] = "地址",
                    ["common.email"] = "邮箱",
                    ["common.price"] = "价格",
                    ["common.quantity"] = "数量",
                    ["common.total"] = "总计",
                    ["common.order"] = "订单",
                    ["common.status"] = "状态",
                    ["footer.about_text"] = "每天清晨来自富寿传统村的热糯米饭——香软可口,地道的河内风味。",
                    ["footer.explore"] = "探索",
                    ["footer.contact_us"] = "联系我们",
                    ["footer.faq"] = "常见问题",
                    ["footer.copyright"] = "© 2026 富寿糯米饭 — 用心做好每一份",
                    ["shop.add_to_cart"] = "加入购物车",
                    ["shop.sold_out"] = "已售罄",
                    ["shop.best_seller"] = "畅销",
                    ["shop.combo"] = "套餐",
                    ["shop.order_online"] = "在线下单",
                },
                ["ja"] = new Dictionary<string, string>
                {
                    ["nav.home"] = "ホーム",
                    ["nav.about"] = "当店について",
                    ["nav.menu"] = "メニュー",
                    ["nav.signature"] = "看板メニュー",
                    ["nav.bulk_order"] = "大量注文",
                    ["nav.party_order"] = "宴会予約",
                    ["nav.franchise"] = "フランチャイズ",
                    ["nav.contact"] = "お問い合わせ",
                    ["nav.order_now"] = "今すぐ注文",
                    ["nav.view_menu"] = "メニューを見る",
                    ["common.login"] = "ログイン",
                    ["common.account"] = "アカウント",
                    ["common.cart"] = "カート",
                    ["common.checkout"] = "お会計",
                    ["common.search"] = "検索",
                    ["common.loading"] = "読み込み中...",
                    ["common.close"] = "閉じる",
                    ["common.cancel"] = "キャンセル",
                    ["common.confirm"] = "確認",
                    ["common.save"] = "保存",
                    ["common.delete"] = "削除",
                    ["common.edit"] = "編集",
                    ["common.add"] = "追加",
                    ["common.phone"] = "電話番号",
                    ["common.name"] = "氏名",
                    ["common.address"] = "住所",
                    ["common.email"] = "メール",
                    ["common.price"] = "価格",
                    ["common.quantity"] = "数量",
                    ["common.total"] = "合計",
                    ["common.order"] = "注文",
                    ["common.status"] = "ステータス",
                    ["footer.about_text"] = "毎朝プートゥオンの伝統村から届く温かいおこわ——香り豊かで本場ハノイの味。",
                    ["footer.explore"] = "探す",
                    ["footer.contact_us"] = "お問い合わせ",
                    ["footer.faq"] = "よくある質問",
                    ["footer.copyright"] = "© 2026 プートゥオンおこわ — 一つひとつに心を込めて",
                    ["shop.add_to_cart"] = "カートに入れる",
                    ["shop.sold_out"] = "売り切れ",
                    ["shop.best_seller"] = "人気商品",
                    ["shop.combo"] = "セット",
                    ["shop.order_online"] = "オンライン注文",
                },
            };
    }
}
