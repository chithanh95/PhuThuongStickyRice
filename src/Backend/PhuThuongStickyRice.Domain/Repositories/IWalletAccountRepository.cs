#nullable enable annotations // Task<WalletAccount?> ở dưới cần nullable annotations context (CS8632)
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
        /// Trả về số dư MỚI sau khi trừ (đọc lại sau UPDATE), hoặc null nếu số dư
        /// không đủ tại thời điểm UPDATE (2 request đồng thời không thể cùng qua
        /// bước check rồi trừ — chống race check-then-set gây số dư âm).
        /// Dùng giá trị trả về để ghi đúng BalanceAfter vào lịch sử: bản cũ trả
        /// bool nên caller phải tự tính balanceCũ - amount, sai khi 2 trừ tiền
        /// đồng thời cùng ví (bug-hunt 2026-10-20).
        /// </summary>
        /// <param name="accountId">Id tài khoản ví.</param>
        /// <param name="amount">Số tiền cần trừ (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<decimal?> TryDecrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cộng số dư ví một cách nguyên tử: UPDATE Balance = Balance + amount.
        /// Trả về số dư mới sau khi cộng. Dùng cho hoàn tiền: RefundAsync trước
        /// đây đọc-modify-ghi (account.Balance += amount rồi UpdateAsync) nên
        /// 2 refund đồng thời, hoặc refund chạy song song với ChargeAsync
        /// (đã nguyên tử), có thể làm mất phần cộng của nhau / ghi đè số dư
        /// sai — lỗi tiền thật. ExecuteUpdateAsync chạy 1 câu UPDATE nên không
        /// bị race.
        /// </summary>
        /// <param name="accountId">Id tài khoản ví.</param>
        /// <param name="amount">Số tiền cần cộng (phải &gt; 0).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<decimal> IncrementBalanceAsync(
            Guid accountId, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo ví mới một cách nguyên tử: trả về ví vừa tạo, hoặc null nếu SĐT
        /// đã có ví (bên khác thắng race). Chống race check-then-insert —
        /// 2 request đồng thời cho SĐT chưa từng có ví đều qua bước kiểm tra
        /// rồi cùng insert: unique (Phone) chỉ cho 1 bên thắng, bên thua cũ
        /// nhận DbUpdateException -&gt; 500. Pattern theo sẵn LoyaltyAccountRepository.
        /// </summary>
        /// <param name="account">Ví cần tạo (Phone đã chuẩn hóa).</param>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>Ví vừa tạo, hoặc null khi SĐT đã có ví (bên khác thắng race).</returns>
        Task<WalletAccount?> TryInsertAsync(
            WalletAccount account, CancellationToken cancellationToken = default);
    }
}
