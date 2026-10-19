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

        public async Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Type type = typeof(ICommandHandler<>);
            Type[] typeArgs = { command.GetType() };
            Type handlerType = type.MakeGenericType(typeArgs);

            dynamic handler = _provider.GetService(handlerType);
            await handler.HandleAsync((dynamic)command, cancellationToken);
        }

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