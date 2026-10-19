using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        /// <summary>
        /// Thông tin một feature flag (công tắc bật/tắt tính năng từ xa mà
        /// không cần deploy code mới).
        /// </summary>
        public class FeatureFlagDto
        {
            public Guid Id { get; set; }
            public string Key { get; set; }
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
            public string Description { get; set; }
        }
    /// <summary>
    /// Dữ liệu bật/tắt một feature flag (chỉ cần trạng thái mới).
    /// </summary>
    public class SetFeatureFlagEnabledDto
    {
        public bool Enabled { get; set; }
    }
}
