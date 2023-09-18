using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class HostExtension
    {
        public static void AddLogger(this IHostBuilder host, IConfiguration configuration)
        {
            host.UseSerilog((hostContext, services, _configuration) =>
            {
                //var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
                //var requestId = httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(TRACE_IDENTIFIER_KEY, out var traceId);
                _configuration
                            .ReadFrom.Configuration(configuration)
                            .Enrich.FromLogContext()
                            .AddElasticLogging(
                                    elasticUri: "http://elasticsearch.kube-logging:9200/",
                                    indexPrefix: $"euroins-{Assembly.GetExecutingAssembly().GetName().Name!.ToLower().Replace(".", "-")}",
                                    minLoggingLevel: LogEventLevel.Information)
                            ;
            }); 
        }
    }
}
