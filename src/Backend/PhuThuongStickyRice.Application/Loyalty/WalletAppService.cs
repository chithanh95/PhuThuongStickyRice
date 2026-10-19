
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Loyalty
{
    public interface IWalletAppService : IApplicationService
    {
        /// <summary>Public: balance for a phone (checkout shows the ví option when ≥ total).</summary>
        Task<WalletAccountDto> GetBalanceAsync(string phone);

        /// <summary>Admin: all wallets, optional phone search.</summary>
        Task<List<WalletAccountDto>> GetListAsync(string searchTerm);

        /// <summary>Admin: top-up (positive) or manual deduct (negative) with a reason.</summary>
        Task<WalletAccountDto> AdjustAsync(AdjustWalletInput input);

        /// <summary>Admin: transaction log for a phone.</summary>
        Task<List<WalletTransactionDto>> GetTransactionsAsync(string phone);

        /// <summary>Internal: charge the wallet for a new order. Throws when insufficient.</summary>
        Task ChargeAsync(string phone, decimal amount, string orderNumber);

        /// <summary>
        /// Internal: credit the wallet back (order cancelled).
        /// <para>
        /// Cộng lại tiền vào ví. Idempotent theo cặp (SĐT, lý do):
        /// hủy đơn và bớt món phải dùng lý do khác nhau — nếu dùng chung,
        /// lần hoàn từng phần (bớt món) sẽ chặn nhầm lần hoàn tiền khi hủy
        /// đơn (khách mất số tiền còn lại). Webhook hủy gọi lặp vẫn an toàn
        /// vì cùng lý do hủy đơn cho cùng mã đơn chỉ hoàn đúng 1 lần.
        /// </para>
        /// </summary>
        Task RefundAsync(string phone, decimal amount, string orderNumber, string refundReason = null);
    }

    /// <summary>
    /// Item 140: ví tiền của khách hàng (định danh bằng SĐT, không cần tài khoản đăng nhập).
    /// Ví dùng để thanh toán đơn hàng khi số dư đủ và được hoàn tiền khi đơn bị hủy.
    /// Mọi biến động số dư đều ghi lại vào <see cref="WalletTransaction"/> để đối soát.
    /// </summary>
    public class WalletAppService : ApplicationService, IWalletAppService
    {
        private readonly IRepository<WalletAccount, Guid> _wallets;
        private readonly IRepository<WalletTransaction, Guid> _transactions;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service ví tiền với repository tài khoản và lịch sử giao dịch.</summary>
        public WalletAppService(
            IRepository<WalletAccount, Guid> wallets,
            IRepository<WalletTransaction, Guid> transactions,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _wallets = wallets;
            _transactions = transactions;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Lấy số dư ví theo SĐT (public — trang checkout gọi để hiện lựa chọn "thanh toán bằng ví"
        /// khi số dư đủ). Không yêu cầu đăng nhập để khách vãng lai vẫn kiểm tra được.
        /// </summary>
        /// <param name="phone">SĐT khách hàng (tự chuẩn hóa đầu số Việt Nam).</param>
        /// <returns>DTO số dư; trả ví số dư 0 nếu SĐT chưa từng có ví (thay vì lỗi 404).</returns>
        [AllowAnonymous]
        public virtual async Task<WalletAccountDto> GetBalanceAsync(string phone)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                return new WalletAccountDto { Phone = phone, Balance = 0 };
            }

            var account = await FindAsync(normalized);
            return account == null
                ? new WalletAccountDto { Phone = normalized, Balance = 0 }
                : MapToDto(account);
        }

        /// <summary>
        /// Admin: liệt kê toàn bộ ví tiền, tìm kiếm theo SĐT (chỉ lọc theo chữ số
        /// vì khách có thể nhập kèm dấu cách, dấu chấm).
        /// </summary>
        /// <param name="searchTerm">Chuỗi tìm kiếm SĐT (tùy chọn).</param>
        /// <returns>Danh sách ví sắp xếp theo số dư giảm dần, tối đa 200 dòng.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<WalletAccountDto>> GetListAsync(string searchTerm)
        {
            var queryable = await _wallets.GetQueryableAsync();
            IQueryable<WalletAccount> query = queryable.OrderByDescending(x => x.Balance);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = new string(searchTerm.Where(char.IsDigit).ToArray());
                query = query.Where(x => x.Phone.Contains(term));
            }

            // perf-hunt: cap at 200 like LoyaltyAppService.GetListAsync (admin list only;
            // without Take the whole wallet table is materialized and sorted).
            var list = await _asyncExecuter.ToListAsync(query.Take(200));
            return list.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Admin: cộng (số dương) hoặc trừ tay (số âm) số dư ví kèm lý do.
        /// Số dư không bao giờ được âm — trừ quá số dư hiện có sẽ bị từ chối.
        /// </summary>
        /// <param name="input">SĐT, số tiền điều chỉnh (khác 0) và lý do.</param>
        /// <returns>Ví sau khi điều chỉnh.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<WalletAccountDto> AdjustAsync(AdjustWalletInput input)
        {
            var normalized = VietnamTime.NormalizePhone(input?.Phone);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new DomainException("Số điện thoại không hợp lệ.");
            }

            if (input.Amount == 0)
            {
                throw new DomainException("Số tiền phải khác 0.");
            }

            var account = await GetOrCreateAsync(normalized);
            var newBalance = account.Balance + input.Amount;
            if (newBalance < 0)
            {
                throw new DomainException("Số dư ví không đủ để trừ.");
            }

            account.Balance = newBalance;
            account.UpdatedAt = DateTime.UtcNow;
            await _wallets.UpdateAsync(account, autoSave: true);

            await LogAsync(normalized, input.Amount, newBalance, input.Reason.Trim(),
                CurrentUser?.Email ?? CurrentUser?.UserName ?? "system");

            return MapToDto(account);
        }

        /// <summary>Admin: lịch sử biến động ví của một SĐT (100 giao dịch mới nhất).</summary>
        /// <param name="phone">SĐT cần tra cứu.</param>
        /// <returns>Danh sách giao dịch sắp xếp mới nhất trước.</returns>
        [Authorize(Roles = "Admin")]
        public virtual async Task<List<WalletTransactionDto>> GetTransactionsAsync(string phone)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                return new List<WalletTransactionDto>();
            }

            var queryable = await _transactions.GetQueryableAsync();
            var list = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.Phone == normalized)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(100));
            return list.Select(MapToTransactionDto).ToList();
        }

        /// <summary>
        /// Internal: trừ tiền ví khi tạo đơn hàng mới. Chỉ checkout gọi.
        /// Ném lỗi khi số dư không đủ — đơn sẽ không được tạo, tránh số dư âm.
        /// </summary>
        /// <param name="phone">SĐT chủ ví.</param>
        /// <param name="amount">Số tiền cần trừ (phải &gt; 0).</param>
        /// <param name="orderNumber">Mã đơn để ghi vào lịch sử giao dịch.</param>
        public virtual async Task ChargeAsync(string phone, decimal amount, string orderNumber)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized))
            {
                throw new DomainException("Số điện thoại không hợp lệ.");
            }

            if (amount <= 0)
            {
                throw new DomainException("Số tiền thanh toán không hợp lệ.");
            }

            var account = await GetOrCreateAsync(normalized);
            if (account.Balance < amount)
            {
                throw new DomainException(
                    $"Số dư ví không đủ. Cần {amount:N0} ₫, hiện có {account.Balance:N0} ₫.");
            }

            account.Balance -= amount;
            account.UpdatedAt = DateTime.UtcNow;
            await _wallets.UpdateAsync(account, autoSave: true);

            await LogAsync(normalized, -amount, account.Balance,
                $"Thanh toán đơn {orderNumber}", "system");
        }

        /// <summary>
        /// Internal: cộng lại tiền vào ví khi đơn hàng bị hủy.
        /// Không ném lỗi với input không hợp lệ — hoàn tiền phải idempotent:
        /// webhook hủy đơn có thể gọi lặp, nên input xấu thì bỏ qua thay vì làm vỡ luồng hủy.
        /// </summary>
        /// <param name="phone">SĐT chủ ví.</param>
        /// <param name="amount">Số tiền hoàn lại.</param>
        /// <param name="orderNumber">Mã đơn bị hủy, ghi vào lịch sử giao dịch.</param>
        public virtual async Task RefundAsync(string phone, decimal amount, string orderNumber, string refundReason = null)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized) || amount <= 0)
            {
                return;
            }

            // Idempotent: cùng mã đơn chỉ hoàn đúng 1 lần cho cùng một lý do.
            // Luồng hủy đơn có thể được gọi lặp (webhook gọi lại, customer +
            // staff cùng hủy) — kiểm tra lịch sử giao dịch trước khi cộng tiền
            // để tránh hoàn trùng. Lý do phân biệt loại hoàn tiền: hoàn từng
            // phần khi bớt món không được dùng lý do "hủy đơn", nếu không lần
            // hoàn đó sẽ chặn nhầm lần hoàn tiền thật khi đơn bị hủy sau đó.
            var reason = refundReason ?? $"Hoàn tiền hủy đơn {orderNumber}";
            var txQueryable = await _transactions.GetQueryableAsync();
            var alreadyRefunded = await _asyncExecuter.AnyAsync(
                txQueryable.Where(x => x.Phone == normalized && x.Reason == reason));
            if (alreadyRefunded)
            {
                return;
            }

            var account = await GetOrCreateAsync(normalized);
            account.Balance += amount;
            account.UpdatedAt = DateTime.UtcNow;
            await _wallets.UpdateAsync(account, autoSave: true);

            await LogAsync(normalized, amount, account.Balance, reason, "system");
        }

        private async Task<WalletAccount> FindAsync(string phone)
        {
            var queryable = await _wallets.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable.Where(x => x.Phone == phone));
        }

        private async Task<WalletAccount> GetOrCreateAsync(string phone)
        {
            var account = await FindAsync(phone);
            if (account != null)
            {
                return account;
            }

            account = new WalletAccount
            {
                Phone = phone,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _wallets.InsertAsync(account, autoSave: true);
            return account;
        }

        private async Task LogAsync(string phone, decimal amount, decimal balanceAfter, string reason, string createdBy)
        {
            await _transactions.InsertAsync(new WalletTransaction
            {
                Phone = phone,
                Amount = amount,
                Reason = reason,
                BalanceAfter = balanceAfter,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            }, autoSave: true);
        }

        private static WalletAccountDto MapToDto(WalletAccount x)
        {
            return new WalletAccountDto { Id = x.Id, Phone = x.Phone, Balance = x.Balance };
        }

        private static WalletTransactionDto MapToTransactionDto(WalletTransaction x)
        {
            return new WalletTransactionDto
            {
                Id = x.Id,
                Phone = x.Phone,
                Amount = x.Amount,
                Reason = x.Reason,
                BalanceAfter = x.BalanceAfter,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            };
        }
    }
}
