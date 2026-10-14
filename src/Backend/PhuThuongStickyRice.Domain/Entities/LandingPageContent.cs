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

        protected LandingPageContent()
        {
        }

        public static LandingPageContent Create(
            string heroTitle, string heroTagline, string heroButtonText,
            string storyTitle, string storyBody,
            string menuTitle, string menuLabel1, string menuLabel2, string menuLabel3,
            string newsTitle, string newsItemsJson,
            string footerTitle, string contactAddress, string contactPhone, string contactEmail,
            string footerButtonText)
        {
            var content = new LandingPageContent();
            content.Update(
                heroTitle, heroTagline, heroButtonText,
                storyTitle, storyBody,
                menuTitle, menuLabel1, menuLabel2, menuLabel3,
                newsTitle, newsItemsJson,
                footerTitle, contactAddress, contactPhone, contactEmail,
                footerButtonText);
            return content;
        }

        public void Update(
            string heroTitle, string heroTagline, string heroButtonText,
            string storyTitle, string storyBody,
            string menuTitle, string menuLabel1, string menuLabel2, string menuLabel3,
            string newsTitle, string newsItemsJson,
            string footerTitle, string contactAddress, string contactPhone, string contactEmail,
            string footerButtonText)
        {
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
        }
    }
}
