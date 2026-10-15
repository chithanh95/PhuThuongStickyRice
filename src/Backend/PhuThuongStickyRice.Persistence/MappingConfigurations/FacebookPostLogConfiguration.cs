using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    /// <summary>
    /// Item 130: history of auto-posts to the Facebook Page feed.
    /// </summary>
    public class FacebookPostLogConfiguration : IEntityTypeConfiguration<FacebookPostLog>
    {
        public void Configure(EntityTypeBuilder<FacebookPostLog> builder)
        {
            builder.ToTable("FacebookPostLogs");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.ProductName)
                .HasMaxLength(FacebookPostLog.ProductNameMaxLength)
                .IsRequired();
            builder.Property(x => x.PostUrl)
                .HasMaxLength(FacebookPostLog.PostUrlMaxLength);
            builder.Property(x => x.Status)
                .HasMaxLength(FacebookPostLog.StatusMaxLength)
                .IsRequired();
            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(FacebookPostLog.ErrorMessageMaxLength);
            builder.Property(x => x.CreatedAt).IsRequired();

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
