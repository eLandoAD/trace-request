using elando.ELK.TraceLogging.Sinks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

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
                    .WriteTo.Elasticsearch(elasticUri, indexPrefix, minLoggingLevel);
            }
            else
            {
                logConfiguration
                    .Filter.ByIncludingOnly(logEvent => logEvent.MessageTemplate.Text.Contains(logFilter))
                    .WriteTo.Elasticsearch(elasticUri, indexPrefix, minLoggingLevel);
            }

            return logConfiguration;
        }

        /// <summary>
        /// Writes log events to a single Elasticsearch index.
        /// </summary>
        /// <remarks>
        /// Keeps the name <c>WriteTo.Elasticsearch(..)</c> that Serilog.Sinks.Elasticsearch used, so
        /// the call reads the same, but the sink underneath is
        /// <see cref="ElasticsearchIndexSink"/> - see that type for why the official
        /// Elastic.Serilog.Sinks replacement could not be used directly.
        /// </remarks>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration sinkConfiguration,
            string elasticUri,
            string indexName,
            LogEventLevel minLoggingLevel)
        {
            // Passed as a LoggingLevelSwitch rather than restrictedToMinimumLevel to match how the
            // previous sink was configured (ElasticsearchSinkOptions.LevelSwitch).
            var levelSwitch = new LoggingLevelSwitch(minLoggingLevel);

            return sinkConfiguration.Sink(new ElasticsearchIndexSink(elasticUri, indexName, levelSwitch));
        }
    }
}
