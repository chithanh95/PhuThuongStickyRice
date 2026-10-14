using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Editable content of the public landing page (single row concept).
    /// News items are stored as a JSON array in <see cref="NewsItemsJson"/>.
    /// </summary>
    public class LandingPageContent : Entity<Guid>, IAggregateRoot
    {
        public const int HeroTitleMaxLength = 200;
        public const int HeroTaglineMaxLength = 500;
        public const int ButtonTextMaxLength = 100;
        public const int StoryTitleMaxLength = 200;
        public const int StoryBodyMaxLength = 4000;
        public const int MenuTitleMaxLength = 200;
        public const int MenuLabelMaxLength = 100;
        public const int NewsTitleMaxLength = 200;
        public const int NewsItemsJsonMaxLength = 4000;
        public const int FooterTitleMaxLength = 200;
        public const int ContactAddressMaxLength = 500;
        public const int ContactPhoneMaxLength = 100;
        public const int ContactEmailMaxLength = 254;
        public const int ImageUrlMaxLength = 500;
        public const int VideoTitleMaxLength = 200;
        public const int VideoUrlMaxLength = 500;
        public const int MapEmbedUrlMaxLength = 1000;
        public const int ReviewsEmbedHtmlMaxLength = 8000;

        public string HeroTitle { get; private set; }

        public string HeroTagline { get; private set; }

        public string HeroButtonText { get; private set; }

        public string StoryTitle { get; private set; }

        public string StoryBody { get; private set; }

        public string MenuTitle { get; private set; }

        public string MenuLabel1 { get; private set; }

        public string MenuLabel2 { get; private set; }

        public string MenuLabel3 { get; private set; }

        public string NewsTitle { get; private set; }

        public string NewsItemsJson { get; private set; }

        public string FooterTitle { get; private set; }

        public string ContactAddress { get; private set; }

        public string ContactPhone { get; private set; }

        public string ContactEmail { get; private set; }

        public string FooterButtonText { get; private set; }

        /// <summary>Uploaded image URLs (relative, e.g. /uploads/landing/abc.jpg). Null = use placeholder.</summary>
        public string HeroImageUrl { get; private set; }

        public string BannerImageUrl { get; private set; }

        public string MenuImage1Url { get; private set; }

        public string MenuImage2Url { get; private set; }

        public string MenuImage3Url { get; private set; }

        public string FooterBackgroundUrl { get; private set; }

        /// <summary>YouTube video title shown above the embed.</summary>
        public string VideoTitle { get; private set; }

        /// <summary>YouTube watch/share URL; rendered as an embed.</summary>
        public string VideoUrl { get; private set; }

        /// <summary>Google Maps embed URL rendered as an iframe in the contact section.</summary>
        public string MapEmbedUrl { get; private set; }

        /// <summary>
        /// Trusted raw HTML snippet (e.g. Google reviews widget). Admin-only
        /// field; rendered unencoded on the landing page.
        /// </summary>
        public string ReviewsEmbedHtml { get; private set; }

        protected LandingPageContent()
        {
        }

        public static LandingPageContent Create(
            string heroTitle, string heroTagline, string heroButtonText,
            string storyTitle, string storyBody,
            string menuTitle, string menuLabel1, string menuLabel2, string menuLabel3,
            string newsTitle, string newsItemsJson,
            string footerTitle, string contactAddress, string contactPhone, string contactEmail,
            string footerButtonText,
            string heroImageUrl = null, string bannerImageUrl = null,
            string menuImage1Url = null, string menuImage2Url = null, string menuImage3Url = null,
            string footerBackgroundUrl = null,
            string videoTitle = null, string videoUrl = null,
            string mapEmbedUrl = null, string reviewsEmbedHtml = null)
        {
            var content = new LandingPageContent();
            content.Update(
                heroTitle, heroTagline, heroButtonText,
                storyTitle, storyBody,
                menuTitle, menuLabel1, menuLabel2, menuLabel3,
                newsTitle, newsItemsJson,
                footerTitle, contactAddress, contactPhone, contactEmail,
                footerButtonText,
                heroImageUrl, bannerImageUrl,
                menuImage1Url, menuImage2Url, menuImage3Url,
                footerBackgroundUrl,
                videoTitle, videoUrl,
                mapEmbedUrl, reviewsEmbedHtml);
            return content;
        }

        public void Update(
            string heroTitle, string heroTagline, string heroButtonText,
            string storyTitle, string storyBody,
            string menuTitle, string menuLabel1, string menuLabel2, string menuLabel3,
            string newsTitle, string newsItemsJson,
            string footerTitle, string contactAddress, string contactPhone, string contactEmail,
            string footerButtonText,
            string heroImageUrl = null, string bannerImageUrl = null,
            string menuImage1Url = null, string menuImage2Url = null, string menuImage3Url = null,
            string footerBackgroundUrl = null,
            string videoTitle = null, string videoUrl = null,
            string mapEmbedUrl = null, string reviewsEmbedHtml = null)
        {
            VideoTitle = videoTitle;
            VideoUrl = videoUrl;
            MapEmbedUrl = mapEmbedUrl;
            ReviewsEmbedHtml = reviewsEmbedHtml;
            HeroTitle = heroTitle;
            HeroTagline = heroTagline;
            HeroButtonText = heroButtonText;
            StoryTitle = storyTitle;
            StoryBody = storyBody;
            MenuTitle = menuTitle;
            MenuLabel1 = menuLabel1;
            MenuLabel2 = menuLabel2;
            MenuLabel3 = menuLabel3;
            NewsTitle = newsTitle;
            NewsItemsJson = newsItemsJson;
            FooterTitle = footerTitle;
            ContactAddress = contactAddress;
            ContactPhone = contactPhone;
            ContactEmail = contactEmail;
            FooterButtonText = footerButtonText;
            HeroImageUrl = heroImageUrl;
            BannerImageUrl = bannerImageUrl;
            MenuImage1Url = menuImage1Url;
            MenuImage2Url = menuImage2Url;
            MenuImage3Url = menuImage3Url;
            FooterBackgroundUrl = footerBackgroundUrl;
        }
    }
}
