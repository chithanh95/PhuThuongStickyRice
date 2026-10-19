using System;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Nhận diện type có phải interface handler CQRS không
    /// (<see cref="ICommandHandler{T}"/> hoặc <see cref="IQueryHandler{T,R}"/>)
    /// để DI tự quét đăng ký.
    /// </summary>
    internal static class HandlerTypeInspector
    {
        /// <summary>True khi type là interface handler command/query.</summary>
        public static bool IsHandlerInterface(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var typeDefinition = type.GetGenericTypeDefinition();

            return typeDefinition == typeof(ICommandHandler<>)
                || typeDefinition == typeof(IQueryHandler<,>);
        }
    }
}