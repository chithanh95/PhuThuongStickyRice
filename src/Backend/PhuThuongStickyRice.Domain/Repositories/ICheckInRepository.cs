using PhuThuongStickyRice.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Domain.Repositories
{
    /// <summary>
    /// ABP repository cho <see cref="CheckIn"/> — bổ sung insert nguyên tử
    /// để vá race điểm danh đồng thời: 2 request cùng SĐT cùng ngày đều qua
    /// bước check rồi cùng insert -> bên thua nhận DbUpdateException (unique
    /// (Phone, Date)) -> 500, trong khi kết quả đúng phải là "đã điểm danh".
    /// </summary>
    public interface ICheckInRepository : Volo.Abp.Domain.Repositories.IRepository<CheckIn, System.Guid>
    {
        /// <summary>
        /// Ghi nhận điểm danh nguyên tử: chỉ insert khi chưa có bản ghi cùng
        /// (Phone, Date). Trả về true nếu điểm danh thành công, false nếu SĐT
        /// đã điểm danh ngày đó (bên thua race). Pre-check <c>AnyAsync</c> cho
        /// đường nhanh, catch <c>DbUpdateException</c> cho trường hợp 2 request
        /// cùng qua pre-check — entity lỗi được detach để UoW hiện tại vẫn
        /// dùng được, rồi verify lại bản ghi của bên thắng.
        /// </summary>
        /// <param name="checkIn">Bản ghi điểm danh cần tạo.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        Task<bool> TryInsertAsync(CheckIn checkIn, CancellationToken cancellationToken = default);
    }
}
