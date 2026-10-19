using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PhuThuongStickyRice.Application.Common.ImageBackup
{
    /// <summary>
    /// Cloud backup for uploaded images (item 114). Implementations copy a
    /// local file to external storage. Register the desired provider by name
    /// in <c>ShopSetting.ImageBackupProvider</c> and its JSON settings in
    /// <c>ShopSetting.ImageBackupSettingsJson</c>.
    ///
    /// How to add a real provider (e.g. S3):
    /// 1. Create <c>S3ImageBackupProvider : IImageBackupProvider</c> with
    ///    <c>Name = "S3"</c>, injecting AWS SDK credentials from the settings
    ///    JSON (access key/secret/bucket — never commit them, keep them in
    ///    appsettings.Production.json or the ShopSetting JSON field).
    /// 2. Upload the file stream to the bucket under the same relative path
    ///    (e.g. "uploads/landing/abc.webp"), public-read or signed-URL per
    ///    your needs.
    /// 3. Nothing else to wire: the backup worker resolves providers by
    ///    name automatically via DI.
    /// </summary>
    public interface IImageBackupProvider : ITransientDependency
    {
        /// <summary>Provider name as stored in ShopSetting.ImageBackupProvider.</summary>
        string Name { get; }

        /// <summary>Copy a file to backup storage. Returns the remote key/URL.</summary>
        Task<string> BackupFileAsync(string relativePath, Stream content);

        /// <summary>True when the provider is configured and usable.</summary>
        Task<bool> IsConfiguredAsync();
    }

    /// <summary>
    /// Default provider: does nothing (local disk is the only copy). Chosen
    /// when <c>ImageBackupProvider</c> is "None" (default) or unknown.
    /// Null-object implementation of <see cref="IImageBackupProvider"/>.
    /// </summary>
    public class NullImageBackupProvider : IImageBackupProvider
    {
        public string Name => "None";

        public Task<string> BackupFileAsync(string relativePath, Stream content)
        {
            return Task.FromResult<string>(null);
        }

        public Task<bool> IsConfiguredAsync()
        {
            return Task.FromResult(true);
        }
    }
}
