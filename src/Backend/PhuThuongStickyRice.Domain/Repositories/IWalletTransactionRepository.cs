using PhuThuongStickyRice.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="WalletTransaction"/> — bổ sung thao tác
    /// guard chống hoàn tiền trùng để vá race check-then-insert của
    /// <c>WalletAppService.RefundAsync</c>.
    /// </summary>
    public interface IWalletTransactionRepository : Volo.Abp.Domain.Repositories.IRepository<WalletTransaction, System.Guid>
    {
        /// <summary>
        /// Chèn dòng guard hoàn tiền một cách nguyên tử: chỉ insert khi chưa
        /// có dòng nào cùng <c>IdempotencyKey</c>. Trả về dòng đã chèn nếu
        /// request này là bên đầu tiên ghi nhận hoàn tiền, null nếu đã có
        /// (bên thua race phải return mà KHÔNG cộng tiền vào ví).
        /// Dùng unique index trên IdempotencyKey làm trọng tài: pre-check
        /// <c>AnyAsync</c> cho đường nhanh, catch <c>DbUpdateException</c> cho
        /// trường hợp 2 request cùng qua pre-check — entity lỗi được detach
        /// để UoW hiện tại vẫn dùng được, rồi verify lại dòng của bên thắng.
        /// </summary>
        /// <param name="guard">Dòng giao dịch guard (đã đặt IdempotencyKey).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<WalletTransaction> TryInsertRefundGuardAsync(
            WalletTransaction guard, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật <c>BalanceAfter</c> của dòng guard sau khi số dư ví đã
        /// được cộng nguyên tử. Guard được insert trước với BalanceAfter tạm
        /// (chưa biết số dư mới), nên phải set lại giá trị đúng — nếu không
        /// lịch sử ví sẽ ghi sai số dư sau giao dịch, gây lệch đối soát.
        /// </summary>
        /// <param name="idempotencyKey">Khóa của dòng guard vừa insert.</param>
        /// <param name="balanceAfter">Số dư ví mới sau khi cộng tiền hoàn.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task UpdateRefundBalanceAfterAsync(
            string idempotencyKey, decimal balanceAfter, CancellationToken cancellationToken = default);
    }
}
