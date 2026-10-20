using AutoMapper;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Profile AutoMapper tầng application: bóc value-object (Code, Money...)
    /// và enum của entity ra DTO dạng primitive cho API trả về.
    /// </summary>
    public class StickyRiceApplicationAutoMapperProfile : Profile
    {
        /// <summary>
        /// Định nghĩa các map entity → DTO. Lưu ý: DB chỉ lưu tổng đã tính
        /// (TotalAmount) nên subtotal được suy ngược = tổng + các khoản đã
        /// giảm (mã giảm/loyalty/hạng/khuyến mãi) − phí giao hàng/quà tặng.
        /// </summary>
        public StickyRiceApplicationAutoMapperProfile()
        {
            CreateMap<Product, Products.DTOs.ProductDto>()
                .ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.Price.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Price.Currency))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // bug-hunt 2026-10-19: clone DTO trước khi che giá vốn (CostPrice)
            // ở endpoint public — không mutate object trong cache dùng chung.
            CreateMap<Products.DTOs.ProductDto, Products.DTOs.ProductDto>();

            // Item 148: product change history (convention mapping).
            CreateMap<ProductChangeLog, Products.DTOs.ProductChangeLogDto>();

            CreateMap<Order, Orders.DTOs.OrderDto>()
                .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.TotalAmount.Currency))
                .ForMember(d => d.Subtotal, o => o.MapFrom(s => s.TotalAmount.Amount - s.DeliveryFee.Amount - s.GiftFee.Amount + s.DiscountAmount.Amount + s.LoyaltyDiscount.Amount + s.TierDiscount.Amount + s.PromotionDiscountAmount))
                .ForMember(d => d.DeliveryFee, o => o.MapFrom(s => s.DeliveryFee.Amount))
                .ForMember(d => d.DiscountAmount, o => o.MapFrom(s => s.DiscountAmount.Amount))
                .ForMember(d => d.LoyaltyDiscount, o => o.MapFrom(s => s.LoyaltyDiscount.Amount))
                .ForMember(d => d.TierDiscount, o => o.MapFrom(s => s.TierDiscount.Amount))
                .ForMember(d => d.GiftFee, o => o.MapFrom(s => s.GiftFee.Amount))
                .ForMember(d => d.ShipperName, o => o.MapFrom(s => s.Shipper != null ? s.Shipper.Name : null))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
                .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()))
                .ForMember(d => d.FulfillmentMethod, o => o.MapFrom(s => s.FulfillmentMethod.ToString()))
                .ForMember(d => d.RefundStatus, o => o.MapFrom(s => s.RefundStatus.ToString()));

            CreateMap<OrderLine, Orders.DTOs.OrderLineDto>()
                .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.UnitPrice.Amount))
                .ForMember(d => d.LineTotal, o => o.MapFrom(s => s.LineTotal.Amount))
                // Entity lưu id topping dạng CSV — DTO trả List<Guid> để
                // frontend "đặt lại" dùng thẳng, không tự parse ở client.
                .ForMember(d => d.ToppingIds, o => o.MapFrom(s => s.GetToppingIds()));
        }
    }
}
