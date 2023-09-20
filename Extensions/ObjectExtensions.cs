using elando.ELK.TraceLogging.Constants;
using Google.Protobuf.Collections;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class ObjectExtensions
    {
        #region RedactSensitiveData overloads
        /// <summary>
        /// Redacts the values of all given properties in the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <param name="propertyNames"></param>
        public static void RedactSensitiveData<T>(this RepeatedField<T> objects, params string[] propertyNames)
           where T : class
        {
            foreach (var @object in objects)
            {
                RedactSensitiveData(@object, propertyNames);
            }
        }

        /// <summary>
        /// Redacts the values of all given properties in the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <param name="propertyNames"></param>
        public static void RedactSensitiveData<T>(this IEnumerable<T> objects, params string[] propertyNames)
           where T : class
        {
            foreach (var @object in objects)
            {
                RedactSensitiveData(@object, propertyNames);
            }
        }

        /// <summary>
        /// Redacts the values of all given properties in the object.
        /// </summary>
        /// <code>
        /// foreach (var user in Users)
        /// {
        ///    LogControllerHelper.RedactSensitiveData(user, nameof(user.EGN));
        /// }
        /// </code>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        /// <param name="propertyNames"></param>
        public static void RedactSensitiveData<T>(this T @object, params string[] propertyNames)
            where T : class
        {
            if (@object == null || propertyNames.Count() == 0)
            {
                return;
            }

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
        }
        #endregion

        #region private
        /// <summary>
        /// Returns custom default values depending on it's type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object GetDefaultRedactedValue(Type type)
        {
            if (type == typeof(string))
            {
                return ELKConstants.REDACTED;
            }
            else if (type.IsValueType)
            {
                return Activator.CreateInstance(type)!;
            }

            return null!;
        }
        #endregion
    }
}
