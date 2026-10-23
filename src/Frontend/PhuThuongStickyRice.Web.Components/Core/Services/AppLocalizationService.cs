using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>
    /// Service đa ngôn ngữ dùng chung cho phần khung web công khai (header/nav trang chủ,
    /// footer dùng chung, các nhãn chung). Ngôn ngữ mặc định là tiếng Việt; hỗ trợ thêm
    /// tiếng Anh, tiếng Trung (giản thể) và tiếng Nhật. Lưu lựa chọn trong localStorage
    /// dưới khóa "pt-lang". Service này chỉ dịch phần khung trang — dịch toàn bộ nội dung
    /// trang là việc riêng trong tương lai.
    /// </summary>
    public class AppLocalizationService
    {
        public const string StorageKey = "pt-lang";

        /// <summary>Các mã ngôn ngữ được hỗ trợ (vi/en/zh/ja).</summary>
        public static readonly string[] SupportedLanguages = { "vi", "en", "zh", "ja" };

        /// <summary>Mã ngôn ngữ mặc định (tiếng Việt).</summary>
        public const string DefaultLanguage = "vi";

        private readonly IJSRuntime _jsRuntime;
        private string _language = DefaultLanguage;
        private bool _loaded;

        public AppLocalizationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action OnChange;

        /// <summary>Mã ngôn ngữ đang dùng (vi/en/zh/ja).</summary>
        public string CurrentLanguage => _language;

        /// <summary>true khi ngôn ngữ hiện tại là tiếng Việt.</summary>
        public bool IsVietnamese => _language == "vi";

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
        /// Đổi ngôn ngữ và lưu vào localStorage; mã không hỗ trợ → về tiếng Việt.
        /// </summary>
        public async Task SetLanguageAsync(string language)
        {
            var next = IsSupported(language) ? language : DefaultLanguage;
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

        /// <summary>true khi mã ngôn ngữ nằm trong <see cref="SupportedLanguages"/>.</summary>
        public static bool IsSupported(string language)
        {
            // Dùng chung mảng SupportedLanguages thay vì hardcode "vi"/"en"/"zh"/"ja"
            // — thêm ngôn ngữ mới vào SupportedLanguages thì hàm này tự nhận.
            return SupportedLanguages.Contains(language);
        }

        /// <summary>
        /// Dịch một khóa chrome. Trả về bản dịch theo ngôn ngữ hiện tại,
        /// fallback sang tiếng Việt rồi tới chính khóa khi thiếu bản dịch.
        /// </summary>
        public string Translate(string key)
        {
            if (_strings.TryGetValue(_language, out var dict)
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
                    ["nav.track_order"] = "Tra cứu đơn hàng",
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
                    ["order_status.pending"] = "Chờ xác nhận",
                    ["order_status.confirmed"] = "Đã xác nhận",
                    ["order_status.preparing"] = "Đang chuẩn bị",
                    ["order_status.delivering"] = "Đang giao",
                    ["order_status.completed"] = "Hoàn thành",
                    ["order_status.cancelled"] = "Đã hủy",
                    // Footer
                    ["footer.about_text"] = "Gói xôi nóng hổi mỗi sáng từ làng nghề Phú Thượng — dẻo thơm, đúng vị Hà Nội.",
                    ["footer.explore"] = "Khám phá",
                    ["footer.contact_us"] = "Liên hệ",
                    ["footer.faq"] = "Câu hỏi thường gặp",
                    ["footer.copyright"] = "© 2026 Xôi Huyền — Thổi hồn vào từng gói xôi",
                    // Shop
                    ["shop.add_to_cart"] = "Thêm vào giỏ",
                    ["shop.sold_out"] = "Tạm hết",
                    ["shop.best_seller"] = "Bán chạy",
                    ["shop.combo"] = "Combo",
                    ["shop.order_online"] = "Đặt xôi online",
                    ["shop.cart_empty"] = "Giỏ hàng đang trống.",
                    ["shop.cart_browse"] = "Xem thực đơn",
                    ["shop.continue_shopping"] = "Tiếp tục mua",
                    ["shop.share_cart"] = "🔗 Chia sẻ giỏ hàng",
                    ["shop.creating_link"] = "Đang tạo link...",
                    ["shop.product"] = "Sản phẩm",
                    ["shop.unit_price"] = "Đơn giá",
                    ["shop.line_total"] = "Thành tiền",
                    ["shop.toppings_label"] = "Ăn kèm",
                    ["shop.item_note_placeholder"] = "📝 Ghi chú món (vd: ít hành)",
                    ["shop.suggested_sides"] = "🍢 Thường được mua cùng",
                    ["shop.add"] = "+ Thêm",
                    ["shop.subtotal"] = "Tạm tính",
                    ["shop.discount"] = "Giảm giá",
                    ["shop.your_info"] = "Thông tin của bạn",
                    ["shop.note"] = "Ghi chú",
                    ["shop.address"] = "Địa chỉ giao hàng",
                    ["shop.payment_method"] = "Hình thức thanh toán",
                    ["shop.pay_cod"] = "Tiền mặt khi nhận hàng",
                    ["shop.pay_bank"] = "Chuyển khoản QR (VietQR)",
                    ["shop.pay_momo"] = "Ví MoMo",
                    ["shop.pay_wallet"] = "Thanh toán bằng ví",
                    ["shop.order_now"] = "Đặt hàng ngay",
                    ["shop.processing"] = "Đang xử lý...",
                    ["shop.order_summary"] = "Đơn hàng của bạn",
                    ["shop.discount_code"] = "Mã giảm giá",
                    ["shop.recipient"] = "Người nhận",
                    ["shop.other_recipient"] = "Đặt hộ",
                    ["shop.track_title"] = "Tra cứu đơn hàng",
                    ["shop.order_number"] = "Mã đơn hàng",
                    ["shop.order_phone"] = "Số điện thoại đặt hàng",
                    ["shop.lookup"] = "Tra cứu",
                    ["shop.looking_up"] = "Đang tra cứu...",
                    ["shop.order_history"] = "📋 Lịch sử đơn hàng",
                    ["shop.history_hint"] = "Nhập số điện thoại đã dùng để đặt hàng để xem toàn bộ đơn cũ và đặt lại 1 chạm.",
                    ["shop.view_history"] = "Xem lịch sử",
                    ["shop.no_orders"] = "Số này chưa có đơn hàng nào.",
                    ["shop.order_date"] = "Ngày đặt",
                    ["shop.order_total"] = "Tổng tiền",
                    ["shop.detail"] = "Chi tiết",
                    ["shop.reorder"] = "🔄 Đặt lại",
                    ["shop.reordering"] = "Đang khôi phục...",
                    ["shop.logout"] = "Đăng xuất",
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
                    ["nav.track_order"] = "Track order",
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
                    ["order_status.pending"] = "Pending",
                    ["order_status.confirmed"] = "Confirmed",
                    ["order_status.preparing"] = "Preparing",
                    ["order_status.delivering"] = "Delivering",
                    ["order_status.completed"] = "Completed",
                    ["order_status.cancelled"] = "Cancelled",
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
                    ["shop.cart_empty"] = "Your cart is empty.",
                    ["shop.cart_browse"] = "Browse menu",
                    ["shop.continue_shopping"] = "Continue shopping",
                    ["shop.share_cart"] = "🔗 Share cart",
                    ["shop.creating_link"] = "Creating link...",
                    ["shop.product"] = "Product",
                    ["shop.unit_price"] = "Unit price",
                    ["shop.line_total"] = "Amount",
                    ["shop.toppings_label"] = "Sides",
                    ["shop.item_note_placeholder"] = "📝 Note (e.g. less onion)",
                    ["shop.suggested_sides"] = "🍢 Frequently bought together",
                    ["shop.add"] = "+ Add",
                    ["shop.subtotal"] = "Subtotal",
                    ["shop.discount"] = "Discount",
                    ["shop.your_info"] = "Your information",
                    ["shop.note"] = "Note",
                    ["shop.address"] = "Delivery address",
                    ["shop.payment_method"] = "Payment method",
                    ["shop.pay_cod"] = "Cash on pickup",
                    ["shop.pay_bank"] = "Bank transfer (VietQR)",
                    ["shop.pay_momo"] = "MoMo wallet",
                    ["shop.pay_wallet"] = "Pay with wallet",
                    ["shop.order_now"] = "Place order",
                    ["shop.processing"] = "Processing...",
                    ["shop.order_summary"] = "Your order",
                    ["shop.discount_code"] = "Discount code",
                    ["shop.recipient"] = "Recipient",
                    ["shop.other_recipient"] = "Different recipient",
                    ["shop.track_title"] = "Track order",
                    ["shop.order_number"] = "Order number",
                    ["shop.order_phone"] = "Order phone number",
                    ["shop.lookup"] = "Track",
                    ["shop.looking_up"] = "Tracking...",
                    ["shop.order_history"] = "📋 Order history",
                    ["shop.history_hint"] = "Enter the phone number you ordered with to see past orders and reorder in one tap.",
                    ["shop.view_history"] = "View history",
                    ["shop.no_orders"] = "No orders found for this number.",
                    ["shop.order_date"] = "Order date",
                    ["shop.order_total"] = "Total",
                    ["shop.detail"] = "Details",
                    ["shop.reorder"] = "🔄 Reorder",
                    ["shop.reordering"] = "Restoring...",
                    ["shop.logout"] = "Log out",
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
                    ["nav.track_order"] = "查询订单",
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
                    ["order_status.pending"] = "待确认",
                    ["order_status.confirmed"] = "已确认",
                    ["order_status.preparing"] = "准备中",
                    ["order_status.delivering"] = "配送中",
                    ["order_status.completed"] = "已完成",
                    ["order_status.cancelled"] = "已取消",
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
                    ["shop.cart_empty"] = "购物车是空的。",
                    ["shop.cart_browse"] = "浏览菜单",
                    ["shop.continue_shopping"] = "继续购物",
                    ["shop.share_cart"] = "🔗 分享购物车",
                    ["shop.creating_link"] = "正在生成链接...",
                    ["shop.product"] = "商品",
                    ["shop.unit_price"] = "单价",
                    ["shop.line_total"] = "金额",
                    ["shop.toppings_label"] = "配菜",
                    ["shop.item_note_placeholder"] = "📝 备注（例如：少葱）",
                    ["shop.suggested_sides"] = "🍢 经常一起购买",
                    ["shop.add"] = "+ 添加",
                    ["shop.subtotal"] = "小计",
                    ["shop.discount"] = "折扣",
                    ["shop.your_info"] = "您的信息",
                    ["shop.note"] = "备注",
                    ["shop.address"] = "收货地址",
                    ["shop.payment_method"] = "支付方式",
                    ["shop.pay_cod"] = "取货时付现金",
                    ["shop.pay_bank"] = "银行转账 (VietQR)",
                    ["shop.pay_momo"] = "MoMo钱包",
                    ["shop.pay_wallet"] = "钱包支付",
                    ["shop.order_now"] = "提交订单",
                    ["shop.processing"] = "处理中...",
                    ["shop.order_summary"] = "您的订单",
                    ["shop.discount_code"] = "优惠码",
                    ["shop.recipient"] = "收件人",
                    ["shop.other_recipient"] = "其他收件人",
                    ["shop.track_title"] = "订单查询",
                    ["shop.order_number"] = "订单号",
                    ["shop.order_phone"] = "下单手机号",
                    ["shop.lookup"] = "查询",
                    ["shop.looking_up"] = "查询中...",
                    ["shop.order_history"] = "📋 历史订单",
                    ["shop.history_hint"] = "输入下单时使用的手机号，查看历史订单并一键重新下单。",
                    ["shop.view_history"] = "查看历史",
                    ["shop.no_orders"] = "该号码暂无订单。",
                    ["shop.order_date"] = "下单日期",
                    ["shop.order_total"] = "总金额",
                    ["shop.detail"] = "详情",
                    ["shop.reorder"] = "🔄 重新下单",
                    ["shop.reordering"] = "恢复中...",
                    ["shop.logout"] = "退出登录",
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
                    ["nav.track_order"] = "注文を追跡",
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
                    ["order_status.pending"] = "確認待ち",
                    ["order_status.confirmed"] = "確認済み",
                    ["order_status.preparing"] = "準備中",
                    ["order_status.delivering"] = "配達中",
                    ["order_status.completed"] = "完了",
                    ["order_status.cancelled"] = "キャンセル済み",
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
                    ["shop.cart_empty"] = "カートは空です。",
                    ["shop.cart_browse"] = "メニューを見る",
                    ["shop.continue_shopping"] = "買い物を続ける",
                    ["shop.share_cart"] = "🔗 カートを共有",
                    ["shop.creating_link"] = "リンクを生成中...",
                    ["shop.product"] = "商品",
                    ["shop.unit_price"] = "単価",
                    ["shop.line_total"] = "金額",
                    ["shop.toppings_label"] = "付け合わせ",
                    ["shop.item_note_placeholder"] = "📝 備考（例：ネギ少なめ）",
                    ["shop.suggested_sides"] = "🍢 よく一緒に購入される",
                    ["shop.add"] = "+ 追加",
                    ["shop.subtotal"] = "小計",
                    ["shop.discount"] = "割引",
                    ["shop.your_info"] = "お客様情報",
                    ["shop.note"] = "備考",
                    ["shop.address"] = "お届け先住所",
                    ["shop.payment_method"] = "お支払い方法",
                    ["shop.pay_cod"] = "受取時の現金払い",
                    ["shop.pay_bank"] = "銀行振込 (VietQR)",
                    ["shop.pay_momo"] = "MoMoウォレット",
                    ["shop.pay_wallet"] = "ウォレットで支払う",
                    ["shop.order_now"] = "注文する",
                    ["shop.processing"] = "処理中...",
                    ["shop.order_summary"] = "ご注文内容",
                    ["shop.discount_code"] = "割引コード",
                    ["shop.recipient"] = "受取人",
                    ["shop.other_recipient"] = "別の受取人",
                    ["shop.track_title"] = "注文状況の確認",
                    ["shop.order_number"] = "注文番号",
                    ["shop.order_phone"] = "注文時の電話番号",
                    ["shop.lookup"] = "確認する",
                    ["shop.looking_up"] = "確認中...",
                    ["shop.order_history"] = "📋 注文履歴",
                    ["shop.history_hint"] = "注文時に使用した電話番号を入力すると、過去の注文を確認してワンタップで再注文できます。",
                    ["shop.view_history"] = "履歴を見る",
                    ["shop.no_orders"] = "この番号の注文はありません。",
                    ["shop.order_date"] = "注文日",
                    ["shop.order_total"] = "合計",
                    ["shop.detail"] = "詳細",
                    ["shop.reorder"] = "🔄 再注文",
                    ["shop.reordering"] = "復元中...",
                    ["shop.logout"] = "ログアウト",
                },
            };
    }
}
