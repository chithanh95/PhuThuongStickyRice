using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;

using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IGroupOrderAppService : IApplicationService
    {
        /// <summary>Public: creates a group and returns its code.</summary>
        Task<GroupOrderDto> CreateAsync(CreateGroupOrderDto dto);

        /// <summary>Public: anyone with the link can view/add items.</summary>
        Task<GroupOrderDto> GetByCodeAsync(string code);

        /// <summary>Public: add an item to an open group.</summary>
        Task<GroupOrderDto> AddLineAsync(string code, AddGroupOrderLineDto dto);

        /// <summary>
        /// Public: the creator closes the group → a normal COD order is
        /// created from the aggregated lines and the group is closed.
        /// </summary>
        Task<CloseGroupOrderResultDto> CloseAsync(string code, CloseGroupOrderDto dto);
    }

    public class CloseGroupOrderResultDto
    {
        public GroupOrderDto Group { get; set; }
        public string OrderNumber { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class GroupOrderAppService : ApplicationService, IGroupOrderAppService
    {
        private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        private readonly IRepository<GroupOrder, Guid> _groups;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IProductAppService _products;
        private readonly IOrderAppService _orders;
        private readonly IClock _clock;

        public GroupOrderAppService(
            IRepository<GroupOrder, Guid> groups,
            IAsyncQueryableExecuter asyncExecuter,
            IProductAppService products,
            IOrderAppService orders,
            IClock clock)
        {
            _groups = groups;
            _asyncExecuter = asyncExecuter;
            _products = products;
            _orders = orders;
            _clock = clock;
        }

        [AllowAnonymous]
        public virtual async Task<GroupOrderDto> CreateAsync(CreateGroupOrderDto dto)
        {
            var entity = new GroupOrder
            {
                Code = await GenerateUniqueCodeAsync(),
                CreatorPhone = dto.CreatorPhone.Trim(),
                GroupName = string.IsNullOrWhiteSpace(dto.GroupName) ? null : dto.GroupName.Trim(),
                CutoffAt = dto.CutoffAt,
                Status = GroupOrder.StatusOpen,
                LinesJson = "[]",
                CreatedAt = _clock.Now
            };
            await _groups.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [AllowAnonymous]
        public virtual async Task<GroupOrderDto> GetByCodeAsync(string code)
        {
            return MapToDto(await FindByCodeOrThrowAsync(code));
        }

        [AllowAnonymous]
        public virtual async Task<GroupOrderDto> AddLineAsync(string code, AddGroupOrderLineDto dto)
        {
            var entity = await FindByCodeOrThrowAsync(code);
            if (entity.Status != GroupOrder.StatusOpen)
            {
                throw new Domain.Exceptions.DomainException("Nhóm đã chốt đơn, không thể thêm món.");
            }

            ProductDto product;
            try
            {
                product = await _products.GetAsync(dto.ProductId);
            }
            catch (Exception)
            {
                throw new Domain.Exceptions.DomainException("Món ăn không tồn tại.");
            }

            var lines = ParseLines(entity.LinesJson);
            var addedBy = string.IsNullOrWhiteSpace(dto.AddedBy) ? null : dto.AddedBy.Trim();
            var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

            var existing = lines.FirstOrDefault(x =>
                x.ProductId == dto.ProductId
                && string.Equals(x.Note ?? string.Empty, note ?? string.Empty, StringComparison.Ordinal)
                && string.Equals(x.AddedBy ?? string.Empty, addedBy ?? string.Empty, StringComparison.Ordinal));
            if (existing != null)
            {
                existing.Quantity = Math.Min(100, existing.Quantity + dto.Quantity);
            }
            else
            {
                lines.Add(new GroupOrderLineDto
                {
                    ProductId = dto.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = dto.Quantity,
                    Note = note,
                    AddedBy = addedBy
                });
            }

            entity.LinesJson = JsonSerializer.Serialize(lines);
            await _groups.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [AllowAnonymous]
        public virtual async Task<CloseGroupOrderResultDto> CloseAsync(string code, CloseGroupOrderDto dto)
        {
            var entity = await FindByCodeOrThrowAsync(code);
            if (entity.Status != GroupOrder.StatusOpen)
            {
                throw new Domain.Exceptions.DomainException("Nhóm đã được chốt đơn.");
            }

            var lines = ParseLines(entity.LinesJson);
            if (lines.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Nhóm chưa có món nào để chốt đơn.");
            }

            var order = await _orders.CreateAsync(new CreateOrderDto
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerPhone = dto.CustomerPhone.Trim(),
                CustomerAddress = dto.CustomerAddress.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes)
                    ? $"Đặt nhóm {entity.Code}"
                    : $"Đặt nhóm {entity.Code} — {dto.Notes.Trim()}",
                PaymentMethod = "Cod",
                FulfillmentMethod = "Delivery",
                Source = "Gom đơn",
                Lines = lines.Select(x => new CreateOrderLineDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    Note = string.IsNullOrWhiteSpace(x.AddedBy)
                        ? x.Note
                        : string.IsNullOrWhiteSpace(x.Note) ? $"({x.AddedBy})" : $"{x.Note} ({x.AddedBy})"
                }).ToList()
            }, skipRecaptchaCheck: true);

            entity.Status = GroupOrder.StatusClosed;
            entity.ClosedAt = _clock.Now;
            entity.ResultOrderNumber = order.OrderNumber;
            await _groups.UpdateAsync(entity, autoSave: true);

            return new CloseGroupOrderResultDto
            {
                Group = MapToDto(entity),
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount
            };
        }

        private async Task<GroupOrder> FindByCodeOrThrowAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Domain.Exceptions.DomainException("Mã nhóm không hợp lệ.");
            }

            var normalized = code.Trim().ToUpperInvariant();
            var queryable = await _groups.GetQueryableAsync();
            var entity = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Code == normalized));
            if (entity == null)
            {
                throw new Domain.Exceptions.DomainException("Không tìm thấy nhóm đặt món.");
            }

            return entity;
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            var random = new Random();
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var code = new string(
                    Enumerable.Range(0, 6).Select(_ => CodeChars[random.Next(CodeChars.Length)]).ToArray());

                var queryable = await _groups.GetQueryableAsync();
                var exists = await _asyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Code == code));
                if (exists == null)
                {
                    return code;
                }
            }

            return Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        private static List<GroupOrderLineDto> ParseLines(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<GroupOrderLineDto>>(json ?? "[]")
                    ?? new List<GroupOrderLineDto>();
            }
            catch
            {
                return new List<GroupOrderLineDto>();
            }
        }

        private static GroupOrderDto MapToDto(GroupOrder entity)
        {
            return new GroupOrderDto
            {
                Id = entity.Id,
                Code = entity.Code,
                CreatorPhone = entity.CreatorPhone,
                GroupName = entity.GroupName,
                CutoffAt = entity.CutoffAt,
                Status = entity.Status,
                Lines = ParseLines(entity.LinesJson),
                CreatedAt = entity.CreatedAt,
                ClosedAt = entity.ClosedAt,
                ResultOrderNumber = entity.ResultOrderNumber
            };
        }
    }
}
