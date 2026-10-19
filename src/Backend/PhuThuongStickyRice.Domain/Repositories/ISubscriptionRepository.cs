using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Domain.Repositories
{
    public interface ISubscriptionRepository : Volo.Abp.Domain.Repositories.IRepository<Subscription, Guid>
    {
        /// <summary>
        /// Giành quyền sinh đơn định kỳ cho một gói một cách nguyên tử:
        /// UPDATE LastOrderDate = today WHERE Id + Active + (LastOrderDate IS
        /// NULL OR LastOrderDate &lt; today). Trả về true nếu giành được,
        /// false nếu gói không đến hạn hoặc đã bị lần chạy khác giành trước —
        /// 2 lần chạy <c>CreateDueOrdersAsync</c> đồng thời (worker 6h sáng +
        /// admin bấm tay) không thể cùng sinh 2 đơn COD cùng ngày cho 1 gói.
        /// Chạy trong DbContext/UoW hiện tại nên tham gia cùng transaction
        /// với phần còn lại của nghiệp vụ.
        /// </summary>
        /// <param name="id">Id gói đăng ký.</param>
        /// <param name="today">Ngày VN hiện tại (chuẩn so sánh "đến hạn").</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryClaimDueAsync(Guid id, DateTime today, CancellationToken cancellationToken = default);
    }
}
