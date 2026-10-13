using System;

namespace PhuThuongStickyRice.Application.Decorators.AuditLog
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class AuditLogAttribute : Attribute
    {
    }
}
