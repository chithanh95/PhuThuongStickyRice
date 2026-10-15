using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Customer
{
    public class CustomerProfileDto
    {
        public List<string> TasteTags { get; set; } = new List<string>();

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>False when DOB was changed within the last year.</summary>
        public bool CanChangeDateOfBirth { get; set; }

        public string DateOfBirthChangeHint { get; set; }

        /// <summary>This year's birthday gift code (tied to the customer's phone).</summary>
        public string BirthdayGiftCode { get; set; }

        public decimal? BirthdayGiftAmount { get; set; }

        public DateTime? BirthdayGiftExpiresAt { get; set; }
    }

    public class UpdateCustomerProfileDto
    {
        public List<string> TasteTags { get; set; }

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }

    public class BirthdayMemberDto
    {
        public Guid UserId { get; set; }

        public string Phone { get; set; }

        public string UserName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool HasGiftThisYear { get; set; }

        public string GiftCode { get; set; }
    }

    public class GrantBirthdayGiftDto
    {
        /// <summary>Gift amount in VND. Defaults to 25000.</summary>
        public decimal DiscountAmount { get; set; } = 25000;
    }

    public class BirthdayGiftResultDto
    {
        public string Code { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    public class CreateReminderDto
    {
        public string Phone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }
    }

    public class CustomerReminderDto
    {
        public Guid Id { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }

        public string SentVia { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
