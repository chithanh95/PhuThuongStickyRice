using System;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>Lightweight toast notifications (e.g. "added to cart").</summary>
    public class ToastService
    {
        public event Action<ToastMessage> OnShow;

        public void Show(string message, string linkText = null, string linkUrl = null, ToastKind kind = ToastKind.Success)
        {
            OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                LinkText = linkText,
                LinkUrl = linkUrl,
                Id = Guid.NewGuid(),
                Kind = kind
            });
        }
    }

    /// <summary>Loại toast — quyết định icon và ngữ nghĩa ARIA cho trình đọc màn hình.
    /// Success = xác nhận (check xanh, role="status"); Warning = cảnh báo/lỗi nghiệp vụ
    /// (icon tam giác + role="alert" để đọc ngay); Info = thông tin trung tính.</summary>
    public enum ToastKind
    {
        Success,
        Warning,
        Info
    }

    public class ToastMessage
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public string LinkText { get; set; }

        public string LinkUrl { get; set; }

        /// <summary>Loại toast (mặc định Success để tương thích mọi caller cũ).</summary>
        public ToastKind Kind { get; set; } = ToastKind.Success;
    }
}
