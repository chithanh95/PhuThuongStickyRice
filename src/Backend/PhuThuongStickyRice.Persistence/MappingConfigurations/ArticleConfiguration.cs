using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class ArticleConfiguration : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> builder)
        {
            builder.ToTable("Articles");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Title).HasMaxLength(Article.TitleMaxLength).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(Article.BodyMaxLength).IsRequired();
            builder.Property(x => x.ImageUrl).HasMaxLength(Article.ImageUrlMaxLength);
            builder.Property(x => x.PublishedAt).IsRequired();
            builder.Property(x => x.IsPublished).IsRequired();
        }
    }
}
