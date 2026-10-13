using System;

namespace PhuThuongStickyRice.WebAPI.Models.Users
{
    public class SetPasswordModel
    {
        public Guid Id { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
