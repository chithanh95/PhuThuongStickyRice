using PhuThuongStickyRice.Application.Wave5.DTOs;
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

namespace PhuThuongStickyRice.Application.Wave5
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

        /// <summary>Internal: credit the wallet back (order cancelled).</summary>
        Task RefundAsync(string phone, decimal amount, string orderNumber);
    }

    /// <summary>Item 140: customer wallet.</summary>
    public class WalletAppService : ApplicationService, IWalletAppService
    {
        private readonly IRepository<WalletAccount, Guid> _wallets;
        private readonly IRepository<WalletTransaction, Guid> _transactions;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public WalletAppService(
            IRepository<WalletAccount, Guid> wallets,
            IRepository<WalletTransaction, Guid> transactions,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _wallets = wallets;
            _transactions = transactions;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public async Task<WalletAccountDto> GetBalanceAsync(string phone)
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

        [Authorize(Roles = "Admin")]
        public async Task<List<WalletAccountDto>> GetListAsync(string searchTerm)
        {
            var queryable = await _wallets.GetQueryableAsync();
            IQueryable<WalletAccount> query = queryable.OrderByDescending(x => x.Balance);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = new string(searchTerm.Where(char.IsDigit).ToArray());
                query = query.Where(x => x.Phone.Contains(term));
            }

            var list = await _asyncExecuter.ToListAsync(query);
            return list.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<WalletAccountDto> AdjustAsync(AdjustWalletInput input)
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

        [Authorize(Roles = "Admin")]
        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(string phone)
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

        public async Task ChargeAsync(string phone, decimal amount, string orderNumber)
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

        public async Task RefundAsync(string phone, decimal amount, string orderNumber)
        {
            var normalized = VietnamTime.NormalizePhone(phone);
            if (string.IsNullOrEmpty(normalized) || amount <= 0)
            {
                return;
            }

            var account = await GetOrCreateAsync(normalized);
            account.Balance += amount;
            account.UpdatedAt = DateTime.UtcNow;
            await _wallets.UpdateAsync(account, autoSave: true);

            await LogAsync(normalized, amount, account.Balance,
                $"Hoàn tiền hủy đơn {orderNumber}", "system");
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
