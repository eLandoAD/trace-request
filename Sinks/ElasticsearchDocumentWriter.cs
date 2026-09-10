using System.Globalization;
using System.Text.Json;
using Serilog.Events;

// Deliberately not a namespace called "Formatting": from inside
// elando.ELK.TraceLogging.Extensions that would bind closer than Newtonsoft.Json.Formatting and
// silently break ConvertExtensions' `Formatting.Indented`.
namespace elando.ELK.TraceLogging.Sinks
{
    /// <summary>
    /// Writes a Serilog <see cref="LogEvent"/> in the exact document shape that the previous sink
    /// produced, so the swap off Serilog.Sinks.Elasticsearch changes no field name in Kibana.
    ///
    /// The shape reproduced is Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter with the
    /// options ElasticsearchSink actually defaulted to in 10.0.0 - renderMessage: true,
    /// inlineFields: false, formatStackTraceAsArray: false:
    ///
    ///   {"@timestamp":..,"level":..,"messageTemplate":..,"message":..,"exceptions":[..],"fields":{..}}
    ///
    /// "exceptions" is emitted only when the event carries one, and "fields" only when the event has
    /// properties - matching the old formatter, which skipped both keys rather than writing empties.
    /// </summary>
    public static class ElasticsearchDocumentWriter
    {
        /// <summary>
        /// The old formatter stopped walking the InnerException chain at 20 levels to bound the
        /// document size on deeply wrapped exceptions. Kept identical so nothing that fits today
        /// starts being truncated.
        /// </summary>
        private const int MaxExceptionDepth = 20;

        public static void Write(Utf8JsonWriter writer, LogEvent logEvent, IFormatProvider? formatProvider = null)
        {
            writer.WriteStartObject();

            writer.WriteString("@timestamp", logEvent.Timestamp.ToString("o", CultureInfo.InvariantCulture));
            writer.WriteString("level", logEvent.Level.ToString());
            writer.WriteString("messageTemplate", logEvent.MessageTemplate.Text);
            writer.WriteString("message", logEvent.RenderMessage(formatProvider));

            if (logEvent.Exception is not null)
            {
                WriteExceptions(writer, logEvent.Exception);
            }

            if (logEvent.Properties.Count != 0)
            {
                writer.WritePropertyName("fields");
                writer.WriteStartObject();
                foreach (var property in logEvent.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    WriteValue(writer, property.Value, formatProvider);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private static void WriteExceptions(Utf8JsonWriter writer, Exception exception)
        {
            writer.WritePropertyName("exceptions");
            writer.WriteStartArray();

            var current = exception;
            var depth = 0;
            while (current is not null && depth < MaxExceptionDepth)
            {
                writer.WriteStartObject();
                writer.WriteNumber("Depth", depth);
                writer.WriteString("ClassName", current.GetType().FullName);
                writer.WriteString("Message", current.Message);
                writer.WriteString("Source", current.Source);
                writer.WriteString("StackTraceString", current.StackTrace);
                // The old formatter surfaced the private serialization fields of System.Exception.
                // They are always null/0 for an exception that was thrown in-process rather than
                // deserialized across a remoting boundary, so they are written as constants instead
                // of reflected over - the values are identical and the reflection is not.
                writer.WriteNull("RemoteStackTraceString");
                writer.WriteNumber("RemoteStackIndex", 0);
                writer.WriteNumber("HResult", current.HResult);
                writer.WriteNull("HelpURL");
                writer.WriteEndObject();

                current = current.InnerException;
                depth++;
            }

            writer.WriteEndArray();
        }

        private static void WriteValue(Utf8JsonWriter writer, LogEventPropertyValue value, IFormatProvider? formatProvider)
        {
            switch (value)
            {
                case ScalarValue scalar:
                    WriteScalar(writer, scalar.Value, formatProvider);
                    break;

                case SequenceValue sequence:
                    writer.WriteStartArray();
                    foreach (var element in sequence.Elements)
                    {
                        WriteValue(writer, element, formatProvider);
                    }
                    writer.WriteEndArray();
                    break;

                case StructureValue structure:
                    writer.WriteStartObject();
                    // _typeTag leads, as it did in the old formatter.
                    if (structure.TypeTag is not null)
                    {
                        writer.WriteString("_typeTag", structure.TypeTag);
                    }
                    foreach (var property in structure.Properties)
                    {
                        writer.WritePropertyName(property.Name);
                        WriteValue(writer, property.Value, formatProvider);
                    }
                    writer.WriteEndObject();
                    break;

                case DictionaryValue dictionary:
                    writer.WriteStartObject();
                    foreach (var entry in dictionary.Elements)
                    {
                        writer.WritePropertyName(entry.Key.Value?.ToString() ?? "null");
                        WriteValue(writer, entry.Value, formatProvider);
                    }
                    writer.WriteEndObject();
                    break;

                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }

        private static void WriteScalar(Utf8JsonWriter writer, object? value, IFormatProvider? formatProvider)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case string s:
                    writer.WriteStringValue(s);
                    return;
                case bool b:
                    writer.WriteBooleanValue(b);
                    return;
                case char c:
                    writer.WriteStringValue(c.ToString());
                    return;
                case byte or sbyte or short or ushort or int or uint:
                    writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return;
                case long l:
                    writer.WriteNumberValue(l);
                    return;
                case ulong ul:
                    writer.WriteNumberValue(ul);
                    return;
                case decimal m:
                    writer.WriteNumberValue(m);
                    return;
                case double d:
                    // JSON has no NaN/Infinity literal; the old formatter degraded these to strings
                    // rather than emitting invalid JSON that Elasticsearch would reject.
                    if (double.IsNaN(d) || double.IsInfinity(d)) writer.WriteStringValue(d.ToString(CultureInfo.InvariantCulture));
                    else writer.WriteNumberValue(d);
                    return;
                case float f:
                    if (float.IsNaN(f) || float.IsInfinity(f)) writer.WriteStringValue(f.ToString(CultureInfo.InvariantCulture));
                    else writer.WriteNumberValue(f);
                    return;
                case DateTime dt:
                    writer.WriteStringValue(dt.ToString("o", CultureInfo.InvariantCulture));
                    return;
                case DateTimeOffset dto:
                    writer.WriteStringValue(dto.ToString("o", CultureInfo.InvariantCulture));
                    return;
                default:
                    if (value is IFormattable formattable)
                    {
                        writer.WriteStringValue(formattable.ToString(null, formatProvider ?? CultureInfo.InvariantCulture));
                        return;
                    }
                    writer.WriteStringValue(value.ToString());
                    return;
            }
        }
    }
}
