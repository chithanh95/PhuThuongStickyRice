using PhuThuongStickyRice.Application.Landing;
using PhuThuongStickyRice.Application.Landing.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API nội dung trang landing: public đọc nội dung hiển thị (hero, món
    /// nổi bật, câu chuyện...), admin sửa nội dung và upload ảnh ở trang
    /// /admin/noi-dung-landing. Ảnh upload tối đa 5MB.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    // Item 107: versioned prefix — both routes stay live.
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LandingController : ControllerBase
    {
        private static readonly HashSet<string> AllowedImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp", ".gif"
            };

        private const long MaxImageBytes = 5L * 1024 * 1024; // 5 MB

        private readonly ILandingAppService _landingAppService;
        private readonly IWebHostEnvironment _environment;

        public LandingController(
            ILandingAppService landingAppService,
            IWebHostEnvironment environment)
        {
            _landingAppService = landingAppService;
            _environment = environment;
        }

        /// <summary>
        /// Public: lấy toàn bộ nội dung hiển thị của trang landing
        /// (banner, món nổi bật, câu chuyện thương hiệu...).
        /// </summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("content")]
        public async Task<ActionResult> GetContent(CancellationToken cancellationToken = default)
        {
            var content = await _landingAppService.GetContentAsync();
            return Ok(content);
        }

        /// <summary>
        /// Admin lưu nội dung trang landing (sửa ở /admin/noi-dung-landing).
        /// Ghi đè toàn bộ nội dung — client phải gửi đủ các trường.
        /// </summary>
        /// <param name="dto">Nội dung landing mới.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("content")]
        public async Task<ActionResult> UpdateContent(
            [FromBody] LandingContentDto dto,
            CancellationToken cancellationToken = default)
        {
            var content = await _landingAppService.UpdateContentAsync(dto);
            return Ok(content);
        }

        /// <summary>
        /// Uploads one image for the landing page (admin only).
        /// Returns a relative URL like /uploads/landing/xxxx.jpg — the client
        /// prepends the API base URL to build the absolute URL.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("images")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult> UploadImage(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn một file ảnh." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Định dạng ảnh không hợp lệ. Chỉ nhận: jpg, jpeg, png, webp, gif."
                });
            }

            // bug-hunt 2026-10-19: kiểm tra chữ ký file (magic bytes) — đuôi
            // ảnh có thể là file thực thi/script đổi tên; chỉ nhận nội dung
            // ảnh thật (JPEG/PNG/GIF/WebP).
            if (!await ImageUploadValidator.HasValidImageSignatureAsync(file, extension, cancellationToken))
            {
                return BadRequest(new
                {
                    message = "File không phải ảnh hợp lệ (nội dung không khớp định dạng ảnh)."
                });
            }

            if (file.Length > MaxImageBytes)
            {
                return BadRequest(new { message = "File ảnh tối đa 5 MB." });
            }

            var folder = Path.Combine((_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot")), "uploads", "landing");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return Ok(new { url = $"/uploads/landing/{fileName}" });
        }
    }
}
