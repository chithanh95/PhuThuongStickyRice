using PhuThuongStickyRice.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="PrepaidCard"/> — bổ sung thao tác trừ
    /// suất nguyên tử để chống race khi 2 lượt trừ suất đồng thời.
    /// </summary>
    public interface IPrepaidCardRepository : Volo.Abp.Domain.Repositories.IRepository<PrepaidCard, System.Guid>
    {
        /// <summary>
        /// Trừ 1 suất khỏi thẻ một cách nguyên tử: UPDATE ... SET UsedSuits =
        /// UsedSuits + 1 WHERE CardCode = cardCode AND Status = Active AND
        /// UsedSuits &lt; TotalSuits. Trả về true nếu trừ thành công, false nếu
        /// không trừ được (không có thẻ Active với mã này, hoặc thẻ đã hết
        /// suất tại thời điểm UPDATE). Chống race read-modify-write: bản cũ
        /// đọc UsedSuits vào memory rồi ++ và UpdateAsync — 2 request trừ suất
        /// đồng thời (nhân viên double-click, retry mạng, 2 quầy) cùng đọc
        /// UsedSuits = N, cùng ghi N+1 → lost update: phục vụ 2 suất nhưng chỉ
        /// trừ 1 (khách được ăn thêm suất miễn phí).
        /// </summary>
        /// <param name="cardCode">Mã thẻ đã chuẩn hóa (trim + upper).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryRedeemAsync(string cardCode, CancellationToken cancellationToken = default);
    }
}
