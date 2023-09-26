using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace elando.ELK.TraceLogging.Extensions
{
    /// <summary>
    /// IHost extensions to config the Serrilog and Elstic Search
    /// </summary>
    public static class HostExtension
    {
        /// <summary>
        /// Extensions to config the Serrilog and Elstic Search
        /// </summary>
        /// <param name="host"></param>
        /// <param name="configuration"></param>
        /// <param name="logLevel"></param>
        public static void AddSerilogLogger(
            this IHostBuilder host,
            IConfiguration configuration,
            string assemblyName,
            LogEventLevel logLevel = LogEventLevel.Information)
        {
            var prefix = configuration.GetPrefix().ToLower();
            var elasticUri = configuration.GetElasticUriUri();
            assemblyName = assemblyName.ToLower().Replace(".", "-").Replace(prefix, "");

            host.UseSerilog((hostContext, services, _configuration) =>
            {

                _configuration
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext()
                            .AddElasticLogging(
                                    elasticUri: elasticUri,
                                    indexPrefix: $"{prefix}-{assemblyName}",
                                    minLoggingLevel: logLevel,
                                    configuration: configuration);
            });
        }
    }
}
