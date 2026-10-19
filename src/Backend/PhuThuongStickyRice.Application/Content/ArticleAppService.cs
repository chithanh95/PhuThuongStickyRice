
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
        Task<List<ArticleDto>> GetListAsync();

        Task<ArticleDto> CreateAsync(CreateArticleDto input);

        Task<ArticleDto> UpdateAsync(Guid id, CreateArticleDto input);

        Task DeleteAsync(Guid id);

        /// <summary>Public: published articles, newest first.</summary>
        Task<List<ArticleDto>> GetPublishedAsync();
    }

    [Authorize(Roles = "Admin")]
    public class ArticleAppService : ApplicationService, IArticleAppService
    {
        private readonly IRepository<Article, Guid> _articles;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly Products.IProductCatalogCache _catalogCache;

        public ArticleAppService(
            IRepository<Article, Guid> articles,
            IAsyncQueryableExecuter asyncExecuter,
            Products.IProductCatalogCache catalogCache)
        {
            _articles = articles;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        public virtual async Task<List<ArticleDto>> GetListAsync()
        {
            var queryable = await _articles.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.PublishedAt));
            return items.Select(Map).ToList();
        }

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

        public virtual async Task DeleteAsync(Guid id)
        {
            await _articles.DeleteAsync(id, autoSave: true);
            _catalogCache.Invalidate();
        }

        [AllowAnonymous]
        public virtual async Task<List<ArticleDto>> GetPublishedAsync()
        {
            return await _catalogCache.GetOrAddAsync("articles:published", async () =>
            {
                var queryable = await _articles.GetQueryableAsync();
                // Perf: the only consumer (landing Index.razor) renders Take(6);
                // don't materialize/map full Body of every published article.
                var items = await _asyncExecuter.ToListAsync(
                    queryable.Where(x => x.IsPublished)
                        .OrderByDescending(x => x.PublishedAt)
                        .Take(6));
                return items.Select(Map).ToList();
            });
        }

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
