using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Transport;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace elando.ELK.TraceLogging.Sinks
{
    /// <summary>
    /// Serilog sink that bulk-writes log events to a single, fixed Elasticsearch index.
    ///
    /// Replaces Serilog.Sinks.Elasticsearch, which shipped Elasticsearch.Net 7.17.5 - a client a
    /// full major behind the 8.x server - and registered its index template through the
    /// Elasticsearch 6 legacy _template API.
    ///
    /// The official successor, Elastic.Serilog.Sinks, is deliberately NOT used: it can only target
    /// data streams (its DataStreamName always renders "{type}-{dataset}-{namespace}"), whereas the
    /// euroins Elasticsearch role "legacy_writer" grants auto_configure + create on an explicit
    /// allow-list of flat index names. Adopting data streams would rename every index, so every
    /// service's writes would be rejected until the role, the index templates and the Kibana data
    /// views were rebuilt in both clusters. Writing straight to the IndexChannel underneath that
    /// sink keeps the index names, the role and the dashboards exactly as they are.
    /// </summary>
    public sealed class ElasticsearchIndexSink : ILogEventSink, IDisposable
    {
        /// <summary>
        /// Serilog's JsonValueFormatter left non-ASCII unescaped, and Bulgarian log messages are the
        /// common case here. The default encoder would escape every Cyrillic character to \uXXXX,
        /// roughly doubling the payload for no gain. "Unsafe" refers to HTML-injection contexts;
        /// these bytes go straight into an Elasticsearch _bulk body.
        /// </summary>
        private static readonly JsonWriterOptions WriterOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            SkipValidation = true,
        };

        private readonly IndexChannel<LogEvent> _channel;
        private readonly LoggingLevelSwitch? _levelSwitch;

        public ElasticsearchIndexSink(string elasticUri, string indexName, LoggingLevelSwitch? levelSwitch = null)
        {
            if (string.IsNullOrWhiteSpace(elasticUri)) throw new ArgumentException("Elasticsearch uri is required.", nameof(elasticUri));
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentException("Index name is required.", nameof(indexName));

            _levelSwitch = levelSwitch;

            var transport = new DistributedTransport(new TransportConfiguration(new Uri(elasticUri)));

            var options = new IndexChannelOptions<LogEvent>(transport)
            {
                // IndexFormat is a composite format string over the event timestamp. The index names
                // built by HostExtension.FormatLogIndex contain no placeholder, so this stays the
                // literal, unrotated index the old sink's IndexFormat produced.
                IndexFormat = indexName,

                // "index" bulk operations, as the old sink's default BatchAction. The legacy_writer
                // role's "create" privilege covers these because documents are written with
                // server-generated ids and so never overwrite an existing one.
                OperationMode = OperationMode.Index,

                TimestampLookup = logEvent => logEvent.Timestamp,
                EventWriter = new LogEventWriter(),
                BufferOptions = new BufferOptions
                {
                    // The library default is 100_000 events. Several services run under a 256Mi
                    // memory limit and already sit close to it, so a burst of logging must not be
                    // able to add ~100MB of retained LogEvents on top.
                    InboundBufferMaxSize = 10_000,

                    // The library default is Wait, which blocks the *calling* thread once the buffer
                    // fills - so a slow or unreachable Elasticsearch would stall request threads.
                    // The old PeriodicBatchingSink dropped instead; keep dropping.
                    BoundedChannelFullMode = System.Threading.Channels.BoundedChannelFullMode.DropWrite,
                },

                // The sink swallows write failures unless something is listening, which is how five
                // services were found to have silently logged nothing at all. Route every failure
                // path to Serilog's SelfLog so it is visible wherever SelfLog is enabled.
                ExportExceptionCallback = e => SelfLog.WriteLine("[Elasticsearch] export failed: {0}", e),
                ServerRejectionCallback = rejections => SelfLog.WriteLine("[Elasticsearch] server rejected {0} item(s)", rejections.Count),
                BufferItemDropped = _ => SelfLog.WriteLine("[Elasticsearch] buffer full, log event dropped"),
                ExportMaxRetriesCallback = items => SelfLog.WriteLine("[Elasticsearch] gave up on {0} item(s) after max retries", items.Count),
            };

            _channel = new IndexChannel<LogEvent>(options);

            // Deliberately no BootstrapElasticsearch call. The old sink asked for AutoRegisterTemplate
            // against the Elasticsearch 6 _template API, which the anonymous legacy_logger user has
            // never had the cluster privileges to perform - so it was a no-op that only produced
            // noise. Index templates stay a deliberate, out-of-band operation.
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent is null) return;
            if (_levelSwitch is not null && logEvent.Level < _levelSwitch.MinimumLevel) return;

            _channel.TryWrite(logEvent);
        }

        public void Dispose()
        {
            // Flush what is still buffered rather than dropping it on shutdown. Bounded, because a
            // pod being terminated must not hang on an unreachable Elasticsearch.
            try
            {
                _channel.TryComplete();
                _channel.WaitForDrainAsync(TimeSpan.FromSeconds(5)).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                SelfLog.WriteLine("[Elasticsearch] drain on shutdown failed: {0}", e);
            }

            _channel.Dispose();
        }

        private sealed class LogEventWriter : IElasticsearchEventWriter<LogEvent>
        {
            public Action<ArrayBufferWriter<byte>, LogEvent>? WriteToArrayBuffer { get; set; } = (buffer, logEvent) =>
            {
                using var writer = new Utf8JsonWriter(buffer, WriterOptions);
                ElasticsearchDocumentWriter.Write(writer, logEvent);
            };

            public Func<Stream, LogEvent, CancellationToken, Task>? WriteToStreamAsync { get; set; } = async (stream, logEvent, ctk) =>
            {
                await using var writer = new Utf8JsonWriter(stream, WriterOptions);
                ElasticsearchDocumentWriter.Write(writer, logEvent);
                await writer.FlushAsync(ctk).ConfigureAwait(false);
            };
        }
    }
}
