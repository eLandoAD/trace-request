using System.Net;
using System.Net.Sockets;
using System.Text;

namespace elando.ELK.TraceLogging.Tests
{
    /// <summary>
    /// A minimal HTTP/1.1 endpoint that records what the sink sends it.
    ///
    /// Deliberately a raw TcpListener rather than HttpListener: on Windows HttpListener needs a URL
    /// ACL reservation for anything but an elevated process, which would make the suite fail on
    /// some developer machines and in CI for reasons that have nothing to do with the sink.
    /// </summary>
    public sealed class FakeElasticsearch : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<CapturedRequest> _requests = new();
        private readonly object _gate = new();
        private readonly int _statusCode;
        private readonly string? _responseBody;

        public FakeElasticsearch(int statusCode = 200, string? responseBody = null)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}");
            _ = Task.Run(AcceptLoopAsync);
        }

        public Uri Uri { get; }

        public IReadOnlyList<CapturedRequest> Requests
        {
            get { lock (_gate) return _requests.ToArray(); }
        }

        /// <summary>Waits for a request whose target contains <paramref name="pathFragment"/>.</summary>
        public async Task<CapturedRequest> WaitForRequestAsync(string pathFragment, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var match = Requests.FirstOrDefault(r => r.Target.Contains(pathFragment, StringComparison.OrdinalIgnoreCase));
                if (match is not null) return match;
                await Task.Delay(25);
            }

            var seen = string.Join(", ", Requests.Select(r => $"{r.Method} {r.Target}"));
            throw new TimeoutException($"No request matching '{pathFragment}' within {timeout}. Saw: [{seen}]");
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(_cts.Token); }
                catch { return; }

                _ = Task.Run(() => HandleAsync(client));
            }
        }

        private async Task HandleAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();

                    // Read headers byte by byte: the body must not be consumed into a buffered
                    // reader, because its length is only known once Content-Length is parsed.
                    var head = new MemoryStream();
                    var window = new byte[4];
                    int b;
                    while ((b = stream.ReadByte()) != -1)
                    {
                        head.WriteByte((byte)b);
                        Array.Copy(window, 1, window, 0, 3);
                        window[3] = (byte)b;
                        if (window[0] == '\r' && window[1] == '\n' && window[2] == '\r' && window[3] == '\n') break;
                    }

                    if (head.Length == 0) return;

                    var headText = Encoding.UTF8.GetString(head.ToArray());
                    var lines = headText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                    var requestLine = lines[0].Split(' ');

                    var headers = lines.Skip(1)
                        .Select(l => l.Split(':', 2))
                        .Where(p => p.Length == 2)
                        .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

                    var body = string.Empty;
                    if (headers.TryGetValue("Content-Length", out var lengthText) && int.TryParse(lengthText, out var length) && length > 0)
                    {
                        var buffer = new byte[length];
                        var read = 0;
                        while (read < length)
                        {
                            var n = await stream.ReadAsync(buffer.AsMemory(read, length - read));
                            if (n == 0) break;
                            read += n;
                        }
                        // Elastic.Transport gzips request bodies by default - the old
                        // Serilog.Sinks.Elasticsearch sent them as plain text - so the bulk payload
                        // has to be inflated before it can be asserted on.
                        body = headers.TryGetValue("Content-Encoding", out var encoding) && encoding.Contains("gzip", StringComparison.OrdinalIgnoreCase)
                            ? Decompress(buffer, read)
                            : Encoding.UTF8.GetString(buffer, 0, read);
                    }

                    lock (_gate)
                    {
                        _requests.Add(new CapturedRequest(requestLine[0], requestLine.Length > 1 ? requestLine[1] : "", headers, body));
                    }

                    // Elastic.Transport validates that it is talking to Elasticsearch and refuses to
                    // continue without this header, so a plain 200 is not enough.
                    var payload = Encoding.UTF8.GetBytes(_responseBody ?? """{"took":1,"errors":false,"items":[{"index":{"_index":"i","status":201}}]}""");
                    var response = Encoding.UTF8.GetBytes(
                        $"HTTP/1.1 {_statusCode} Response\r\n" +
                        "Content-Type: application/json\r\n" +
                        "X-Elastic-Product: Elasticsearch\r\n" +
                        $"Content-Length: {payload.Length}\r\n" +
                        "Connection: close\r\n\r\n");

                    await stream.WriteAsync(response);
                    await stream.WriteAsync(payload);
                    await stream.FlushAsync();
                }
            }
            catch
            {
                // A torn-down connection during disposal is not a test failure.
            }
        }

        private static string Decompress(byte[] buffer, int length)
        {
            using var source = new MemoryStream(buffer, 0, length);
            using var gzip = new System.IO.Compression.GZipStream(source, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }

        public sealed record CapturedRequest(string Method, string Target, IReadOnlyDictionary<string, string> Headers, string Body);
    }
}
