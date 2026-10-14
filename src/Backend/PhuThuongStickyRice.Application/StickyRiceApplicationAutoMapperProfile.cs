using AutoMapper;
using PhuThuongStickyRice.Domain.Entities;

namespace PhuThuongStickyRice.Application
{
    public class StickyRiceApplicationAutoMapperProfile : Profile
    {
        public StickyRiceApplicationAutoMapperProfile()
        {
            CreateMap<Product, Products.DTOs.ProductDto>()
                .ForMember(d => d.Code, o => o.MapFrom(s => s.Code.Value))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.Price.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.Price.Currency))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            CreateMap<Order, Orders.DTOs.OrderDto>()
                .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount.Amount))
                .ForMember(d => d.Currency, o => o.MapFrom(s => s.TotalAmount.Currency))
                .ForMember(d => d.Subtotal, o => o.MapFrom(s => s.TotalAmount.Amount - s.DeliveryFee.Amount + s.DiscountAmount.Amount))
                .ForMember(d => d.DeliveryFee, o => o.MapFrom(s => s.DeliveryFee.Amount))
                .ForMember(d => d.DiscountAmount, o => o.MapFrom(s => s.DiscountAmount.Amount))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
                .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()))
                .ForMember(d => d.FulfillmentMethod, o => o.MapFrom(s => s.FulfillmentMethod.ToString()));

            CreateMap<OrderLine, Orders.DTOs.OrderLineDto>()
                .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.UnitPrice.Amount))
                .ForMember(d => d.LineTotal, o => o.MapFrom(s => s.LineTotal.Amount));
        }
    }
}
