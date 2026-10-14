using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Landing.DTOs
{
    public class LandingNewsItemDto
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public string Source { get; set; }
    }

    public class LandingContentDto
    {
        public string HeroTitle { get; set; }

        public string HeroTagline { get; set; }

        public string HeroButtonText { get; set; }

        public string StoryTitle { get; set; }

        public string StoryBody { get; set; }

        public string MenuTitle { get; set; }

        public string MenuLabel1 { get; set; }

        public string MenuLabel2 { get; set; }

        public string MenuLabel3 { get; set; }

        public string NewsTitle { get; set; }

        public List<LandingNewsItemDto> NewsItems { get; set; } = new List<LandingNewsItemDto>();

        public string FooterTitle { get; set; }

        public string ContactAddress { get; set; }

        public string ContactPhone { get; set; }

        public string ContactEmail { get; set; }

        public string FooterButtonText { get; set; }
    }
}
