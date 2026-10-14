using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.WebAPI.Authorization;
using PhuThuongStickyRice.WebAPI.ConfigurationOptions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Issues self-signed JWTs for admin login. Tokens carry role claims and,
    /// for Admin users, one "permission" claim per product policy so the
    /// existing <see cref="AuthorizationPolicyNames"/> policies keep working.
    /// </summary>
    public class JwtTokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string Token, DateTimeOffset ExpiresAt) CreateToken(User user, IEnumerable<string> roles)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryMinutes);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            var isAdmin = false;
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAdmin = true;
                }
            }

            if (isAdmin)
            {
                // Permission claims for the 5 product policies.
                foreach (var policy in AuthorizationPolicyNames.GetPolicyNames())
                {
                    claims.Add(new Claim(
                        Infrastructure.Web.Authorization.Requirements.PermissionRequirementHandler.PermissionClaimType,
                        policy));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        /// <summary>
        /// Item 98: short-lived token for the 2FA step. Carries the user id
        /// plus purpose="2fa-pending"; never grants API access.
        /// </summary>
        public (string Token, DateTimeOffset ExpiresAt) CreateTwoFactorToken(User user)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim("purpose", "2fa-pending")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        /// <summary>
        /// Validates a 2FA token: signature, expiry, issuer/audience and the
        /// purpose claim. Returns the user id, or null when invalid.
        /// </summary>
        public Guid? ValidateTwoFactorToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _options.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                var purpose = principal.FindFirst("purpose")?.Value;
                if (!string.Equals(purpose, "2fa-pending", StringComparison.Ordinal))
                {
                    return null;
                }

                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(sub, out var userId) ? userId : (Guid?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
