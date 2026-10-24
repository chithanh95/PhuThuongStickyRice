using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Logged-in customer's profile: taste preferences + birthday
    /// (api/customer/profile). Requires login; JWT attached by ApiClientBase.
    /// </summary>
    public class CustomerProfileService : ApiClientBase
    {
        /// <summary>
        /// Khởi tạo service hồ sơ khách hàng (gắn JWT qua <see cref="ApiClientBase"/>).
        /// </summary>
        public CustomerProfileService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Lấy hồ sơ của khách đang đăng nhập. Fail-soft: lỗi mạng/API thì trả
        /// model trống thay vì nổ để trang TaiKhoan vẫn render được.
        /// </summary>
        /// <returns>Hồ sơ khách (khẩu vị, ngày sinh); rỗng nếu lỗi.</returns>
        public async Task<CustomerProfileModel> GetMyAsync()
        {
            try
            {
                return await GetAsync<CustomerProfileModel>("api/customer/profile")
                    ?? new CustomerProfileModel();
            }
            catch (Exception)
            {
                return new CustomerProfileModel();
            }
        }

        /// <summary>
        /// Cập nhật khẩu vị/ngày sinh. Luật "ngày sinh đổi 1 lần/năm" do server
        /// kiểm tra — client chỉ hiển thị gợi ý từ <c>CanChangeDateOfBirth</c>.
        /// </summary>
        /// <param name="model">Hồ sơ cần lưu.</param>
        /// <returns>Hồ sơ sau khi server lưu.</returns>
        /// <exception cref="InvalidOperationException">Ném khi server trả lỗi (kèm message tiếng Việt).</exception>
        public async Task<CustomerProfileModel> UpdateAsync(CustomerProfileModel model)
        {
            await SetBearerToken();
            // Họ tên CHỈ HIỂN THỊ ở trang tài khoản (user chốt 2026-10-24)
            // nên không gửi lên khi lưu hồ sơ: gửi null để server giữ
            // nguyên claim tên — tránh ghi đè tên tài khoản bằng giá trị
            // hiển thị (vốn có thể là tên dự phòng từ đơn hàng gần nhất).
            var payload = new CustomerProfileModel
            {
                FullName = null,
                TasteTags = model.TasteTags,
                TasteNote = model.TasteNote,
                DateOfBirth = model.DateOfBirth,
            };
            var response = await _httpClient.PutAsJsonAsync("api/customer/profile", payload);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await HttpErrorMessageReader.ReadErrorMessageAsync(
                    response, "Lưu thất bại. Vui lòng thử lại."));
            }

            return await response.Content.ReadFromJsonAsync<CustomerProfileModel>();
        }
    }

    public class CustomerProfileModel
    {
        /// <summary>Họ tên khách nhập khi đăng ký (server lưu dạng claim) — checkout tự điền.</summary>
        public string FullName { get; set; }

        /// <summary>SĐT của tài khoản — checkout tự điền.</summary>
        public string PhoneNumber { get; set; }

        /// <summary>Email của tài khoản — checkout tự điền ô email.</summary>
        public string Email { get; set; }

        public List<string> TasteTags { get; set; } = new List<string>();

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool CanChangeDateOfBirth { get; set; } = true;

        public string DateOfBirthChangeHint { get; set; }

        public string BirthdayGiftCode { get; set; }

        public decimal? BirthdayGiftAmount { get; set; }

        public DateTime? BirthdayGiftExpiresAt { get; set; }
    }
}
