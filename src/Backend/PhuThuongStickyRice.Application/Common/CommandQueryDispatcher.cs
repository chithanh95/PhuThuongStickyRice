using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Bộ điều phối CQRS thủ công: nhận command/query, tra DI để tìm handler
    /// tương ứng (ICommandHandler&lt;T&gt;/IQueryHandler&lt;T,R&gt;) rồi gọi
    /// HandleAsync. Dùng dynamic để tránh reflection phức tạp trên generics.
    /// </summary>
    public class CommandQueryDispatcher
    {
        private readonly IServiceProvider _provider;

        /// <summary>Khởi tạo dispatcher với service provider dùng để tra handler.</summary>
        public CommandQueryDispatcher(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Điều phối 1 command: tra DI tìm <c>ICommandHandler&lt;T&gt;</c>
        /// khớp kiểu runtime của command rồi gọi HandleAsync. Ném khi không
        /// tìm thấy handler đã đăng ký.
        /// </summary>
        /// <param name="command">Command cần xử lý.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Type type = typeof(ICommandHandler<>);
            Type[] typeArgs = { command.GetType() };
            Type handlerType = type.MakeGenericType(typeArgs);

            dynamic handler = _provider.GetService(handlerType);
            await handler.HandleAsync((dynamic)command, cancellationToken);
        }

        /// <summary>
        /// Điều phối 1 query: tra DI tìm <c>IQueryHandler&lt;T,R&gt;</c> khớp
        /// kiểu runtime của query rồi gọi HandleAsync, trả kết quả về.
        /// Ném khi không tìm thấy handler đã đăng ký.
        /// </summary>
        /// <typeparam name="T">Kiểu kết quả query trả về.</typeparam>
        /// <param name="query">Query cần xử lý.</param>
        /// <param name="cancellationToken">Token hủy.</param>
        /// <returns>Kết quả do handler của query trả về.</returns>
        public async Task<T> DispatchAsync<T>(IQuery<T> query, CancellationToken cancellationToken = default)
        {
            Type type = typeof(IQueryHandler<,>);
            Type[] typeArgs = { query.GetType(), typeof(T) };
            Type handlerType = type.MakeGenericType(typeArgs);

            dynamic handler = _provider.GetService(handlerType);
            return await handler.HandleAsync((dynamic)query, cancellationToken);
        }
    }
}