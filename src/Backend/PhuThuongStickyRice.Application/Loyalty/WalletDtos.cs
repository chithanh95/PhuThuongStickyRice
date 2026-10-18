using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty
{
        public class WalletAccountDto
        {
            public Guid Id { get; set; }
            public string Phone { get; set; }
            public decimal Balance { get; set; }
        }

        public class WalletTransactionDto
        {
            public Guid Id { get; set; }
            public string Phone { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; }
            public decimal BalanceAfter { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class AdjustWalletInput
        {
            [Required]
            [StringLength(30)]
            public string Phone { get; set; }

            /// <summary>Positive = top-up, negative = manual deduct.</summary>
            [Range(1000, 100000000)]
            public decimal Amount { get; set; }

            [Required]
            [StringLength(500)]
            public string Reason { get; set; }
        }
}
