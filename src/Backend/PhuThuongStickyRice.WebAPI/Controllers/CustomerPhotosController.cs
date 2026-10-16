using PhuThuongStickyRice.Application.Wave5;
using PhuThuongStickyRice.Application.Wave5.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
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
    // ================= Customer photos (item 123) =================
    // Restored 2026-10-16: the dead-code sweep removed this controller as
    // "unreferenced", but the shop gallery (GalleryKhachHang), track-order
    // photo upload (TrackOrder) and admin photo moderation (DanhGia) all call
    // these endpoints. Route fixed to kebab-case to match the frontend
    // (Wave5Service calls api/customer-photos/*).

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/customer-photos")]
    [ApiController]
    public class CustomerPhotosController : ControllerBase
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly ICustomerPhotoAppService _photos;
        private readonly IWebHostEnvironment _environment;

        public CustomerPhotosController(
            ICustomerPhotoAppService photos,
            IWebHostEnvironment environment)
        {
            _photos = photos;
            _environment = environment;
        }

        /// <summary>Public: approved gallery photos.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<CustomerPhotoDto>>> GetApproved(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _photos.GetApprovedAsync());
        }

        /// <summary>
        /// Public: shares a photo of a received dish (from track-order).
        /// Goes to moderation first (IsApproved = false).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("upload")]
        [RequestSizeLimit(MaxImageBytes)]
        public async Task<ActionResult<CustomerPhotoDto>> Upload(
            [FromForm] IFormFile file,
            [FromForm] string phone,
            [FromForm] string caption,
            [FromForm] string name,
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

            if (file.Length > MaxImageBytes)
            {
                return BadRequest(new { message = "File ảnh tối đa 5 MB." });
            }

            var folder = Path.Combine(_environment.WebRootPath, "uploads", "customer-photos");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            try
            {
                var photo = await _photos.RegisterAsync(
                    $"/uploads/customer-photos/{fileName}",
                    new CreateCustomerPhotoDto { Phone = phone, Caption = caption, Name = name });
                return Ok(photo);
            }
            catch (DomainException ex)
            {
                System.IO.File.Delete(filePath);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<CustomerPhotoDto>>> GetList(
            [FromQuery] bool pendingOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _photos.GetListAsync(pendingOnly));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<ActionResult> Approve(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _photos.ApproveAsync(id);
            return Ok(new { message = "Đã duyệt ảnh." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Reject(Guid id,
            CancellationToken cancellationToken = default)
        {
            var photo = await _photos.RejectAsync(id);
            // Remove the file too.
            var fileName = Path.GetFileName(photo.ImageUrl ?? string.Empty);
            if (!string.IsNullOrEmpty(fileName))
            {
                var path = Path.Combine(
                    _environment.WebRootPath, "uploads", "customer-photos", fileName);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            return NoContent();
        }
    }
}
