using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Client phía admin gọi API mã giảm giá: admin tạo/sửa/xóa mã, và luồng
    /// checkout public (khách vãng lai) kiểm tra mã / tự áp mã tốt nhất. Các
    /// method ở đây chỉ là HTTP wrapper mỏng — toàn bộ logic validate, tính
    /// tiền giảm và tăng lượt dùng nằm ở backend
    /// (DiscountAppService) nên không nhân bản logic ở client.
    /// </summary>
    public class DiscountService : HttpService
    {
        public DiscountService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Admin: danh sách mã giảm giá mới nhất (tối đa 500).</summary>
        public async Task<List<DiscountCodeModel>> GetListAsync()
        {
            return await GetAsync<List<DiscountCodeModel>>("api/discounts");
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): tạo mã giảm giá mới. Mã được
        /// backend chuẩn hóa in hoa và kiểm tra không trùng; lỗi nghiệp vụ
        /// (trùng mã, thiếu giá trị giảm) do backend ném và hiển thị cho admin.
        /// </summary>
        public async Task<DiscountCodeModel> CreateDiscountCodeAsync(DiscountCodeModel model)
        {
            return await PostAsync<DiscountCodeModel>("api/discounts", model);
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): cập nhật mã giảm giá. Backend
        /// kiểm tra trùng mã loại trừ chính mã đang sửa nên đổi các field khác
        /// không bị báo trùng oan.
        /// </summary>
        public async Task<DiscountCodeModel> UpdateDiscountCodeAsync(Guid id, DiscountCodeModel model)
        {
            return await PutAsync<DiscountCodeModel>($"api/discounts/{id}", model);
        }

        /// <summary>Admin (cần quyền discounts.manage): xóa mã giảm giá theo Id.</summary>
        public async Task DeleteDiscountCodeAsync(Guid id)
        {
            await DeleteAsync($"api/discounts/{id}");
        }

        /// <summary>
        /// Public (checkout): kiểm tra 1 mã với tổng giỏ hàng — chỉ tính thử,
        /// KHÔNG tăng lượt dùng. Lượt dùng chỉ tăng khi đơn được tạo thành công
        /// để đơn lỗi không đốt lượt mã của khách.
        /// </summary>
        /// <param name="code">Mã khách nhập (không phân biệt hoa/thường).</param>
        /// <param name="subtotal">Tổng tiền giỏ hàng trước khi giảm.</param>
        /// <param name="customerPhone">SĐT khách (kiểm tra mã gắn SĐT như quay số/sinh nhật).</param>
        /// <returns>Kết quả validate kèm số tiền được giảm dự kiến.</returns>
        public async Task<ValidateDiscountResultModel> ValidateAsync(string code, decimal subtotal, string customerPhone = null)
        {
            return await PostAsync<ValidateDiscountResultModel>("api/discounts/validate",
                new { Code = code, Subtotal = subtotal, CustomerPhone = customerPhone });
        }

        /// <summary>Item 139: active public codes for checkout auto-apply.</summary>
        public async Task<List<DiscountCodeModel>> GetActivePublicAsync()
        {
            return await GetAsync<List<DiscountCodeModel>>("api/discounts/active");
        }

        /// <summary>
        /// perf-hunt 2026-10-19: mã giảm tiền tốt nhất cho giỏ hàng trong 1
        /// round-trip — thay vòng lặp N lần ValidateAsync ở checkout.
        /// </summary>
        public async Task<BestDiscountResultModel> EvaluateBestAsync(decimal subtotal, string customerPhone = null)
        {
            return await PostAsync<BestDiscountResultModel>("api/discounts/best",
                new { Subtotal = subtotal, CustomerPhone = customerPhone });
        }
    }
}
