using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class HostExtension
    {
        public static void AddSerilogLogger(this IHostBuilder host, IConfiguration configuration)
        {
            var prefix = configuration.GetPrefix();
            host.UseSerilog((hostContext, services, _configuration) =>
            {

                _configuration
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext()
                            .AddElasticLogging(
                                    elasticUri: "http://elasticsearch.kube-logging:9200/",
                                    indexPrefix: $"{prefix}-{Assembly.GetExecutingAssembly().GetName().Name!.ToLower().Replace(".", "-")}",
                                    minLoggingLevel: LogEventLevel.Information)
                            ;
            });
        }
    }
}
