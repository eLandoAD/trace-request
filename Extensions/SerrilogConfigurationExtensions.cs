using Microsoft.Extensions.Configuration;
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
        public static LoggerConfiguration AddElasticLogging(
            this LoggerConfiguration logConfiguration,
            string elasticUri,
            string indexPrefix,
            LogEventLevel minLoggingLevel,
            IConfiguration configuration)
        {
            var logFilter = configuration.GetLogFilter();

            if (string.IsNullOrWhiteSpace(logFilter))
            {
                logConfiguration
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                    {
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                        IndexFormat = $"{indexPrefix}",
                        LevelSwitch = new LoggingLevelSwitch(minLoggingLevel),
                    });
            }
            else
            {
                logConfiguration
                    .Filter.ByIncludingOnly(logEvent => logEvent.MessageTemplate.Text.Contains(logFilter))
                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                    {
                        AutoRegisterTemplate = true,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                        IndexFormat = $"{indexPrefix}",
                        LevelSwitch = new LoggingLevelSwitch(minLoggingLevel),
                    });
            }

            return logConfiguration;
        }
    }
}
