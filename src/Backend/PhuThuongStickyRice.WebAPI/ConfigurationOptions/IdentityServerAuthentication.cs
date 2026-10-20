using CryptographyHelper.Certificates;

namespace PhuThuongStickyRice.WebAPI.ConfigurationOptions
{
    /// <summary>
    /// Cấu hình xác thực API: chọn provider (JWT tự cấp hoặc OpenIddict) và
    /// thông tin authority/audience để validate token.
    /// </summary>
    public class IdentityServerAuthentication
    {
        /// <summary>Tên provider xác thực đang dùng (vd: "JwtBearer", "OpenIddict").</summary>
        public string Provider { get; set; }

        /// <summary>URL của identity provider phát hành token.</summary>
        public string Authority { get; set; }

        /// <summary>Tên API (audience) mà token phải nhắm tới mới được chấp nhận.</summary>
        public string ApiName { get; set; }

        /// <summary>
        /// Có bắt buộc HTTPS khi tải metadata của provider không — tắt chỉ khi
        /// chạy dev/LAN nội bộ dùng HTTP.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; }

        /// <summary>Cấu hình riêng cho provider OpenIddict (issuer, chứng thư ký token).</summary>
        public OpenIddictOptions OpenIddict { get; set; }
    }

    /// <summary>Cấu hình OpenIddict: issuer và chứng thư dùng để ký/giải mã token.</summary>
    public class OpenIddictOptions
    {
        /// <summary>URI định danh issuer trong token (phải khớp với token đã phát hành).</summary>
        public string IssuerUri { get; set; }

        /// <summary>Chứng thư dùng để giải mã token.</summary>
        public CertificateOption TokenDecryptionCertificate { get; set; }

        /// <summary>Chứng thư dùng để ký token phát hành.</summary>
        public CertificateOption IssuerSigningCertificate { get; set; }
    }
}
