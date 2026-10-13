using Microsoft.Extensions.Configuration;

namespace PhuThuongStickyRice.Infrastructure.Configuration
{
    public static class SqlConfigurationExtensions
    {
        public static IConfigurationBuilder AddSqlServer(this IConfigurationBuilder builder, SqlServerOptions options)
        {
            return builder.Add(new SqlConfigurationSource(options));
        }
    }
}
