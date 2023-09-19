using Newtonsoft.Json;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class ConvertExtensions
    {
        public static string ToJSON<T>(this T @object)
            where T : class
            => JsonConvert.SerializeObject(@object, Formatting.Indented);

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
