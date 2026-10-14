using PhuThuongStickyRice.Application.Wave4.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace PhuThuongStickyRice.Application.Wave4
{
    /// <summary>
    /// Dispatches outbound webhooks (item 110). Fire-and-log: failures are
    /// recorded in <see cref="WebhookDelivery"/> and never break the order flow.
    /// </summary>
    public interface IWebhookDispatcher : ITransientDependency
    {
        Task DispatchAsync(string eventName, object payload);
    }

    public class WebhookDispatcher : IWebhookDispatcher
    {
        private readonly IRepository<WebhookEndpoint, Guid> _endpoints;
        private readonly IRepository<WebhookDelivery, Guid> _deliveries;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IClock _clock;

        public WebhookDispatcher(
            IRepository<WebhookEndpoint, Guid> endpoints,
            IRepository<WebhookDelivery, Guid> deliveries,
            IAsyncQueryableExecuter asyncExecuter,
            IHttpClientFactory httpClientFactory,
            IClock clock)
        {
            _endpoints = endpoints;
            _deliveries = deliveries;
            _asyncExecuter = asyncExecuter;
            _httpClientFactory = httpClientFactory;
            _clock = clock;
        }

        public async Task DispatchAsync(string eventName, object payload)
        {
            var queryable = await _endpoints.GetQueryableAsync();
            var endpoints = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.IsActive));

            foreach (var endpoint in endpoints)
            {
                var events = (endpoint.Events ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
                if (!events.Any(x => string.Equals(x, eventName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                await SendAsync(endpoint, eventName, payload, ping: false);
            }
        }

        /// <summary>Admin "Gửi thử" button: posts a ping event.</summary>
        public async Task<WebhookDeliveryDto> SendTestAsync(Guid endpointId)
        {
            var endpoint = await _endpoints.GetAsync(endpointId);
            return await SendAsync(endpoint, "Ping", new
            {
                message = "Kết nối webhook OK",
                at = _clock.Now
            }, ping: true);
        }

        private async Task<WebhookDeliveryDto> SendAsync(
            WebhookEndpoint endpoint, string eventName, object payload, bool ping)
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                @event = eventName,
                at = _clock.Now,
                data = payload
            });

            bool success = false;
            int? statusCode = null;
            string error = null;

            try
            {
                var client = _httpClientFactory.CreateClient("Webhooks");
                client.Timeout = TimeSpan.FromSeconds(10);

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(endpoint.Secret))
                {
                    request.Headers.Add(
                        "X-Webhook-Signature",
                        ComputeSignature(endpoint.Secret.Trim(), body));
                }

                var response = await client.SendAsync(request);
                statusCode = (int)response.StatusCode;
                success = response.IsSuccessStatusCode;
                if (!success)
                {
                    error = $"HTTP {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + (ex.Message ?? string.Empty);
                if (error.Length > 2000)
                {
                    error = error.Substring(0, 2000);
                }
            }

            var delivery = new WebhookDelivery
            {
                At = _clock.Now,
                EndpointId = endpoint.Id,
                EndpointName = endpoint.Name,
                Event = ping ? "Ping" : eventName,
                Success = success,
                StatusCode = statusCode,
                Error = error
            };
            await _deliveries.InsertAsync(delivery, autoSave: true);

            return new WebhookDeliveryDto
            {
                Id = delivery.Id,
                At = delivery.At,
                EndpointId = delivery.EndpointId,
                EndpointName = delivery.EndpointName,
                Event = delivery.Event,
                Success = delivery.Success,
                StatusCode = delivery.StatusCode,
                Error = delivery.Error
            };
        }

        /// <summary>HMAC-SHA256 of the raw body, hex-encoded.</summary>
        public static string ComputeSignature(string secret, string body)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    public interface IWebhookAppService : IApplicationService
    {
        Task<List<WebhookEndpointDto>> GetListAsync();
        Task<WebhookEndpointDto> GetAsync(Guid id);
        Task<WebhookEndpointDto> CreateAsync(CreateUpdateWebhookEndpointDto dto);
        Task<WebhookEndpointDto> UpdateAsync(Guid id, CreateUpdateWebhookEndpointDto dto);
        Task DeleteAsync(Guid id);
        Task<WebhookDeliveryDto> SendTestAsync(Guid id);
        Task<List<WebhookDeliveryDto>> GetDeliveriesAsync(Guid? endpointId, int maxCount);
    }

    public class WebhookAppService : ApplicationService, IWebhookAppService
    {
        private readonly IRepository<WebhookEndpoint, Guid> _endpoints;
        private readonly IRepository<WebhookDelivery, Guid> _deliveries;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly WebhookDispatcher _dispatcher;

        public WebhookAppService(
            IRepository<WebhookEndpoint, Guid> endpoints,
            IRepository<WebhookDelivery, Guid> deliveries,
            IAsyncQueryableExecuter asyncExecuter,
            WebhookDispatcher dispatcher)
        {
            _endpoints = endpoints;
            _deliveries = deliveries;
            _asyncExecuter = asyncExecuter;
            _dispatcher = dispatcher;
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<WebhookEndpointDto>> GetListAsync()
        {
            var queryable = await _endpoints.GetQueryableAsync();
            var entities = await _asyncExecuter.ToListAsync(
                queryable.OrderBy(x => x.Name));
            return entities.Select(MapToDto).ToList();
        }

        [Authorize(Roles = "Admin")]
        public async Task<WebhookEndpointDto> GetAsync(Guid id)
        {
            return MapToDto(await _endpoints.GetAsync(id));
        }

        [Authorize(Roles = "Admin")]
        public async Task<WebhookEndpointDto> CreateAsync(CreateUpdateWebhookEndpointDto dto)
        {
            var entity = new WebhookEndpoint
            {
                Name = dto.Name.Trim(),
                Url = dto.Url.Trim(),
                Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret.Trim(),
                Events = NormalizeEvents(dto.Events),
                IsActive = dto.IsActive
            };
            await _endpoints.InsertAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task<WebhookEndpointDto> UpdateAsync(Guid id, CreateUpdateWebhookEndpointDto dto)
        {
            var entity = await _endpoints.GetAsync(id);
            entity.Name = dto.Name.Trim();
            entity.Url = dto.Url.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Secret))
            {
                entity.Secret = dto.Secret.Trim();
            }

            entity.Events = NormalizeEvents(dto.Events);
            entity.IsActive = dto.IsActive;
            await _endpoints.UpdateAsync(entity, autoSave: true);
            return MapToDto(entity);
        }

        [Authorize(Roles = "Admin")]
        public async Task DeleteAsync(Guid id)
        {
            await _endpoints.DeleteAsync(id, autoSave: true);
        }

        [Authorize(Roles = "Admin")]
        public async Task<WebhookDeliveryDto> SendTestAsync(Guid id)
        {
            return await _dispatcher.SendTestAsync(id);
        }

        [Authorize(Roles = "Admin")]
        public async Task<List<WebhookDeliveryDto>> GetDeliveriesAsync(Guid? endpointId, int maxCount)
        {
            var queryable = await _deliveries.GetQueryableAsync();
            if (endpointId.HasValue)
            {
                queryable = queryable.Where(x => x.EndpointId == endpointId.Value);
            }

            var deliveries = await _asyncExecuter.ToListAsync(
                queryable.OrderByDescending(x => x.At).Take(
                    maxCount < 1 ? 50 : Math.Min(maxCount, 200)));

            return deliveries.Select(x => new WebhookDeliveryDto
            {
                Id = x.Id,
                At = x.At,
                EndpointId = x.EndpointId,
                EndpointName = x.EndpointName,
                Event = x.Event,
                Success = x.Success,
                StatusCode = x.StatusCode,
                Error = x.Error
            }).ToList();
        }

        private static string NormalizeEvents(string events)
        {
            if (string.IsNullOrWhiteSpace(events))
            {
                return WebhookEndpoint.EventOrderCreated;
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                WebhookEndpoint.EventOrderCreated,
                WebhookEndpoint.EventOrderStatusChanged
            };
            var picked = events.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(allowed.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return picked.Count == 0 ? WebhookEndpoint.EventOrderCreated : string.Join(",", picked);
        }

        private static WebhookEndpointDto MapToDto(WebhookEndpoint entity)
        {
            return new WebhookEndpointDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Url = entity.Url,
                HasSecret = !string.IsNullOrWhiteSpace(entity.Secret),
                Events = entity.Events,
                IsActive = entity.IsActive
            };
        }
    }
}
