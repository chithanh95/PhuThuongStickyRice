using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Discounts
{
    public interface IDiscountAppService : IApplicationService
    {
        Task<List<DiscountCodeDto>> GetListAsync();

        Task<DiscountCodeDto> GetAsync(Guid id);

        Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto);

        Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto);

        Task DeleteAsync(Guid id);

        /// <summary>Public: checks a code against a cart subtotal.</summary>
        Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input);

        /// <summary>
        /// Order creation: returns the discount amount and bumps the usage
        /// counter. Throws DomainException when the code cannot be applied.
        /// </summary>
        Task<decimal> ApplyAsync(string code, decimal subtotal);
    }

    public class DiscountAppService : ApplicationService, IDiscountAppService
    {
        private readonly IRepository<DiscountCode, Guid> _discounts;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public DiscountAppService(
            IRepository<DiscountCode, Guid> discounts,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _discounts = discounts;
            _asyncExecuter = asyncExecuter;
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<DiscountCodeDto>> GetListAsync()
        {
            var queryable = await _discounts.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.CreatedDateTime));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> GetAsync(Guid id)
        {
            var entity = await _discounts.GetAsync(id);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> CreateAsync(CreateDiscountCodeDto dto)
        {
            var code = NormalizeCode(dto.Code);
            await EnsureCodeUniqueAsync(code, null);
            EnsureValueProvided(dto);

            var entity = new DiscountCode
            {
                Code = code,
                DiscountPercent = dto.DiscountPercent,
                DiscountAmount = dto.DiscountAmount,
                MinOrderAmount = Math.Max(0, dto.MinOrderAmount),
                ExpiresAt = dto.ExpiresAt,
                MaxUses = dto.MaxUses,
                UsedCount = 0,
                IsActive = dto.IsActive
            };
            await _discounts.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<DiscountCodeDto> UpdateAsync(Guid id, CreateDiscountCodeDto dto)
        {
            var entity = await _discounts.GetAsync(id);
            var code = NormalizeCode(dto.Code);
            await EnsureCodeUniqueAsync(code, id);
            EnsureValueProvided(dto);

            entity.Code = code;
            entity.DiscountPercent = dto.DiscountPercent;
            entity.DiscountAmount = dto.DiscountAmount;
            entity.MinOrderAmount = Math.Max(0, dto.MinOrderAmount);
            entity.ExpiresAt = dto.ExpiresAt;
            entity.MaxUses = dto.MaxUses;
            entity.IsActive = dto.IsActive;
            await _discounts.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task DeleteAsync(Guid id)
        {
            await _discounts.DeleteAsync(id, autoSave: true);
        }

        [AllowAnonymous]
        public async Task<ValidateDiscountResultDto> ValidateAsync(ValidateDiscountInput input)
        {
            var code = NormalizeCode(input.Code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == code));

            var (valid, message) = CheckUsable(entity, input.Subtotal);
            return new ValidateDiscountResultDto
            {
                Valid = valid,
                Message = message,
                DiscountAmount = valid ? ComputeDiscount(entity, input.Subtotal) : 0
            };
        }

        /// <summary>
        /// Used by order creation: returns the discount amount and bumps the
        /// usage counter. Throws <see cref="Domain.Exceptions.DomainException"/>
        /// when the code cannot be applied.
        /// </summary>
        public async Task<decimal> ApplyAsync(string code, decimal subtotal)
        {
            var normalized = NormalizeCode(code);
            var queryable = await _discounts.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));

            var (valid, message) = CheckUsable(entity, subtotal);
            if (!valid)
            {
                throw new Domain.Exceptions.DomainException(message);
            }

            var amount = ComputeDiscount(entity, subtotal);
            entity.UsedCount += 1;
            await _discounts.UpdateAsync(entity, autoSave: true);
            return amount;
        }

        private static (bool Valid, string Message) CheckUsable(DiscountCode entity, decimal subtotal)
        {
            if (entity == null)
            {
                return (false, "Mã giảm giá không tồn tại.");
            }

            if (!entity.IsActive)
            {
                return (false, "Mã giảm giá đã bị tắt.");
            }

            if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < DateTime.UtcNow)
            {
                return (false, "Mã giảm giá đã hết hạn.");
            }

            if (entity.MaxUses.HasValue && entity.UsedCount >= entity.MaxUses.Value)
            {
                return (false, "Mã giảm giá đã hết lượt sử dụng.");
            }

            if (subtotal < entity.MinOrderAmount)
            {
                return (false, $"Mã này áp dụng cho đơn từ {entity.MinOrderAmount:N0} ₫ trở lên.");
            }

            return (true, "Áp dụng mã giảm giá thành công.");
        }

        private static decimal ComputeDiscount(DiscountCode entity, decimal subtotal)
        {
            decimal amount;
            if (entity.DiscountPercent.HasValue)
            {
                var percent = Math.Min(100, Math.Max(0, entity.DiscountPercent.Value));
                amount = Math.Round(subtotal * percent / 100, 0);
            }
            else
            {
                amount = entity.DiscountAmount ?? 0;
            }

            return Math.Min(Math.Max(0, amount), subtotal);
        }

        private static void EnsureValueProvided(CreateDiscountCodeDto dto)
        {
            if (!dto.DiscountPercent.HasValue && !dto.DiscountAmount.HasValue)
            {
                throw new Domain.Exceptions.DomainException(
                    "Phải nhập phần trăm giảm hoặc số tiền giảm.");
            }
        }

        private static string NormalizeCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Domain.Exceptions.DomainException("Mã giảm giá không được để trống.");
            }

            return code.Trim().ToUpperInvariant();
        }

        private async Task EnsureCodeUniqueAsync(string code, Guid? excludeId)
        {
            var queryable = await _discounts.GetQueryableAsync();
            var exists = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == code && x.Id != excludeId));
            if (exists != null)
            {
                throw new Domain.Exceptions.DomainException($"Mã '{code}' đã tồn tại.");
            }
        }

        private static DiscountCodeDto MapToDto(DiscountCode entity)
        {
            return new DiscountCodeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                DiscountPercent = entity.DiscountPercent,
                DiscountAmount = entity.DiscountAmount,
                MinOrderAmount = entity.MinOrderAmount,
                ExpiresAt = entity.ExpiresAt,
                MaxUses = entity.MaxUses,
                UsedCount = entity.UsedCount,
                IsActive = entity.IsActive
            };
        }
    }
}
