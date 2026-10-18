using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Content
{
        public class ArticleDto
        {
            public Guid Id { get; set; }

            public string Title { get; set; }

            public string Body { get; set; }

            public string ImageUrl { get; set; }

            public DateTime PublishedAt { get; set; }

            public bool IsPublished { get; set; }
        }

        public class CreateArticleDto
        {
            [Required]
            [StringLength(300)]
            public string Title { get; set; }

            [Required]
            [StringLength(10000)]
            public string Body { get; set; }

            [StringLength(500)]
            public string ImageUrl { get; set; }

            public DateTime? PublishedAt { get; set; }

            public bool IsPublished { get; set; } = true;
        }
}
