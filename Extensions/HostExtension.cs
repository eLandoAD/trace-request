using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace elando.ELK.TraceLogging.Extensions
{
    /// <summary>
    /// IHost extensions to config the Serrilog and Elstic Search
    /// </summary>
    public static class HostExtension
    {
        /// <summary>
        /// Extensions to config the Serrilog and ElasticSearch
        /// </summary>
        /// <param name="host"></param>
        /// <param name="configuration"></param>
        /// <param name="assemblyName"></param>
        /// <param name="logLevel"></param>
        public static void AddSerilogLogger(
            this IHostBuilder host,
            IConfiguration configuration,
            string assemblyName,
            LogEventLevel logLevel = LogEventLevel.Information)
        {
            var prefix = configuration.GetPrefix();
            var indexPrefix = FormatLogIndex(prefix, assemblyName);
            var elasticUri = configuration.GetElasticUriUri();

            host.UseSerilog((hostContext, services, _configuration) =>
            {
                _configuration
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext()
                            .WriteTo.Console(levelSwitch: new LoggingLevelSwitch(logLevel))
                            .WriteTo.Logger(loggerConfig =>
                                loggerConfig.AddElasticLogging(
                                                        elasticUri: elasticUri,
                                                        indexPrefix: indexPrefix,
                                                        minLoggingLevel: logLevel,
                                                        configuration: configuration));
            });
        }

        /// <summary>
        ///  Extensions to config the Sentry into Serrilog and ElasticSearch
        /// </summary>
        /// <param name="host"></param>
        /// <param name="configuration"></param>
        /// <param name="assemblyName"></param>
        /// <param name="logLevel"></param>
        /// <param name="minimumBreadcrumbLevel"></param>
        /// <param name="minimumEventLevel"></param>
        public static void AddSerilogLoggerWithSentry(
           this IHostBuilder host,
           IConfiguration configuration,
           string assemblyName,
           LogEventLevel logLevel = LogEventLevel.Information,
           LogEventLevel minimumBreadcrumbLevel = LogEventLevel.Debug,
           LogEventLevel minimumEventLevel = LogEventLevel.Warning
            )
        {
            var prefix = configuration.GetPrefix();
            var indexPrefix = FormatLogIndex(prefix, assemblyName);
            var elasticUri = configuration.GetElasticUriUri();

            host.UseSerilog((hostContext, services, _configuration) =>
            {
                _configuration
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext()
                            .WriteTo.Console(levelSwitch: new LoggingLevelSwitch(logLevel))
                            .WriteTo.Logger(loggerConfig =>
                                loggerConfig.AddElasticLogging(
                                                        elasticUri: elasticUri,
                                                        indexPrefix: indexPrefix,
                                                        minLoggingLevel: logLevel,
                                                        configuration: configuration))
                            .WriteTo.Sentry(o => o.AddSentryLogging(
                                minimumBreadcrumbLevel: minimumBreadcrumbLevel,
                                minimumEventLevel: minimumEventLevel,
                                configuration: configuration));
            });
        }

        private static string FormatLogIndex(string prefix, string assemblyName)
        {
            prefix = prefix.ToLower();
            assemblyName = assemblyName.ToLower().Replace(".", "-").Replace(prefix, "");

            return $"{prefix}-{assemblyName}".Replace("--", "-");
        }
    }
}
