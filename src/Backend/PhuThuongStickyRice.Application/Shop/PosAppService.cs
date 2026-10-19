using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;

using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Shop
{
    /// <summary>
    /// Hợp đồng POS tại quầy (Item 127): tạo đơn mua mang về/thanh toán ngay
    /// cho khách không đặt online. Đơn POS đi COD, nhận hàng ngay, thanh toán
    /// tiền mặt và hoàn tất trong 1 lần gọi — bắt buộc đi qua từng trạng thái
    /// (Confirmed → Preparing → Delivering → Completed) để không mất audit,
    /// webhook và điểm loyalty. Khách vãng lai không để lại SĐT dùng SĐT
    /// placeholder "0000000000" (loyalty bỏ qua để điểm không dồn vào 1 pool chung).
    /// </summary>
    public interface IPosAppService : IApplicationService
    {
        /// <summary>
        /// Creates a walk-in POS order: COD, pickup, paid immediately and
        /// Completed in one step (loyalty points still accrue on completion).
        /// </summary>
        Task<OrderDto> CreateOrderAsync(CreatePosOrderInput input);
    }

    /// <summary>Item 127: counter POS for Admin/Cashier.</summary>
    [Authorize(Roles = "Admin,Cashier")]
    public class PosAppService : ApplicationService, IPosAppService
    {
        /// <summary>Thông báo lỗi khi POS chưa chọn món nào (dùng ở 2 điểm validate).</summary>
        private const string NoItemsSelectedMessage = "POS: chưa chọn món nào.";

        /// <summary>Tên hiển thị cho khách vãng lai không để lại tên.</summary>
        private const string WalkInCustomerName = "Khách vãng lai";
        /// <summary>SĐT placeholder cho khách vãng lai không để lại SĐT (loyalty bỏ qua để điểm không dồn vào 1 pool chung).</summary>
        private const string MissingPhonePlaceholder = "0000000000";
        /// <summary>Phương thức thanh toán của đơn POS: tiền mặt tại quầy.</summary>
        private const string PosPaymentMethod = "Cod";
        /// <summary>Cách nhận hàng của đơn POS: khách lấy tại quầy.</summary>
        private const string PosFulfillmentMethod = "Pickup";
        /// <summary>Nguồn đơn cho đơn tạo tại quầy POS.</summary>
        private const string PosOrderSource = "POS";
        /// <summary>
        /// Chuỗi trạng thái đơn POS đi qua sau khi thu tiền. Không set thẳng
        /// Completed: mỗi bước chuyển đều ghi audit/log nhân viên/webhook, và
        /// chỉ Completed mới cộng điểm loyalty/chạy quy tắc thưởng.
        /// </summary>
        private static readonly string[] PosStatusPipeline = { "Confirmed", "Preparing", "Delivering", "Completed" };

        private readonly IOrderAppService _orders;

        public PosAppService(IOrderAppService orders)
        {
            _orders = orders;
        }

        /// <summary>
        /// Tạo đơn POS cho khách mua tại quầy: thanh toán tiền mặt, nhận hàng
        /// ngay, đơn đi thẳng từ mới tạo đến Hoàn tất trong 1 lần gọi.
        /// BẮT BUỘC đi qua từng trạng thái (Confirmed → Preparing → Delivering →
        /// Completed) thay vì set thẳng Completed — vì domain chỉ cho Completed
        /// từ Delivering, và mỗi bước chuyển đều ghi audit lịch sử trạng thái,
        /// log hoạt động nhân viên và bắn webhook; chỉ bước Completed mới cộng
        /// điểm loyalty và chạy quy tắc thưởng. Nhảy cóc sẽ throw và mất side effect.
        /// SĐT "0000000000" là placeholder cho khách vãng lai không để lại SĐT —
        /// loyalty bỏ qua SĐT này để điểm các đơn vãng lai không dồn vào 1 pool chung.
        /// Bỏ qua reCAPTCHA vì đây là luồng nội bộ của nhân viên, không có token trình duyệt.
        /// </summary>
        /// <param name="input">Tên/SĐT khách (trống thì lấy "Khách vãng lai"), ghi chú và danh sách món.</param>
        /// <returns>Đơn hàng đã hoàn tất.</returns>
        public virtual async Task<OrderDto> CreateOrderAsync(CreatePosOrderInput input)
        {
            if (input?.Lines == null || input.Lines.Count == 0)
            {
                throw new DomainException(NoItemsSelectedMessage);
            }

            var phone = VietnamTime.NormalizePhone(input.CustomerPhone);
            var order = await _orders.CreateAsync(new CreateOrderDto
            {
                CustomerName = string.IsNullOrWhiteSpace(input.CustomerName)
                    ? WalkInCustomerName
                    : input.CustomerName.Trim(),
                CustomerPhone = phone ?? MissingPhonePlaceholder,
                CustomerAddress = null,
                Notes = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
                PaymentMethod = PosPaymentMethod,
                FulfillmentMethod = PosFulfillmentMethod,
                Source = PosOrderSource,
                Lines = input.Lines
                    .Where(x => x.Quantity > 0)
                    .Select(x => new CreateOrderLineDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        SizeOptionId = x.SizeOptionId,
                        ToppingIds = x.ToppingIds ?? new List<System.Guid>()
                    })
                    .ToList()
            }, skipRecaptchaCheck: true);

            if (order.Lines == null || order.Lines.Count == 0)
            {
                throw new DomainException(NoItemsSelectedMessage);
            }

            // Walk-in: cash collected at the counter, order fulfilled immediately.
            // Đi từng bước (không set thẳng Completed): mỗi UpdateStatusAsync
            // ghi audit + log nhân viên + webhook, và chỉ Completed mới cộng
            // điểm loyalty/chạy quy tắc thưởng (xem doc của method).
            await _orders.ConfirmPaymentAsync(order.Id);
            foreach (var status in PosStatusPipeline)
            {
                order = await _orders.UpdateStatusAsync(order.Id, new UpdateOrderStatusDto { Status = status });
            }

            return order;
        }
    }
}
