using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PhuThuongStickyRice.Application
{
    /// <summary>
    /// Tạo instance handler qua reflection cho DI đăng ký factory thủ công:
    /// tự inject các dependency từ provider, còn tham số handler lồng nhau
    /// (cho decorator chain) được truyền thẳng vào.
    /// </summary>
    internal sealed class HandlerFactory
    {
        private readonly List<Func<object, Type, IServiceProvider, object>> _handlerFactoriesPipeline = new List<Func<object, Type, IServiceProvider, object>>();

        /// <summary>Khởi tạo factory cho 1 loại handler cụ thể.</summary>
        /// <param name="type">Type của handler (có thể là generic mở).</param>
        public HandlerFactory(Type type)
        {
            AddHandlerFactory(type);
        }

        /// <summary>
        /// Dựng handler cho interface được yêu cầu: chạy pipeline factory
        /// (decorator chain) — mỗi factory bọc handler của bước trước qua
        /// tham số handler lồng nhau, các tham số còn lại resolve từ DI provider.
        /// </summary>
        /// <param name="provider">DI container để resolve dependency của constructor.</param>
        /// <param name="handlerInterfaceType">Interface handler cần dựng (generic đã đóng).</param>
        /// <returns>Instance handler ngoài cùng của decorator chain.</returns>
        public object Create(IServiceProvider provider, Type handlerInterfaceType)
        {
            object currentHandler = null;
            foreach (var handlerFactory in _handlerFactoriesPipeline)
            {
                currentHandler = handlerFactory(currentHandler, handlerInterfaceType, provider);
            }

            return currentHandler;
        }

        /// <summary>
        /// Đăng ký factory cho 1 handler: khi <see cref="Create"/> chạy, mỗi
        /// factory bọc handler của bước trước (decorator chain) bằng cách
        /// truyền nó vào tham số constructor là interface handler; các tham
        /// số còn lại resolve từ DI provider qua <see cref="GetParameters"/>.
        /// </summary>
        /// <param name="handlerType">Type của handler (có thể là generic mở).</param>
        /// <param name="attribute">Attribute trang trí handler (nếu có), truyền thẳng vào constructor.</param>
        private void AddHandlerFactory(Type handlerType, object attribute = null)
        {
            _handlerFactoriesPipeline.Add(CreateHandler);

            object CreateHandler(object decoratingHandler, Type interfaceType, IServiceProvider provider)
            {
                var ctor = handlerType
                   .MakeGenericTypeSafe(interfaceType.GenericTypeArguments)
                   .GetConstructors()
                   .Single();

                var parameterInfos = ctor.GetParameters();
                var parameters = GetParameters(parameterInfos, decoratingHandler, attribute, provider);

                var handler = ctor.Invoke(parameters);

                return handler;
            }
        }

        /// <summary>
        /// Resolve tham số constructor cho handler: tham số là interface
        /// handler thì nhận handler của bước trước (để bọc decorator), tham
        /// số đúng type attribute thì nhận thẳng attribute, còn lại resolve
        /// từ DI provider. Ném <see cref="ArgumentException"/> khi provider
        /// không có service tương ứng.
        /// </summary>
        private static object[] GetParameters(IEnumerable<ParameterInfo> parameterInfos, object current, object attribute, IServiceProvider provider)
        {
            return parameterInfos.Select(GetParameter).ToArray();

            object GetParameter(ParameterInfo parameterInfo)
            {
                var parameterType = parameterInfo.ParameterType;

                if (HandlerTypeInspector.IsHandlerInterface(parameterType))
                {
                    return current;
                }

                if (parameterType == attribute?.GetType())
                {
                    return attribute;
                }

                var service = provider.GetService(parameterType);
                if (service != null)
                {
                    return service;
                }

                throw new ArgumentException($"Type {parameterType} not found");
            }
        }
    }
}