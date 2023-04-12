using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Events;
using PhuThuongStickyRice.Domain.Identity;
using PhuThuongStickyRice.Domain.Repositories;
using System;

namespace PhuThuongStickyRice.Application.Products.Services
{
    public class ProductService : CrudService<Product>, IProductService
    {
        public ProductService(IRepository<Product, Guid> productRepository, IDomainEvents domainEvents, ICurrentUser currentUser)
            : base(productRepository, domainEvents)
        {
        }
    }
}
