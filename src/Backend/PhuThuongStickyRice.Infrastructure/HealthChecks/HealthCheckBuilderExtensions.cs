using PhuThuongStickyRice.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddHttp(
            this IHealthChecksBuilder builder,
            string uri,
            string name = default,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default,
            TimeSpan? timeout = default)
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                new HttpHealthCheck(uri),
                failureStatus,
                tags,
                timeout));
        }

        public static IHealthChecksBuilder AddSqlServer(
            this IHealthChecksBuilder builder,
            string connectionString,
            string healthQuery = default,
            string name = default,
            HealthStatus? failureStatus = default,
            IEnumerable<string> tags = default,
            TimeSpan? timeout = default)
        {
            return builder.Add(new HealthCheckRegistration(
                name,
                new SqlServerHealthCheck(connectionString, healthQuery),
                failureStatus,
                tags,
                timeout));
        }
    }
}
