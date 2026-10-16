using PhuThuongStickyRice.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PhuThuongStickyRice.Persistence.MappingConfigurations
{
    public class LandingPageContentConfiguration : IEntityTypeConfiguration<LandingPageContent>
    {
        public void Configure(EntityTypeBuilder<LandingPageContent> builder)
        {
            builder.ToTable("LandingPageContent");
            builder.Property(x => x.Id).HasDefaultValueSql("newsequentialid()");

            builder.Property(x => x.HeroTitle).HasMaxLength(LandingPageContent.HeroTitleMaxLength);
            builder.Property(x => x.HeroTagline).HasMaxLength(LandingPageContent.HeroTaglineMaxLength);
            builder.Property(x => x.HeroButtonText).HasMaxLength(LandingPageContent.ButtonTextMaxLength);
            builder.Property(x => x.StoryTitle).HasMaxLength(LandingPageContent.StoryTitleMaxLength);
            builder.Property(x => x.StoryBody).HasMaxLength(LandingPageContent.StoryBodyMaxLength);
            builder.Property(x => x.MenuTitle).HasMaxLength(LandingPageContent.MenuTitleMaxLength);
            builder.Property(x => x.MenuLabel1).HasMaxLength(LandingPageContent.MenuLabelMaxLength);
            builder.Property(x => x.MenuLabel2).HasMaxLength(LandingPageContent.MenuLabelMaxLength);
            builder.Property(x => x.MenuLabel3).HasMaxLength(LandingPageContent.MenuLabelMaxLength);
            builder.Property(x => x.NewsTitle).HasMaxLength(LandingPageContent.NewsTitleMaxLength);
            builder.Property(x => x.NewsItemsJson).HasMaxLength(LandingPageContent.NewsItemsJsonMaxLength);
            builder.Property(x => x.FooterTitle).HasMaxLength(LandingPageContent.FooterTitleMaxLength);
            builder.Property(x => x.ContactAddress).HasMaxLength(LandingPageContent.ContactAddressMaxLength);
            builder.Property(x => x.ContactPhone).HasMaxLength(LandingPageContent.ContactPhoneMaxLength);
            builder.Property(x => x.ContactEmail).HasMaxLength(LandingPageContent.ContactEmailMaxLength);
            builder.Property(x => x.FooterButtonText).HasMaxLength(LandingPageContent.ButtonTextMaxLength);

            builder.Property(x => x.HeroImageUrl).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.BannerImageUrl).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.MenuImage1Url).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.MenuImage2Url).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.MenuImage3Url).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.FooterBackgroundUrl).HasMaxLength(LandingPageContent.ImageUrlMaxLength);

            builder.Property(x => x.VideoTitle).HasMaxLength(LandingPageContent.VideoTitleMaxLength);
            builder.Property(x => x.VideoUrl).HasMaxLength(LandingPageContent.VideoUrlMaxLength);
            builder.Property(x => x.MapEmbedUrl).HasMaxLength(LandingPageContent.MapEmbedUrlMaxLength);
            builder.Property(x => x.ReviewsEmbedHtml).HasMaxLength(LandingPageContent.ReviewsEmbedHtmlMaxLength);
            builder.Property(x => x.PopupTitle).HasMaxLength(LandingPageContent.PopupTitleMaxLength);
            builder.Property(x => x.PopupBody).HasMaxLength(LandingPageContent.PopupBodyMaxLength);
            builder.Property(x => x.PopupImageUrl).HasMaxLength(LandingPageContent.ImageUrlMaxLength);
            builder.Property(x => x.PopupActive).IsRequired();
            builder.Property(x => x.FlashBannerText).HasMaxLength(LandingPageContent.FlashBannerTextMaxLength);
            builder.Property(x => x.FlashBannerActive).IsRequired();
            builder.Property(x => x.PromoCountdownText).HasMaxLength(LandingPageContent.PromoCountdownTextMaxLength);
            builder.Property(x => x.PromoCountdownActive).IsRequired();
        }
    }
}
