using System.Text.Json;
using elando.ELK.TraceLogging.Sinks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace elando.ELK.TraceLogging.Tests
{
    /// <summary>
    /// End-to-end over a real socket: what matters for the 10.0.2 swap is not just the document
    /// body but that it still lands in the same flat index, through the same bulk action, because
    /// the Elasticsearch role "legacy_writer" allow-lists index names and grants only
    /// auto_configure + create.
    /// </summary>
    public class ElasticsearchIndexSinkTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        [Fact]
        public async Task Writes_to_the_literal_index_name_with_an_index_bulk_action()
        {
            using var elasticsearch = new FakeElasticsearch();

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                       .CreateLogger())
            {
                logger.Information("Policy {PolicyNumber} issued", "BG/03/126000123456");
            }

            var bulk = await elasticsearch.WaitForRequestAsync("_bulk", Timeout);

            // The target index is carried in the request path rather than repeated on every action
            // line. It must be the name as given - no date suffix, and above all not a
            // "{type}-{dataset}-{namespace}" data stream.
            Assert.StartsWith("/euroins-health-api/_bulk", bulk.Target);

            var lines = bulk.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);

            using var action = JsonDocument.Parse(lines[0]);
            var operation = Assert.Single(action.RootElement.EnumerateObject().ToArray());
            Assert.Equal("index", operation.Name);

            // No explicit _id: documents get a server-generated one, so they never overwrite an
            // existing document and the role's "create" privilege is sufficient.
            Assert.False(operation.Value.TryGetProperty("_id", out _));

            using var document = JsonDocument.Parse(lines[1]);
            Assert.Equal("Information", document.RootElement.GetProperty("level").GetString());
            Assert.Equal("Policy {PolicyNumber} issued", document.RootElement.GetProperty("messageTemplate").GetString());
            Assert.Equal("Policy \"BG/03/126000123456\" issued", document.RootElement.GetProperty("message").GetString());
            Assert.Equal("BG/03/126000123456", document.RootElement.GetProperty("fields").GetProperty("PolicyNumber").GetString());
        }

        [Fact]
        public async Task Does_not_register_an_index_template()
        {
            using var elasticsearch = new FakeElasticsearch();

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-health-api"))
                       .CreateLogger())
            {
                logger.Information("anything");
            }

            await elasticsearch.WaitForRequestAsync("_bulk", Timeout);

            // The old sink called the Elasticsearch 6 legacy _template API, which the anonymous
            // legacy_logger user has no privilege to use. Nothing should attempt it now.
            Assert.DoesNotContain(elasticsearch.Requests, r =>
                r.Target.Contains("_template", StringComparison.OrdinalIgnoreCase) ||
                r.Target.Contains("_index_template", StringComparison.OrdinalIgnoreCase) ||
                r.Target.Contains("_ilm", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Batches_multiple_events_into_one_bulk_request()
        {
            using var elasticsearch = new FakeElasticsearch();

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-ssp-api"))
                       .CreateLogger())
            {
                for (var i = 0; i < 5; i++) logger.Information("Event {Index}", i);
            }

            var bulk = await elasticsearch.WaitForRequestAsync("_bulk", Timeout);

            Assert.StartsWith("/euroins-ssp-api/_bulk", bulk.Target);

            var lines = bulk.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(10, lines.Length); // one action line + one document per event
            Assert.All(lines.Where((_, i) => i % 2 == 0), line => Assert.Contains("\"index\"", line));
        }

        [Fact]
        public async Task Level_switch_suppresses_events_below_the_minimum()
        {
            using var elasticsearch = new FakeElasticsearch();
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);

            using (var logger = new LoggerConfiguration()
                       .MinimumLevel.Verbose()
                       .WriteTo.Sink(new ElasticsearchIndexSink(elasticsearch.Uri.ToString(), "euroins-admin", levelSwitch))
                       .CreateLogger())
            {
                logger.Debug("dropped debug");
                logger.Information("dropped information");
                logger.Warning("kept warning");
            }

            var bulk = await elasticsearch.WaitForRequestAsync("_bulk", Timeout);

            var lines = bulk.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            using var document = JsonDocument.Parse(lines[1]);
            Assert.Equal("Warning", document.RootElement.GetProperty("level").GetString());
        }

        [Fact]
        public void An_unreachable_elasticsearch_never_throws_at_the_call_site()
        {
            // Nothing is listening on this port. Logging is not allowed to take a request down with
            // it, and with BoundedChannelFullMode.DropWrite it must not block either.
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new ElasticsearchIndexSink("http://127.0.0.1:1/", "euroins-admin"))
                .CreateLogger();

            var exception = Record.Exception(() =>
            {
                for (var i = 0; i < 100; i++) logger.Information("Event {Index}", i);
            });

            Assert.Null(exception);
        }
    }
}
