using elando.ELK.TraceLogging.Sinks;
using Serilog;
using Xunit;

namespace elando.ELK.TraceLogging.Tests
{
    /// <summary>
    /// What happens when Elasticsearch says no. The staging role legacy_writer is write-only and
    /// index-scoped, so a misconfigured index name or a revoked privilege shows up as a 403 on every
    /// batch - that must degrade logging, never the service doing the logging.
    /// </summary>
    public class RejectionTests
    {
        private const string SecurityException =
            """{"error":{"type":"security_exception","reason":"action [indices:data/write/bulk] is unauthorized for user [legacy_logger]"},"status":403}""";

        [Fact]
        public async Task A_403_on_every_batch_never_throws_at_the_call_site()
        {
            using var elasticsearch = new FakeElasticsearch(statusCode: 403, responseBody: SecurityException);

            var exception = Record.Exception(() =>
            {
                using var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                    .CreateLogger();

                for (var i = 0; i < 200; i++) logger.Information("Event {Index}", i);
            });

            Assert.Null(exception);
            await elasticsearch.WaitForRequestAsync("_bulk", TimeSpan.FromSeconds(20));
        }

        [Fact]
        public async Task A_partial_bulk_rejection_never_throws_at_the_call_site()
        {
            // errors:true with a per-item failure - the shape ES returns when the mapping rejects a
            // single document but the batch as a whole was accepted.
            const string PartialFailure =
                """{"took":1,"errors":true,"items":[{"index":{"status":201}},{"index":{"status":400,"error":{"type":"mapper_parsing_exception","reason":"failed to parse"}}}]}""";

            using var elasticsearch = new FakeElasticsearch(statusCode: 200, responseBody: PartialFailure);

            var exception = Record.Exception(() =>
            {
                using var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                    .CreateLogger();

                logger.Information("first");
                logger.Information("second");
            });

            Assert.Null(exception);
            await elasticsearch.WaitForRequestAsync("_bulk", TimeSpan.FromSeconds(20));
        }

        [Fact]
        public void Disposal_completes_promptly_when_elasticsearch_is_unreachable()
        {
            // A terminating pod must not hang on the drain. Dispose caps the wait at 5s.
            var started = DateTime.UtcNow;

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink("http://127.0.0.1:1/", "euroins-health-api"))
                       .CreateLogger())
            {
                logger.Information("this will never be delivered");
            }

            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(15), $"disposal took {DateTime.UtcNow - started}");
        }
    }
}
