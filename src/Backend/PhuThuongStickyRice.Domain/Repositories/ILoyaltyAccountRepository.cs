using PhuThuongStickyRice.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="LoyaltyAccount"/> — bổ sung thao tác trừ
    /// điểm nguyên tử để chống race khi đổi điểm đồng thời.
    /// </summary>
    public interface ILoyaltyAccountRepository : Volo.Abp.Domain.Repositories.IRepository<LoyaltyAccount, System.Guid>
    {
        /// <summary>
        /// Trừ điểm loyalty một cách nguyên tử: UPDATE ... WHERE Phone = phone
        /// AND Points &gt;= points. Trả về true nếu trừ thành công, false nếu
        /// không đủ điểm tại thời điểm UPDATE (2 request đổi điểm đồng thời
        /// cùng SĐT không thể cùng qua bước check rồi trừ — chống race
        /// check-then-set gây trừ điểm 2 lần / giảm giá 2 lần cho 1 lượt đổi).
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa (khớp cột Phone).</param>
        /// <param name="points">Số điểm cần trừ (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryDecrementPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default);
    }
}
