using Newtonsoft.Json;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class ConvertExtensions
    {
        /// <summary>
        /// Serializer settings for the logging path. A property whose getter throws must not take the
        /// whole log line with it: the error is swallowed, that property is skipped, and the rest of
        /// the object is still written.
        /// <para>
        /// The case that forced this: <c>Grpc.Core.Metadata.Entry.Value</c> throws
        /// "Cannot access string value of a binary metadata entry" by design, and gRPC always sends
        /// <c>grpc-status-details-bin</c> in trailers. Serializing any <c>RpcException</c> therefore
        /// threw, and the resulting JsonSerializationException replaced the real error in the log -
        /// so every gRPC failure was reported as a serialization bug instead of its actual cause.
        /// </para>
        /// </summary>
        private static readonly JsonSerializerSettings LogSerializerSettings = new()
        {
            Formatting = Formatting.Indented,
            Error = (_, args) => args.ErrorContext.Handled = true,
        };

        public static string ToJSON<T>(this T @object)
            where T : class
            => JsonConvert.SerializeObject(@object, LogSerializerSettings);

        public static string ToXML<T>(this T @object)
            where T : class
            => throw new NotImplementedException();

        public static T DeepCopy<T>(this T @object)
            where T : class
        {
            if (@object == null) return null!;

            var objectJson = JsonConvert.SerializeObject(@object);
            return JsonConvert.DeserializeObject<T>(objectJson)!;
        }
    }
}
