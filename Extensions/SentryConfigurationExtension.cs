using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog;
using Sentry.Serilog;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class SentryConfigurationExtension
    {
        public static SentrySerilogOptions AddSentryLogging(
                             this SentrySerilogOptions logConfiguration,
                            LogEventLevel minimumBreadcrumbLevel,
                            LogEventLevel minimumEventLevel,
                            IConfiguration configuration
            )
        {
            var dsn = configuration.GetSentryDsn();
            if (string.IsNullOrWhiteSpace(dsn))
            {
                throw new NullReferenceException("Unable to load Sentry Dsn! Add it to appsettings ad \"Sentry\":{\"Dsn\": <DSN value> }");
            }

            logConfiguration.Dsn = dsn;
            logConfiguration.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
            logConfiguration.MinimumEventLevel = minimumEventLevel;

            return logConfiguration;
        }
    }
}
