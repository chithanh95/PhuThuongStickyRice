using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Một dòng hàng trong <see cref="Order"/>. Thuộc aggregate Order —
    /// chỉ aggregate root Order mới được tạo/sửa dòng này.
    /// </summary>
    public class OrderLine : Entity<Guid>
    {
        /// <summary>Độ dài tối đa cột tên size trong DB.</summary>
        public const int SizeNameMaxLength = 100;
        /// <summary>Độ dài tối đa cột tên topping (gộp bằng dấu phẩy) trong DB.</summary>
        public const int ToppingNamesMaxLength = 500;
        /// <summary>Độ dài tối đa cột ghi chú khách trong DB.</summary>
        public const int NoteMaxLength = 500;
        /// <summary>Độ dài tối đa cột danh sách id topping (CSV các Guid) trong DB.</summary>
        public const int ToppingIdsMaxLength = 1000;

        /// <summary>Id đơn hàng chứa dòng này.</summary>
        public Guid OrderId { get; private set; }

        /// <summary>
        /// Product id — or the combo id when <see cref="IsCombo"/> is true.
        /// There is no FK constraint so combos (which are not products)
        /// can be referenced here.
        /// </summary>
        public Guid ProductId { get; private set; }

        /// <summary>Tên sản phẩm tại thời điểm đặt (chụp lại, không lấy theo catalog sau này).</summary>
        public string ProductName { get; private set; }

        /// <summary>Đơn giá một phần (đã gồm giá size/topping), dùng <see cref="Money"/> để giữ nguyên đơn vị tiền tệ.</summary>
        public Money UnitPrice { get; private set; }

        /// <summary>Số lượng đặt.</summary>
        public int Quantity { get; private set; }

        /// <summary>Thành tiền dòng = <see cref="UnitPrice"/> × <see cref="Quantity"/>; tự tính lại khi đổi số lượng.</summary>
        public Money LineTotal { get; private set; }

        /// <summary>Chosen size option name (Nhỏ / Vừa / Lớn), if any.</summary>
        public string SizeName { get; private set; }

        /// <summary>
        /// Id của size đã chọn — lưu kèm tên để luồng "đặt lại" khôi phục
        /// đúng size theo id thay vì đoán theo tên (tên size có thể bị sửa
        /// sau này). Null khi món không chọn size hoặc đơn cũ chưa có cột này.
        /// </summary>
        public Guid? SizeOptionId { get; private set; }

        /// <summary>Chosen topping names, comma-separated, if any.</summary>
        public string ToppingNames { get; private set; }

        /// <summary>
        /// Id các topping đã chọn, dạng CSV Guid ("guid,guid,...") — lưu kèm
        /// tên để luồng "đặt lại" khôi phục đúng topping theo id. Null khi
        /// không chọn topping hoặc đơn cũ chưa có cột này.
        /// </summary>
        public string ToppingIds { get; private set; }

        /// <summary>Total topping surcharge for one unit, in the line currency.</summary>
        public decimal ToppingsExtra { get; private set; }

        /// <summary>True when this line is a family combo, not a product.</summary>
        public bool IsCombo { get; private set; }

        /// <summary>
        /// Per-item note from the customer (e.g. "ít hành", "không ớt").
        /// Rendered on the kitchen sticky labels when present.
        /// </summary>
        public string Note { get; private set; }

        // EF Core
        private OrderLine()
        {
        }

        private OrderLine(
            Guid productId,
            string productName,
            Money unitPrice,
            int quantity,
            string sizeName,
            string toppingNames,
            decimal toppingsExtra,
            bool isCombo,
            string note = null,
            Guid? sizeOptionId = null,
            string toppingIds = null)
        {
            // Phải gán Id ngay tại đây: khóa Id cấu hình default SQL
            // "newsequentialid()" nên EF không sinh key phía client — mọi
            // dòng mới đều Id = Guid.Empty, khi gắn graph đơn vào DbContext
            // dòng thứ 2 trở đi trùng key với dòng đầu và EF ném
            // InvalidOperationException "another instance with the same key
            // value for {'Id'} is already being tracked" (user gặp 2026-10-21
            // khi đặt đơn nhiều món). EF vẫn ghi đè Id bằng giá trị DB khi
            // nạp đơn cũ nên gán ở đây không ảnh hưởng đọc dữ liệu.
            Id = Guid.NewGuid();
            ProductId = productId;
            ProductName = productName;
            UnitPrice = unitPrice;
            Quantity = quantity;
            LineTotal = Money.Of(unitPrice.Amount * quantity, unitPrice.Currency);
            SizeName = sizeName;
            ToppingNames = toppingNames;
            ToppingsExtra = toppingsExtra;
            IsCombo = isCombo;
            Note = TextValidation.TrimToNull(note);
            SizeOptionId = sizeOptionId;
            ToppingIds = TextValidation.TrimToNull(toppingIds);
        }

        /// <summary>
        /// Factory duy nhất để tạo dòng hàng — ép toàn bộ bất biến domain trước
        /// khi khởi tạo: id sản phẩm khác rỗng, có tên, có đơn giá, số lượng ≥ 1,
        /// phí topping không âm và độ dài text trong giới hạn DB. Setter private
        /// nên code ngoài aggregate Order không thể bỏ qua kiểm tra này.
        /// </summary>
        internal static OrderLine Create(
            Guid productId,
            string productName,
            Money unitPrice,
            int quantity,
            string sizeName = null,
            string toppingNames = null,
            decimal toppingsExtra = 0,
            bool isCombo = false,
            string note = null,
            Guid? sizeOptionId = null,
            IEnumerable<Guid> toppingIds = null)
        {
            if (productId == Guid.Empty)
            {
                throw new DomainException("Order line must reference a product.");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new DomainException("Order line product name must not be empty.");
            }

            if (unitPrice == null)
            {
                throw new DomainException("Order line unit price is required.");
            }

            if (quantity < 1)
            {
                throw new DomainException("Order line quantity must be at least 1.");
            }

            if (toppingsExtra < 0)
            {
                throw new DomainException("Toppings extra must not be negative.");
            }

            // [codequality-smell 2026-10-20] Kiểm tra độ dài gom về
            // TextValidation dùng chung với Order — giữ nguyên kiểu đo độ
            // dài thô (Length, không trim) như bản cũ.
            TextValidation.MaxLength(sizeName, SizeNameMaxLength, "Size name", trimBeforeCheck: false);
            TextValidation.MaxLength(toppingNames, ToppingNamesMaxLength, "Topping names", trimBeforeCheck: false);
            TextValidation.MaxLength(note, NoteMaxLength, "Order line note", trimBeforeCheck: false);

            // Id topping -> CSV để lưu 1 cột: bỏ Guid.Empty và trùng lặp,
            // rỗng thì lưu null như các cột snapshot khác.
            var toppingIdsCsv = toppingIds == null
                ? null
                : string.Join(",", toppingIds.Where(x => x != Guid.Empty).Distinct());
            if (string.IsNullOrEmpty(toppingIdsCsv))
            {
                toppingIdsCsv = null;
            }
            TextValidation.MaxLength(toppingIdsCsv, ToppingIdsMaxLength, "Topping ids", trimBeforeCheck: false);

            return new OrderLine(
                productId,
                productName.Trim(),
                unitPrice,
                quantity,
                TextValidation.TrimToNull(sizeName),
                TextValidation.TrimToNull(toppingNames),
                toppingsExtra,
                isCombo,
                note,
                sizeOptionId,
                toppingIdsCsv);
        }

        /// <summary>
        /// Phân tích <see cref="ToppingIds"/> (CSV Guid) về danh sách id —
        /// dùng khi đặt lại đơn cũ để khôi phục đúng topping đã chọn, và khi
        /// gộp đơn (dòng được copy sang đơn chính phải mang theo id).
        /// </summary>
        /// <returns>Danh sách id topping; rỗng khi dòng không có topping.</returns>
        public List<Guid> GetToppingIds()
        {
            if (string.IsNullOrWhiteSpace(ToppingIds))
            {
                return new List<Guid>();
            }

            return ToppingIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty)
                .Where(x => x != Guid.Empty)
                .ToList();
        }

        /// <summary>
        /// Quantity edit by the customer on a Pending order (0 = remove line).
        /// Called from the Order aggregate root only.
        /// </summary>
        internal void SetQuantity(int quantity)
        {
            if (quantity < 0)
            {
                throw new DomainException("Order line quantity cannot be negative.");
            }

            Quantity = quantity;
            LineTotal = Money.Of(UnitPrice.Amount * quantity, UnitPrice.Currency);
        }
    }
}
