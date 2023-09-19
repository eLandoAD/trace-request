using elando.ELK.TraceLogging.Constants;
using elando.ELK.TraceLogging.Extensions;
using elando.ELK.TraceLogging.Wrappers;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace elando.ELK.TraceLogging.Services.Logger
{
    public record LogModelWithRequestId<T>(T Model, Guid requestId);

    public class TraceLogService : ITraceLogService
    {
        private readonly ILogger<TraceLogService> _logger;
        private readonly IConfiguration _configuration;

        public TraceLogService(
            IConfiguration configuration,
            ILogger<TraceLogService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets requestId from http request header.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="configuration"></param>
        /// <returns>requestId/TraceId</returns>
        public Guid GetTraceId(HttpContext httpContext)
        {
            var success = false;
            StringValues requestIdAsString = "";
            if (httpContext is not null && _configuration is not null)
            {
                success = httpContext.Request.Headers
                    .TryGetValue(_configuration.GetHeaderName(), out requestIdAsString);
            }

            Guid requestId = Guid.Empty;
            if (success)
            {
                requestId = new Guid(requestIdAsString!);
            }

            return requestId;
        }

        #region LogModelWithDepthOne overloads
        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <remarks>The default log level is Information.</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public LogModelWithRequestId<T> LogModelWithDepthOne<T>(
            T model,
            Guid traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => LogModelWithDepthOne(model, traceId, LogLevel.Information, sensitivePropertyNames);

        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="logLevel"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public LogModelWithRequestId<T> LogModelWithDepthOne<T>(
            T model,
            Guid traceId,
            LogLevel logLevel = LogLevel.Information,
            params string[] sensitivePropertyNames)
            where T : class
        {
            var resultWithTraceId = new LogModelWithRequestId<T>(model, traceId);
            if (_logger is null)
            {
                return resultWithTraceId;
            }

            T requestToLog = model;

            bool hasSensitiveData = sensitivePropertyNames != null && sensitivePropertyNames.Length > 0;
            if (hasSensitiveData)
            {
                requestToLog = model.DeepCopy();
                RedactSensitiveData(requestToLog, sensitivePropertyNames!);
            }

            var logModelWithTraceId = hasSensitiveData
                    ? new LogModelWithRequestId<T>(requestToLog, traceId)
                    : resultWithTraceId;

            _logger.Log(logLevel, logModelWithTraceId.ToJSON());

            return resultWithTraceId;
        }

        #endregion

        #region LogResponseWithDepthOne overloads
        /// <summary>
        /// Logs given wrapper with sensitive data depth-1 or without sensitive data depth-10. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>The response with traceId.</returns>
        public string LogResponseWithDepthOne<T>(
            SSPResponseWrapper<T> response,
            Guid traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => LogResponseWithDepthOne(response, traceId, LogLevel.Information, sensitivePropertyNames);

        /// <summary>
        /// Logs given wrapper with sensitive data depth-1 or without sensitive data depth-10. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="logLevel"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>The response with traceId.</returns>
        public string LogResponseWithDepthOne<T>(
            SSPResponseWrapper<T> response,
            Guid traceId,
            LogLevel logLevel = LogLevel.Information,
            params string[] sensitivePropertyNames)
            where T : class
        {
            if (response.Response is null)
            {
                return response?.ToJSON();
            }

            response.Response.UpdateRequestId(traceId);

            if (_logger is null)
            {
                return response.ToJSON();
            }

            var responseToLog = response;

            bool hasSensitiveData = sensitivePropertyNames.Length > 0;
            bool hasData = responseToLog.Response.Values?.FirstOrDefault() != null;
            if (hasSensitiveData && hasData)
            {
                responseToLog = response.DeepCopy();
                RedactSensitiveData(responseToLog.Response.Values![0], sensitivePropertyNames);
            }

            // We use responseJson variable to not call ToJSON twice when we dont have sensitive data.
            var responseJson = response.ToJSON();
            if (hasSensitiveData && hasData)
            {
                _logger.Log(logLevel, responseToLog.ToJSON());
            }
            else
            {
                _logger.Log(logLevel, responseJson);
            }

            return responseJson;
        }

        /// <summary>
        /// Logs given wrapper with sensitive data depth-1 or without sensitive data depth-10. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>The response with traceId.</returns>
        public string LogResponseWithDepthOne<T>(
          HBOResponseWrapper<T> response,
          Guid traceId,
          params string[] sensitivePropertyNames)
          where T : class
            => LogResponseWithDepthOne(response, traceId, LogLevel.Information, sensitivePropertyNames);

        /// <summary>
        /// Logs given wrapper with sensitive data depth-1 or without sensitive data depth-10. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>The response with traceId.</returns>
        public string LogResponseWithDepthOne<T>(
            HBOResponseWrapper<T> response,
            Guid traceId,
            LogLevel logLevel = LogLevel.Information,
            params string[] sensitivePropertyNames)
            where T : class
        {
            if (response.Response is null)
            {
                return response?.ToJSON();
            }

            response.Response.UpdateRequestId(traceId);

            if (_logger is null)
            {
                return response.ToJSON();
            }

            var responseToLog = response;

            bool hasSensitiveData = sensitivePropertyNames.Length > 0;
            bool hasData = responseToLog.Response.Values?.FirstOrDefault() != null;
            if (hasSensitiveData && hasData)
            {
                responseToLog = response.DeepCopy();
                RedactSensitiveData(responseToLog.Response.Values[0], sensitivePropertyNames);
            }

            // We use responseJson variable to not call ToJSON twice when we dont have sensitive data.
            var responseJson = response.ToJSON();
            if (hasSensitiveData && hasData)
            {
                _logger.Log(logLevel, responseToLog.ToJSON());
            }
            else
            {
                _logger.Log(logLevel, responseJson);
            }

            return responseJson;
        }
        #endregion

        #region RedactSensitiveData overloads
        /// <summary>
        /// Redacts the values of all given properties in the objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <param name="propertyNames"></param>
        public static void RedactSensitiveData<T>(RepeatedField<T> objects, params string[] propertyNames)
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
        public static void RedactSensitiveData<T>(IEnumerable<T> objects, params string[] propertyNames)
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
        public static void RedactSensitiveData<T>(T @object, params string[] propertyNames)
            where T : class
        {
            if (@object is not null && propertyNames.Count() != 0)
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
                return Activator.CreateInstance(type);
            }

            return null;
        }
        #endregion
    }
}
