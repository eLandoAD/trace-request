using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class SerrilogConfigurationExtensions
    {
        /// <summary>
        /// Add Serrilog configuration to Elsticsearch  
        /// </summary>
        /// <param name="host"></param>
        /// <param name="elasticUri"></param>
        /// <param name="indexPrefix"></param>
        /// <param name="minLoggingLevel"></param>
        /// <returns></returns>
        public static LoggerConfiguration AddElasticLogging(this LoggerConfiguration configuration, string elasticUri, string indexPrefix, LogEventLevel minLoggingLevel)
        {

            configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                IndexFormat = $"{indexPrefix}-{DateTime.UtcNow:yyyy-MM-dd-HH}",
                LevelSwitch = new LoggingLevelSwitch(minLoggingLevel),
            });

            return configuration;
        }
    }
}
