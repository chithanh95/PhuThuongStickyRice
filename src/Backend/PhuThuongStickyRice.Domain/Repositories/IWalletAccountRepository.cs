using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="WalletAccount"/>.
    /// </summary>
    public interface IWalletAccountRepository : Volo.Abp.Domain.Repositories.IRepository<WalletAccount, Guid>
    {
        /// <summary>
        /// Trừ số dư ví một cách nguyên tử: UPDATE ... WHERE Balance &gt;= amount.
        /// Trả về true nếu trừ thành công, false nếu số dư không đủ tại thời điểm
        /// UPDATE (2 request đồng thời không thể cùng qua bước check rồi trừ —
        /// chống race check-then-set gây số dư âm).
        /// </summary>
        /// <param name="accountId">Id tài khoản ví.</param>
        /// <param name="amount">Số tiền cần trừ (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryDecrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default);
    }
}
