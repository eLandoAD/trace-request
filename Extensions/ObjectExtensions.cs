using Newtonsoft.Json;

using static elando.ELK.TraceLogging.Constants.LibConstants;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Protect sensitive data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static T ProtectSensitiveData<T>(this T @object, params string[] propertyNames)
        {
            var type = typeof(T);
            foreach (var propName in propertyNames)
            {
                var prop = type.GetProperty(propName);
                if (prop is not null && prop.CanWrite)
                {
                    var defaultValue = GetDefaultRedactedValue(prop.PropertyType);
                    prop.SetValue(@object, defaultValue);
                }
            }
            return @object;
        }

        /// <summary>
        /// Returns custom default values depending on it's type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object GetDefaultRedactedValue(Type type)
        {
            if (type == typeof(string))
            {
                return MASK;
            }
            else if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        /// <summary>
        /// Make a cloned object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T @object)
          where T : class
        {
            if (@object == null) return null;

            var objectJson = JsonConvert.SerializeObject(@object);
            return JsonConvert.DeserializeObject<T>(objectJson);
        }

        /// <summary>
        /// Serialize object to JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <returns></returns>
        public static string ToJSON<T>(this T @object)
           where T : class
           => JsonConvert.SerializeObject(@object, Formatting.Indented);
    }
}
