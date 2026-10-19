using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Landing.Models
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

        public string FooterAboutText { get; set; }

        public string FooterCopyrightText { get; set; }

        public string ContactAddress { get; set; }

        public string ContactPhone { get; set; }

        public string ContactEmail { get; set; }

        public string FooterButtonText { get; set; } = "Đặt món ngay";

        public string HeroImageUrl { get; set; }

        public string BannerImageUrl { get; set; }

        public string MenuImage1Url { get; set; }

        public string MenuImage2Url { get; set; }

        public string MenuImage3Url { get; set; }

        public string FooterBackgroundUrl { get; set; }

        public string VideoTitle { get; set; }

        public string VideoUrl { get; set; }

        public string MapEmbedUrl { get; set; }

        public string ReviewsEmbedHtml { get; set; }

        public string PopupTitle { get; set; }

        public string PopupBody { get; set; }

        public string PopupImageUrl { get; set; }

        public bool PopupActive { get; set; }

        public string FlashBannerText { get; set; }

        public bool FlashBannerActive { get; set; }

        /// <summary>Landing item 4: bật countdown khuyến mãi.</summary>
        public bool PromoCountdownActive { get; set; }

        /// <summary>Landing item 4: thời điểm kết thúc countdown (UTC).</summary>
        public System.DateTime? PromoCountdownEnd { get; set; }

        /// <summary>Landing item 4: nội dung countdown.</summary>
        public string PromoCountdownText { get; set; }

        /// <summary>Landing item 38: video review của khách/KOL.</summary>
        public string ReviewVideoUrl { get; set; }

        /// <summary>Landing item 38: tiêu đề video review.</summary>
        public string ReviewVideoTitle { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class LandingStatsModel
    {
        public int TotalOrders { get; set; }
        public double AvgRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class GalleryPhotoModel
    {
        public string ImageUrl { get; set; }
        public string Caption { get; set; }
        public string Name { get; set; }
    }

    public class FaqPublicModel
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class LuckySpinResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PrizeLabel { get; set; }
        public int PrizeValue { get; set; }
        public string VoucherCode { get; set; }
    }

    public class ChatMessageModel
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class OffPeakPromoModel
    {
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public int DiscountAmount { get; set; }
        public bool IsNow { get; set; }
    }

    public class StreakCheckInResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StreakCount { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DishPollModel
    {
        public Guid Id { get; set; }
        public string DishName { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int VoteCount { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class BirthdayGiftResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string GiftCode { get; set; }
    }

    public class VipStatusModel
    {
        public string Tier { get; set; }
        public bool IsVip { get; set; }
        public string ZaloGroupUrl { get; set; }
    }

    public class CourseModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class ParsedOrderItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ParseOrderResult
    {
        public bool IsOrderIntent { get; set; }
        public List<ParsedOrderItem> Items { get; set; } = new List<ParsedOrderItem>();
        public string UnmatchedText { get; set; }
    }

    public class MarketplaceLinksModel
    {
        public string ShopeeFoodUrl { get; set; }
        public string GrabFoodUrl { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    // CA1852: internal và không có subtype → sealed để JIT devirtualize.
    internal sealed class ProductIdsWrapper
    {
        public List<Guid> ProductIds { get; set; }
    }

    internal sealed class ItemsWrapper<T>
    {
        public List<T> Items { get; set; }
    }
}

namespace PhuThuongStickyRice.Web.Components.Landing.Models
{
    public class BackInStockItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrlsJson { get; set; }
    }
}
