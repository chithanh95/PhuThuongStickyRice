using PhuThuongStickyRice.Application.Landing.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Landing
{
    public interface ILandingAppService : IApplicationService
    {
        Task<LandingContentDto> GetContentAsync();

        Task<LandingContentDto> UpdateContentAsync(LandingContentDto dto);
    }

    public class LandingAppService : ApplicationService, ILandingAppService
    {
        private readonly IRepository<LandingPageContent, Guid> _contentRepository;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public LandingAppService(
            IRepository<LandingPageContent, Guid> contentRepository,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _contentRepository = contentRepository;
            _asyncExecuter = asyncExecuter;
        }

        [AllowAnonymous]
        public async Task<LandingContentDto> GetContentAsync()
        {
            var entity = await GetSingleAsync();
            return entity == null ? LandingContentDefaults.GetDefaultDto() : MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<LandingContentDto> UpdateContentAsync(LandingContentDto dto)
        {
            var entity = await GetSingleAsync();
            var newsJson = JsonSerializer.Serialize(dto.NewsItems ?? new List<LandingNewsItemDto>());

            if (entity == null)
            {
                entity = LandingPageContent.Create(
                    dto.HeroTitle, dto.HeroTagline, dto.HeroButtonText,
                    dto.StoryTitle, dto.StoryBody,
                    dto.MenuTitle, dto.MenuLabel1, dto.MenuLabel2, dto.MenuLabel3,
                    dto.NewsTitle, newsJson,
                    dto.FooterTitle, dto.ContactAddress, dto.ContactPhone, dto.ContactEmail,
                    dto.FooterButtonText,
                    dto.HeroImageUrl, dto.BannerImageUrl,
                    dto.MenuImage1Url, dto.MenuImage2Url, dto.MenuImage3Url,
                    dto.FooterBackgroundUrl,
                    dto.VideoTitle, dto.VideoUrl,
                    dto.MapEmbedUrl, dto.ReviewsEmbedHtml,
                    dto.PopupTitle, dto.PopupBody, dto.PopupImageUrl, dto.PopupActive,
                    dto.FlashBannerText, dto.FlashBannerActive,
                    dto.PromoCountdownActive, dto.PromoCountdownEnd, dto.PromoCountdownText,
                    dto.ReviewVideoTitle, dto.ReviewVideoUrl);
                await _contentRepository.InsertAsync(entity, autoSave: true);
            }
            else
            {
                entity.Update(
                    dto.HeroTitle, dto.HeroTagline, dto.HeroButtonText,
                    dto.StoryTitle, dto.StoryBody,
                    dto.MenuTitle, dto.MenuLabel1, dto.MenuLabel2, dto.MenuLabel3,
                    dto.NewsTitle, newsJson,
                    dto.FooterTitle, dto.ContactAddress, dto.ContactPhone, dto.ContactEmail,
                    dto.FooterButtonText,
                    dto.HeroImageUrl, dto.BannerImageUrl,
                    dto.MenuImage1Url, dto.MenuImage2Url, dto.MenuImage3Url,
                    dto.FooterBackgroundUrl,
                    dto.VideoTitle, dto.VideoUrl,
                    dto.MapEmbedUrl, dto.ReviewsEmbedHtml,
                    dto.PopupTitle, dto.PopupBody, dto.PopupImageUrl, dto.PopupActive,
                    dto.FlashBannerText, dto.FlashBannerActive,
                    dto.PromoCountdownActive, dto.PromoCountdownEnd, dto.PromoCountdownText,
                    dto.ReviewVideoTitle, dto.ReviewVideoUrl);
                await _contentRepository.UpdateAsync(entity, autoSave: true);
            }

            return dto;
        }

        private async Task<LandingPageContent> GetSingleAsync()
        {
            // Dùng repository trực tiếp thay vì GetQueryableAsync để tránh
            // DbContext concurrency khi queryable bị giữ qua nhiều await.
            return await _contentRepository.FirstOrDefaultAsync(x => true);
        }

        private static LandingContentDto MapToDto(LandingPageContent entity)
        {
            return new LandingContentDto
            {
                HeroTitle = entity.HeroTitle,
                HeroTagline = entity.HeroTagline,
                HeroButtonText = entity.HeroButtonText,
                StoryTitle = entity.StoryTitle,
                StoryBody = entity.StoryBody,
                MenuTitle = entity.MenuTitle,
                MenuLabel1 = entity.MenuLabel1,
                MenuLabel2 = entity.MenuLabel2,
                MenuLabel3 = entity.MenuLabel3,
                NewsTitle = entity.NewsTitle,
                NewsItems = ParseNews(entity.NewsItemsJson),
                FooterTitle = entity.FooterTitle,
                ContactAddress = entity.ContactAddress,
                ContactPhone = entity.ContactPhone,
                ContactEmail = entity.ContactEmail,
                FooterButtonText = entity.FooterButtonText,
                HeroImageUrl = entity.HeroImageUrl,
                BannerImageUrl = entity.BannerImageUrl,
                MenuImage1Url = entity.MenuImage1Url,
                MenuImage2Url = entity.MenuImage2Url,
                MenuImage3Url = entity.MenuImage3Url,
                FooterBackgroundUrl = entity.FooterBackgroundUrl,
                VideoTitle = entity.VideoTitle,
                VideoUrl = entity.VideoUrl,
                ReviewVideoTitle = entity.ReviewVideoTitle,
                ReviewVideoUrl = entity.ReviewVideoUrl,
                MapEmbedUrl = entity.MapEmbedUrl,
                ReviewsEmbedHtml = entity.ReviewsEmbedHtml,
                PopupTitle = entity.PopupTitle,
                PopupBody = entity.PopupBody,
                PopupImageUrl = entity.PopupImageUrl,
                PopupActive = entity.PopupActive,
                FlashBannerText = entity.FlashBannerText,
                FlashBannerActive = entity.FlashBannerActive,
                PromoCountdownActive = entity.PromoCountdownActive,
                PromoCountdownEnd = entity.PromoCountdownEnd,
                PromoCountdownText = entity.PromoCountdownText
            };
        }

        private static List<LandingNewsItemDto> ParseNews(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<LandingNewsItemDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<LandingNewsItemDto>>(json)
                    ?? new List<LandingNewsItemDto>();
            }
            catch (JsonException)
            {
                return new List<LandingNewsItemDto>();
            }
        }
    }

    /// <summary>
    /// Fallback content shown when the database row has not been seeded yet.
    /// The same copy is seeded by the AddLandingPageContent migration.
    /// </summary>
    public static class LandingContentDefaults
    {
        public static LandingContentDto GetDefaultDto()
        {
            return new LandingContentDto
            {
                HeroTitle = "XÔI PHÚ THƯỢNG",
                HeroTagline = "hương vị làng nghề, dẻo thơm mỗi sáng",
                HeroButtonText = "Đặt món ngay",
                StoryTitle = "Câu chuyện làng nghề",
                StoryBody = "Làng Phú Thượng, bên dòng sông Hồng của Hà Nội, đã gắn liền với nghề nấu xôi suốt hàng trăm năm. Mỗi sáng tinh mơ, tiếng chõ xôi bốc khói, mùi nếp cái hoa vàng quyện cùng đậu xanh, gấc chín đỏ au đã đánh thức cả xóm làng. Người Phú Thượng giữ nghề bằng đôi bàn tay khéo léo: hạt nếp phải chọn loại ngon nhất, đồ xôi đúng lửa, đúng thời gian để hạt xôi dẻo mà không nát. Hôm nay, chúng tôi mang trọn hương vị ấy đến bàn ăn của bạn — gói xôi nóng hổi, thơm dẻo như chính buổi sáng làng nghề.",
                MenuTitle = "Món ngon mỗi sáng",
                MenuLabel1 = "Xôi gấc",
                MenuLabel2 = "Xôi xéo",
                MenuLabel3 = "Xôi ngũ sắc",
                NewsTitle = "Chuyện nhà xôi",
                NewsItems = new List<LandingNewsItemDto>
                {
                    new LandingNewsItemDto
                    {
                        Title = "Món ăn sáng được yêu thích nhất tuần",
                        Body = "Xôi gấc của quán lọt top món ăn sáng được đặt nhiều nhất khu vực Tây Hồ trên ứng dụng đặt món, với hàng trăm đánh giá 5 sao từ thực khách.",
                        Source = "ShopeeFood"
                    },
                    new LandingNewsItemDto
                    {
                        Title = "Giữ trọn nghề nấu xôi trăm năm",
                        Body = "Từ đời bà, đời mẹ đến nay, gia đình vẫn giữ đúng công thức làng nghề Phú Thượng: nếp ngon, đồ hai lần lửa, hành phi tự làm mỗi sáng.",
                        Source = "Chủ quán chia sẻ"
                    },
                    new LandingNewsItemDto
                    {
                        Title = "Khách quen hơn 5 năm vẫn quay lại",
                        Body = "\"Xôi dẻo, đậu xanh bùi, hành phi giòn — ăn sáng ở đây thành thói quen của cả nhà tôi rồi. Gói nào cũng nóng hổi, đầy đặn.\"",
                        Source = "Chị Lan Anh, khách hàng"
                    }
                },
                FooterTitle = "Ghé thăm quán",
                ContactAddress = "Số 1, ngõ 1 Phú Thượng, Tây Hồ, Hà Nội",
                ContactPhone = "0901 234 567",
                ContactEmail = "xinchao@xoiphuthuong.vn",
                FooterButtonText = "Đặt món ngay"
            };
        }
    }
}
