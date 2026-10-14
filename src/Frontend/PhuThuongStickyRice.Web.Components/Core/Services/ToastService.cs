using System;

namespace PhuThuongStickyRice.Web.Components.Core.Services
{
    /// <summary>Lightweight toast notifications (e.g. "added to cart").</summary>
    public class ToastService
    {
        public event Action<ToastMessage> OnShow;

        public void Show(string message, string linkText = null, string linkUrl = null)
        {
            OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                LinkText = linkText,
                LinkUrl = linkUrl,
                Id = Guid.NewGuid()
            });
        }
    }

    public class ToastMessage
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public string LinkText { get; set; }

        public string LinkUrl { get; set; }
    }
}
