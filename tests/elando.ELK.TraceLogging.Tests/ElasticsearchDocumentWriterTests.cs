using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using elando.ELK.TraceLogging.Sinks;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Parsing;
using Xunit;

namespace elando.ELK.TraceLogging.Tests
{
    /// <summary>
    /// The 10.0.2 sink swap is only safe if the document reaching Elasticsearch is unchanged -
    /// otherwise every saved search, visualisation and Discover column in Kibana breaks even though
    /// the index names stayed the same.
    ///
    /// Each test formats the same LogEvent twice: once through the formatter
    /// Serilog.Sinks.Elasticsearch actually defaulted to, and once through the replacement, then
    /// compares the JSON. The oracle is constructed with the exact arguments ElasticsearchSinkState
    /// used for a default ElasticsearchSinkOptions - renderMessage: true, inlineFields: false,
    /// formatStackTraceAsArray: false - which was confirmed by reflecting over the sink's own state.
    /// </summary>
    public class ElasticsearchDocumentWriterTests
    {
        private static readonly MessageTemplateParser Parser = new();

        private static string Legacy(LogEvent logEvent)
        {
            var formatter = new ElasticsearchJsonFormatter(
                omitEnclosingObject: false,
                closingDelimiter: string.Empty,
                renderMessage: true,
                formatProvider: null,
                inlineFields: false,
                formatStackTraceAsArray: false);

            var writer = new StringWriter();
            formatter.Format(logEvent, writer);
            return writer.ToString();
        }

        private static string Replacement(LogEvent logEvent)
        {
            var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                SkipValidation = true,
            }))
            {
                ElasticsearchDocumentWriter.Write(writer, logEvent);
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private static void AssertSameDocument(LogEvent logEvent)
        {
            // Compared as parsed JSON rather than raw strings: the two writers are allowed to differ
            // in insignificant whitespace and unicode escaping (А vs the literal character),
            // which Elasticsearch resolves to the same stored document. Key order and value types
            // still have to match, and JsonNode comparison preserves both.
            var expected = JsonDocument.Parse(Legacy(logEvent)).RootElement.GetRawText();
            var actual = JsonDocument.Parse(Replacement(logEvent)).RootElement.GetRawText();

            Assert.Equal(expected, actual);
        }

        private static LogEvent Event(
            LogEventLevel level = LogEventLevel.Information,
            string template = "A message with no properties",
            Exception? exception = null,
            params LogEventProperty[] properties)
            => new(
                new DateTimeOffset(2026, 9, 10, 8, 30, 0, TimeSpan.FromHours(3)),
                level,
                exception,
                Parser.Parse(template),
                properties);

        [Fact]
        public void Bare_message_matches_the_legacy_formatter()
            => AssertSameDocument(Event());

        [Theory]
        [InlineData(LogEventLevel.Verbose)]
        [InlineData(LogEventLevel.Debug)]
        [InlineData(LogEventLevel.Information)]
        [InlineData(LogEventLevel.Warning)]
        [InlineData(LogEventLevel.Error)]
        [InlineData(LogEventLevel.Fatal)]
        public void Every_level_matches_the_legacy_formatter(LogEventLevel level)
            => AssertSameDocument(Event(level));

        [Fact]
        public void Rendered_message_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Policy {PolicyNumber} issued for {Amount} BGN",
                properties: new[]
                {
                    new LogEventProperty("PolicyNumber", new ScalarValue("BG/03/126000123456")),
                    new LogEventProperty("Amount", new ScalarValue(249.90m)),
                }));

        public static TheoryData<string, object?> Scalars => new()
        {
            { "null", null },
            { "string", "hello" },
            { "emptyString", "" },
            { "bool", true },
            { "char", 'x' },
            { "byte", (byte)7 },
            { "sbyte", (sbyte)-7 },
            { "short", (short)-300 },
            { "ushort", (ushort)300 },
            { "int", 42 },
            { "uint", 42u },
            { "long", 9_000_000_000L },
            { "ulong", 9_000_000_000UL },
            { "decimal", 12.34m },
            { "double", 12.5d },
            { "float", 12.5f },
            { "dateTime", new DateTime(2026, 9, 10, 8, 30, 0, DateTimeKind.Utc) },
            { "dateTimeOffset", new DateTimeOffset(2026, 9, 10, 8, 30, 0, TimeSpan.FromHours(3)) },
            { "timeSpan", TimeSpan.FromMinutes(90) },
            { "guid", Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff") },
            { "enum", LogEventLevel.Warning },
            { "cyrillic", "Полица номер 123 е издадена" },
            { "quotesAndSlashes", "he said \"hi\" \\ then left\r\n" },
        };

        [Theory]
        [MemberData(nameof(Scalars))]
        public void Scalar_property_matches_the_legacy_formatter(string name, object? value)
            => AssertSameDocument(Event(
                template: "Value is {" + name + "}",
                properties: new LogEventProperty(name, new ScalarValue(value))));

        [Fact]
        public void Structure_property_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Customer {@Customer}",
                properties: new LogEventProperty("Customer", new StructureValue(
                    new[]
                    {
                        new LogEventProperty("Name", new ScalarValue("Ана")),
                        new LogEventProperty("Egn", new ScalarValue("[REDACTED]")),
                        new LogEventProperty("Age", new ScalarValue(31)),
                    }))));

        [Fact]
        public void Structure_property_with_a_type_tag_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Customer {@Customer}",
                properties: new LogEventProperty("Customer", new StructureValue(
                    new[] { new LogEventProperty("Name", new ScalarValue("Ана")) },
                    typeTag: "Customer"))));

        [Fact]
        public void Nested_structure_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Order {@Order}",
                properties: new LogEventProperty("Order", new StructureValue(
                    new[]
                    {
                        new LogEventProperty("Id", new ScalarValue(7)),
                        new LogEventProperty("Customer", new StructureValue(
                            new[] { new LogEventProperty("Name", new ScalarValue("Ана")) })),
                    }))));

        [Fact]
        public void Sequence_property_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Codes {Codes}",
                properties: new LogEventProperty("Codes", new SequenceValue(
                    new LogEventPropertyValue[]
                    {
                        new ScalarValue(1),
                        new ScalarValue("two"),
                        new ScalarValue(null),
                    }))));

        [Fact]
        public void Dictionary_property_matches_the_legacy_formatter()
            => AssertSameDocument(Event(
                template: "Headers {Headers}",
                properties: new LogEventProperty("Headers", new DictionaryValue(
                    new[]
                    {
                        KeyValuePair.Create(new ScalarValue("x-trace-id"), (LogEventPropertyValue)new ScalarValue("abc")),
                        KeyValuePair.Create(new ScalarValue("retries"), (LogEventPropertyValue)new ScalarValue(2)),
                    }))));

        [Fact]
        public void Single_exception_matches_the_legacy_formatter()
            => AssertSameDocument(Event(LogEventLevel.Error, "It failed", Thrown(() => throw new InvalidOperationException("boom"))));

        [Fact]
        public void Nested_exception_chain_matches_the_legacy_formatter()
            => AssertSameDocument(Event(LogEventLevel.Error, "It failed", Thrown(() =>
            {
                try
                {
                    throw new InvalidOperationException("inner boom");
                }
                catch (Exception inner)
                {
                    throw new ApplicationException("outer boom", inner);
                }
            })));

        [Fact]
        public void Exception_and_properties_together_match_the_legacy_formatter()
            => AssertSameDocument(Event(
                LogEventLevel.Error,
                "Policy {PolicyNumber} failed",
                Thrown(() => throw new InvalidOperationException("boom")),
                new LogEventProperty("PolicyNumber", new ScalarValue("BG/03/126000123456"))));

        [Fact]
        public void Exception_with_no_stack_trace_matches_the_legacy_formatter()
            => AssertSameDocument(Event(LogEventLevel.Error, "It failed", new InvalidOperationException("never thrown")));

        private static Exception Thrown(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }

            throw new InvalidOperationException("the action was expected to throw");
        }
    }
}
