using elando.ELK.TraceLogging.Sinks;
using Serilog;
using Xunit;

namespace elando.ELK.TraceLogging.Tests
{
    /// <summary>
    /// Locks in what the replacement client actually puts on the wire, because that is what the
    /// Elasticsearch role and the in-cluster network have to accept.
    /// </summary>
    public class WireProtocolTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        private static async Task<FakeElasticsearch> LogOneEventAsync(FakeElasticsearch elasticsearch)
        {
            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                       .CreateLogger())
            {
                logger.Information("hello {X}", 1);
            }

            await elasticsearch.WaitForRequestAsync("_bulk", Timeout);

            // Give any follow-up round-trip (a product check, a ping, a sniff) a chance to land
            // before asserting that there is none.
            await Task.Delay(500);
            return elasticsearch;
        }

        [Fact]
        public async Task Logging_costs_exactly_one_request_and_no_handshake()
        {
            using var elasticsearch = new FakeElasticsearch();
            await LogOneEventAsync(elasticsearch);

            // No product-check GET /, no ping, no node sniffing. This matters twice over: it means
            // the anonymous legacy_logger needs no privilege beyond the write itself, and it means
            // the swap adds no extra round-trip per batch.
            var request = Assert.Single(elasticsearch.Requests);
            Assert.Equal("POST", request.Method);
            Assert.StartsWith("/euroins-health-api/_bulk", request.Target);
        }

        [Fact]
        public async Task Request_bodies_are_gzipped()
        {
            using var elasticsearch = new FakeElasticsearch();
            await LogOneEventAsync(elasticsearch);

            // This is the one genuinely new thing on the wire versus Serilog.Sinks.Elasticsearch,
            // which posted plain text. Elastic.Transport compresses by default. Asserted so that a
            // future transport upgrade silently turning it off - or on - is visible here.
            var request = Assert.Single(elasticsearch.Requests);
            Assert.Equal("gzip", request.Headers["Content-Encoding"]);
            Assert.Equal("application/json", request.Headers["Content-Type"]);
        }

        [Fact]
        public async Task Batches_are_capped_at_fifty_documents()
        {
            using var elasticsearch = new FakeElasticsearch();

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                       .CreateLogger())
            {
                for (var i = 0; i < 120; i++) logger.Information("Event {Index}", i);
            }

            await elasticsearch.WaitForDocumentsAsync(120, Timeout);

            // 50 is the batch size the previous sink used (BatchPostingLimit). It is pinned rather
            // than left at this library's default of 1000 so that a SIGKILL - services here are
            // routinely OOMKilled - can only lose a small batch. If someone drops the explicit
            // BufferOptions, this fails.
            var documentsPerRequest = elasticsearch.Requests
                .Select(r => r.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length / 2)
                .Where(count => count > 0)
                .ToList();

            Assert.All(documentsPerRequest, count => Assert.InRange(count, 1, 50));
            Assert.True(documentsPerRequest.Count >= 3,
                $"120 events capped at 50 should need at least 3 requests, got {documentsPerRequest.Count}: [{string.Join(", ", documentsPerRequest)}]");
            Assert.Equal(120, documentsPerRequest.Sum());
        }

        [Fact]
        public async Task Bulk_response_is_filtered_down_to_the_fields_the_client_needs()
        {
            using var elasticsearch = new FakeElasticsearch();
            await LogOneEventAsync(elasticsearch);

            // filter_path is a response-shaping query parameter, not an action, so it has no bearing
            // on what the legacy_writer role must permit - recorded here so the extra query string
            // is not mistaken for a permissions change when someone reads the ES access logs.
            var request = Assert.Single(elasticsearch.Requests);
            Assert.Contains("filter_path=", request.Target);
        }
    }
}
