using System.Collections.Generic;

namespace PhuThuongStickyRice.Blazor.Modules.Landing.Models
{
    public class LandingNewsItemModel
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public string Source { get; set; }
    }

    public class LandingContentModel
    {
        public string HeroTitle { get; set; } = "XÔI PHÚ THƯỢNG";

        public string HeroTagline { get; set; } = "hương vị làng nghề, dẻo thơm mỗi sáng";

        public string HeroButtonText { get; set; } = "Đặt món ngay";

        public string StoryTitle { get; set; }

        public string StoryBody { get; set; }

        public string MenuTitle { get; set; }

        public string MenuLabel1 { get; set; } = "Xôi gấc";

        public string MenuLabel2 { get; set; } = "Xôi xéo";

        public string MenuLabel3 { get; set; } = "Xôi ngũ sắc";

        public string NewsTitle { get; set; }

        public List<LandingNewsItemModel> NewsItems { get; set; } = new List<LandingNewsItemModel>();

        public string FooterTitle { get; set; }

        public string ContactAddress { get; set; }

        public string ContactPhone { get; set; }

        public string ContactEmail { get; set; }

        public string FooterButtonText { get; set; } = "Đặt món ngay";
    }
}
