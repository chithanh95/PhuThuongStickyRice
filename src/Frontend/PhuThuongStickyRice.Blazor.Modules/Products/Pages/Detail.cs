using PhuThuongStickyRice.Blazor.Modules.Products.Components;
using PhuThuongStickyRice.Blazor.Modules.Products.Models;
using PhuThuongStickyRice.Blazor.Modules.Products.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Products.Pages
{
    public partial class Detail
    {
        [Inject]
        public ProductService ProductService { get; set; }

        [Inject]
        public NavigationManager NavManager { get; set; }

        [Inject]
        public ILogger<Detail> Logger { get; set; }

        [Parameter]
        public Guid ProductId { get; set; }

        public ProductModel Product { get; set; } = new ProductModel();

        protected AuditLogsDialog AuditLogsDialog { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Product = await ProductService.GetProductByIdAsync(ProductId);
        }

        protected async Task ViewAuditLogs()
        {
            var logs = await ProductService.GetAuditLogsAsync(Product.Id);
            AuditLogsDialog.Show(logs);
        }
    }
}
