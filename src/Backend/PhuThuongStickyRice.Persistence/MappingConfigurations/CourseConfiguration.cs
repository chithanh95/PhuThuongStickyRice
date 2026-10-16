using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.ToTable("Courses");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.Title).HasMaxLength(Course.TitleMaxLength).IsRequired();
            builder.Property(x => x.Description);
            builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
            builder.Property(x => x.ThumbnailUrl).HasMaxLength(Course.UrlMaxLength);
            builder.Property(x => x.VideoUrl).HasMaxLength(Course.UrlMaxLength);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
