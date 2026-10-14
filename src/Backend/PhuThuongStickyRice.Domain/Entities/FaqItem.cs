using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// FAQ entry shown on the public /faq page.
    /// </summary>
    public class FaqItem : Entity<Guid>, IAggregateRoot
    {
        public const int QuestionMaxLength = 500;
        public const int AnswerMaxLength = 4000;

        public string Question { get; set; }

        public string Answer { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }
}
