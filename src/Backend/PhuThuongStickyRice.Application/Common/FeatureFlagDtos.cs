using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Common
{
        public class FeatureFlagDto
        {
            public Guid Id { get; set; }
            public string Key { get; set; }
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
            public string Description { get; set; }
        }
    public class SetFeatureFlagEnabledDto
    {
        public bool Enabled { get; set; }
    }
}
