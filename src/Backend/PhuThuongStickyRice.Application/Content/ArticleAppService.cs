
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Content;

namespace PhuThuongStickyRice.Application.Content
{
    /// <summary>
    /// Item 93: news CMS for the landing page "Chuyện nhà xôi" section.
    /// Published articles replace the hardcoded JSON news items when any exist.
    /// </summary>
    public interface IArticleAppService : IApplicationService
    {
        /// <summary>
        /// Admin: danh sách bài viết (mới nhất trước). Body chỉ trả excerpt
        /// 220 ký tự để nhẹ payload; muốn full Body phải gọi
        /// <see cref="GetByIdAsync"/>.
        /// </summary>
        /// <returns>Danh sách bài viết (Body rút gọn), tối đa 200.</returns>
        Task<List<ArticleDto>> GetListAsync();

        /// <summary>
        /// Admin: lấy 1 bài viết đầy đủ nội dung để sửa. List chỉ trả excerpt
        /// Body (xem GetListAsync) nên form edit phải gọi riêng endpoint này.
        /// </summary>
        Task<ArticleDto> GetByIdAsync(Guid id);

        /// <summary>
        /// Admin: tạo bài viết mới (xóa cache catalog để landing thấy bài mới ngay).
        /// </summary>
        /// <param name="input">Tiêu đề, nội dung, ảnh, ngày đăng, trạng thái xuất bản.</param>
        /// <returns>Bài viết vừa tạo.</returns>
        Task<ArticleDto> CreateAsync(CreateArticleDto input);

        /// <summary>
        /// Admin: cập nhật bài viết (xóa cache catalog để landing thấy nội dung mới ngay).
        /// </summary>
        /// <param name="id">Id bài viết cần sửa.</param>
        /// <param name="input">Dữ liệu mới (tiêu đề, nội dung, ảnh, ngày đăng, trạng thái).</param>
        /// <returns>Bài viết sau khi cập nhật.</returns>
        Task<ArticleDto> UpdateAsync(Guid id, CreateArticleDto input);

        /// <summary>
        /// Admin: xóa bài viết (xóa cache catalog để landing không còn hiển thị).
        /// </summary>
        /// <param name="id">Id bài viết cần xóa.</param>
        Task DeleteAsync(Guid id);

        /// <summary>Public: published articles, newest first.</summary>
        Task<List<ArticleDto>> GetPublishedAsync();
    }

    /// <summary>
    /// Item 93: CMS tin tức cho mục "Chuyện nhà xôi" trên landing page.
    /// Khi có bài viết đã xuất bản, landing dùng bài viết từ DB thay cho
    /// danh sách tin cứng (hardcoded). Admin: CRUD đầy đủ; list admin chỉ
    /// trả excerpt 220 ký tự để nhẹ payload, form sửa lấy full Body qua
    /// GetByIdAsync. Landing đọc qua cache nên mọi thay đổi đều xóa cache.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ArticleAppService : ApplicationService, IArticleAppService
    {
        private readonly IRepository<Article, Guid> _articles;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        /// <summary>
        /// Độ dài excerpt Body trả về cho list admin — khớp đúng
        /// Truncate(a.Body, 220) mà trang Articles.razor đang hiển thị nên
        /// giao diện không đổi, chỉ giảm payload DB → JSON (trước đây mỗi lần
        /// mở trang admin nạp full Body × 200 bài viết, ~MB).
        /// </summary>
        private const int BodyExcerptLength = 220;

        // [codequality-smell] Magic number: số bài viết tối đa 1 lần lấy —
        // đặt hằng số có tên để thay đổi 1 chỗ.
        private const int MaxListRows = 200;

        /// <summary>
        /// Khởi tạo service quản lý bài viết.
        /// </summary>
        /// <param name="articles">Repository bài viết.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        /// <param name="catalogCache">Cache catalog (dùng chung khóa articles:published của landing).</param>
        public ArticleAppService(
            IRepository<Article, Guid> articles,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _articles = articles;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        /// <summary>
        /// Admin: danh sách bài viết, mới nhất trước. Chỉ trả excerpt Body
        /// (220 ký tự) thay vì full — đủ cho cột xem trước ở bảng admin,
        /// giảm payload so với nạp full Body của tối đa 200 bài.
        /// </summary>
        /// <returns>Danh sách bài viết (Body rút gọn), tối đa 200.</returns>
        public virtual async Task<List<ArticleDto>> GetListAsync()
        {
            // perf-hunt 2026-10-19 (run 14:46): trước đây materialize full
            // Body của 200 bài viết cho trang admin (payload ~MB); giờ chỉ
            // project excerpt 220 ký tự server-side — đúng bằng phần admin
            // hiển thị (Truncate 220). Form sửa lấy full Body qua GetByIdAsync.
            var queryable = await _articles.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.PublishedAt).Take(MaxListRows)
                    .Select(x => new ArticleDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Body = x.Body.Length > BodyExcerptLength
                            ? x.Body.Substring(0, BodyExcerptLength)
                            : x.Body,
                        ImageUrl = x.ImageUrl,
                        PublishedAt = x.PublishedAt,
                        IsPublished = x.IsPublished
                    }));
            return items;
        }

        /// <summary>
        /// Admin: lấy 1 bài viết đầy đủ nội dung để sửa. List chỉ trả excerpt
        /// Body (xem GetListAsync) nên form edit phải gọi riêng endpoint này.
        /// </summary>
        /// <param name="id">Id bài viết.</param>
        /// <returns>Bài viết với đầy đủ Body.</returns>
        public virtual async Task<ArticleDto> GetByIdAsync(Guid id)
        {
            var article = await _articles.GetAsync(id);
            return Map(article);
        }

        /// <summary>
        /// Admin: tạo bài viết mới. Tên và nội dung được trim; ảnh trống lưu
        /// null; ngày đăng trống thì mặc định là thời điểm tạo. Xóa cache
        /// catalog để landing thấy bài mới ngay.
        /// </summary>
        /// <param name="input">Tiêu đề, nội dung, ảnh, ngày đăng, trạng thái xuất bản.</param>
        /// <returns>Bài viết vừa tạo.</returns>
        public virtual async Task<ArticleDto> CreateAsync(CreateArticleDto input)
        {
            var article = new Article
            {
                Title = input.Title.Trim(),
                Body = input.Body.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim(),
                PublishedAt = input.PublishedAt?.ToUniversalTime() ?? DateTime.UtcNow,
                IsPublished = input.IsPublished
            };
            await _articles.InsertAsync(article, autoSave: true);
            _catalogCache.Invalidate();
            return Map(article);
        }

        /// <summary>
        /// Admin: cập nhật bài viết. Ngày đăng chỉ đổi khi form nhập giá trị
        /// mới — tránh ghi đè về hiện tại mỗi lần sửa tiêu đề. Xóa cache
        /// catalog để landing thấy nội dung mới ngay.
        /// </summary>
        /// <param name="id">Id bài viết cần sửa.</param>
        /// <param name="input">Dữ liệu mới (tiêu đề, nội dung, ảnh, ngày đăng, trạng thái).</param>
        /// <returns>Bài viết sau khi cập nhật.</returns>
        public virtual async Task<ArticleDto> UpdateAsync(Guid id, CreateArticleDto input)
        {
            var article = await _articles.GetAsync(id);
            article.Title = input.Title.Trim();
            article.Body = input.Body.Trim();
            article.ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim();
            if (input.PublishedAt.HasValue)
            {
                article.PublishedAt = input.PublishedAt.Value.ToUniversalTime();
            }

            article.IsPublished = input.IsPublished;
            await _articles.UpdateAsync(article, autoSave: true);
            _catalogCache.Invalidate();
            return Map(article);
        }

        /// <summary>
        /// Admin: xóa bài viết. Xóa cache catalog để landing không còn
        /// hiển thị bài đã xóa.
        /// </summary>
        /// <param name="id">Id bài viết cần xóa.</param>
        public virtual async Task DeleteAsync(Guid id)
        {
            await _articles.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
        }

        /// <summary>
        /// Public: lấy các bài viết đã xuất bản (mới nhất trước) để hiển thị
        /// ở trang landing. Kết quả cache theo key "articles:published" — admin
        /// tạo/sửa/xóa bài sẽ xóa cache này nên landing luôn thấy bài mới.
        /// </summary>
        /// <returns>Danh sách bài viết đã xuất bản (tối đa 6 bài mới nhất).</returns>
        [AllowAnonymous]
        public virtual async Task<List<ArticleDto>> GetPublishedAsync()
        {
            return await _catalogCache.GetOrAddAsync("articles:published", async () =>
            {
                var queryable = await _articles.GetQueryableAsync();
                // Perf: the only consumer (landing Index.razor) renders Take(6);
                // don't materialize/map full Body of every published article.
                // (Tối ưu hiệu năng: landing chỉ hiển thị tối đa 6 bài nên chỉ
                // lấy 6 bài mới nhất, không nạp toàn bộ nội dung Body của mọi
                // bài đã xuất bản.)
                var items = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsPublished)
                        .OrderByDescending(x => x.PublishedAt)
                        .Take(6));
                return items.Select(Map).ToList();
            });
        }

        /// <summary>Map entity bài viết → DTO trả về API.</summary>
        private static ArticleDto Map(Article a) => new ArticleDto
        {
            Id = a.Id,
            Title = a.Title,
            Body = a.Body,
            ImageUrl = a.ImageUrl,
            PublishedAt = a.PublishedAt,
            IsPublished = a.IsPublished
        };
    }
}
