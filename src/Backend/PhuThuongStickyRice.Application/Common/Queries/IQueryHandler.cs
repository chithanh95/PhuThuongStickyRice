using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Handler thực thi một <see cref="IQuery{TResult}"/>: đọc dữ liệu
    /// (có thể join nhiều nguồn) và trả về DTO cho client.
    /// </summary>
    /// <typeparam name="TQuery">Loại query được xử lý.</typeparam>
    /// <typeparam name="TResult">Kiểu dữ liệu trả về.</typeparam>
    public interface IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Thực thi query và trả về kết quả.
        /// </summary>
        /// <param name="query">Query cần xử lý.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        /// <returns>Kết quả truy vấn.</returns>
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
