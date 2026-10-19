using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Handler xử lý một <see cref="ICommand"/> cụ thể: chứa toàn bộ logic
    /// nghiệp vụ của hành động đó (validate, ghi DB, gửi thông báo...).
    /// </summary>
    /// <typeparam name="TCommand">Loại command được xử lý.</typeparam>
    public interface ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Thực thi command.
        /// </summary>
        /// <param name="command">Command cần xử lý.</param>
        /// <param name="cancellationToken">Token hủy thao tác.</param>
        Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }
}
