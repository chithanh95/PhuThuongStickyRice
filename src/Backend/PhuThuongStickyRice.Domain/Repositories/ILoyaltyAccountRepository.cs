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

        /// <summary>
        /// Cộng điểm loyalty một cách nguyên tử: UPDATE ... SET Points =
        /// Points + points, TotalEarned = TotalEarned + points WHERE Phone =
        /// phone. Trả về true nếu cộng thành công (tài khoản tồn tại), false
        /// nếu không tìm thấy tài khoản. Chống race read-modify-write: 2 lần
        /// hoàn điểm đồng thời cùng SĐT (hủy 2 đơn cùng lúc) không thể cùng
        /// đọc số dư cũ rồi ghi đè làm mất phần cộng của nhau.
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa (khớp cột Phone).</param>
        /// <param name="points">Số điểm cần cộng (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryIncrementPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hoàn điểm loyalty một cách nguyên tử: UPDATE ... SET Points =
        /// Points + points WHERE Phone = phone — KHÔNG cộng TotalEarned.
        /// TotalEarned là lịch sử "đã kiếm", chỉ tăng khi khách thật sự kiếm
        /// điểm (mua hàng, điểm danh, giới thiệu...); hoàn điểm khi hủy đơn
        /// là trả lại số dư chứ không phải kiếm mới. Dùng cho refund, không
        /// dùng cho earn.
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa (khớp cột Phone).</param>
        /// <param name="points">Số điểm cần hoàn (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryRefundPointsAsync(
            string phone, int points, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cộng điểm + tổng chi tiêu một cách nguyên tử: UPDATE ... SET
        /// Points = Points + points, TotalEarned = TotalEarned + points,
        /// TotalSpent = TotalSpent + spentAmount WHERE Phone = phone. Trả về
        /// true nếu cộng thành công (tài khoản tồn tại), false nếu không tìm
        /// thấy. Chống race read-modify-write khi 2 đơn của cùng SĐT hoàn
        /// thành đồng thời: bản cũ đọc Points/TotalSpent vào memory rồi +=
        /// và UpdateAsync — bên ghi sau đè mất phần cộng của bên ghi trước
        /// (mất điểm tích lũy / sai tổng chi tiêu tính hạng thành viên).
        /// </summary>
        /// <param name="phone">SĐT đã chuẩn hóa (khớp cột Phone).</param>
        /// <param name="points">Số điểm tích lũy cộng thêm (có thể = 0 khi đơn nhỏ hơn mốc).</param>
        /// <param name="spentAmount">Số tiền đơn hàng cộng vào tổng chi tiêu (₫).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryIncrementEarnedAndSpentAsync(
            string phone, int points, decimal spentAmount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo tài khoản loyalty nguyên tử: chỉ insert khi chưa có tài khoản
        /// cùng Phone. Trả về tài khoản đã tạo, null nếu đã tồn tại — chống
        /// race 2 request đồng thời tạo tài khoản cho SĐT mới (unique index
        /// trên Phone chỉ cho 1 bên insert; bên thua nhận null thay vì
        /// DbUpdateException -> 500). Pre-check <c>AnyAsync</c> cho đường
        /// nhanh, catch <c>DbUpdateException</c> cho trường hợp cùng qua
        /// pre-check — entity lỗi được detach để UoW hiện tại vẫn dùng được.
        /// </summary>
        /// <param name="account">Tài khoản cần tạo (Phone đã chuẩn hóa).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<LoyaltyAccount> TryInsertAsync(
            LoyaltyAccount account, CancellationToken cancellationToken = default);
    }
}
