
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.AuditLogs;

namespace PhuThuongStickyRice.Application.AuditLogs
{
    /// <summary>Item 99: login history (Admin).</summary>
    public interface ILoginAuditAppService : IApplicationService
    {
        Task<PagedResultDto<LoginAuditDto>> GetListAsync(int skipCount = 0, int maxResultCount = 50);

        /// <summary>
        /// Internal: records a login attempt. Called by the auth controller.
        /// </summary>
        Task RecordAsync(string userName, string ipAddress, bool success, string failureReason = null);

        /// <summary>
        /// True when this IP has never been seen on a successful login for
        /// this user (the current attempt is not yet recorded).
        /// </summary>
        Task<bool> IsNewIpForUserAsync(string userName, string ipAddress);
    }

    [Authorize(Roles = "Admin")]
    public class LoginAuditAppService : ApplicationService, ILoginAuditAppService
    {
        private readonly IRepository<LoginAudit, Guid> _audits;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public LoginAuditAppService(
            IRepository<LoginAudit, Guid> audits,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _audits = audits;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<PagedResultDto<LoginAuditDto>> GetListAsync(int skipCount = 0, int maxResultCount = 50)
        {
            var queryable = await _audits.GetQueryableAsync();
            var query = queryable.OrderByDescending(x => x.At);
            var total = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(
                query.Skip(Math.Max(0, skipCount)).Take(Math.Clamp(maxResultCount, 1, 200)));
            return new PagedResultDto<LoginAuditDto>(
                total,
                items.Select(x => new LoginAuditDto
                {
                    Id = x.Id,
                    UserName = x.UserName,
                    At = x.At,
                    IpAddress = x.IpAddress,
                    IsSuccess = x.Success,
                    FailureReason = x.FailureReason
                }).ToList());
        }

        /// <summary>Internal — no Admin policy so the login flow can call it.</summary>
        [AllowAnonymous]
        public virtual async Task RecordAsync(string userName, string ipAddress, bool success, string failureReason = null)
        {
            try
            {
                await _audits.InsertAsync(new LoginAudit
                {
                    UserName = (userName ?? "?").Trim(),
                    At = DateTime.UtcNow,
                    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
                    Success = success,
                    FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim()
                }, autoSave: true);
            }
            catch (Exception ex)
            {
                // Login auditing must never break the login flow.
                Logger.LogWarning(ex, "Failed to record login audit for {UserName}.", userName);
            }
        }

        /// <summary>
        /// True when this IP has never been seen on a successful login for
        /// this user (the current attempt is not yet recorded).
        /// </summary>
        [AllowAnonymous]
        public virtual async Task<bool> IsNewIpForUserAsync(string userName, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            var normalized = userName.Trim();
            var queryable = await _audits.GetQueryableAsync();
            return !await _asyncExecuter.AnyAsync(
                queryable.Where(x => x.UserName == normalized && x.Success && x.IpAddress == ipAddress));
        }
    }
}
