using elando.ELK.TraceLogging.Extensions;
using Microsoft.Extensions.Logging;

using static elando.ELK.TraceLogging.Extensions.ObjectExtensions;

namespace elando.ELK.TraceLogging.Services.Logger
{
    public record LogModelWithRequestId<T>(T Model, Guid requestId);

    public static class LoggerTraceExtensions
    {
        #region LogWithTraceId overloads

        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="traceId"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        // - message - logLevel
        public static LogModelWithRequestId<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            Guid traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => LogWithTraceId(logger, model, traceId, LogLevel.Information, null!, sensitivePropertyNames);


        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="traceId"></param>
        /// <param name="logLevel"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        // - message
        public static LogModelWithRequestId<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            Guid traceId,
            LogLevel logLevel,
            params string[] sensitivePropertyNames)
            where T : class
                => LogWithTraceId(logger, model, traceId, logLevel, null!, sensitivePropertyNames);


        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="traceId"></param>
        /// <param name="message"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        // - logLevel
        public static LogModelWithRequestId<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            Guid traceId,
            string message,
            params string[] sensitivePropertyNames)
            where T : class
                => LogWithTraceId(logger, model, traceId, LogLevel.Information, message, sensitivePropertyNames);

        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="logLevel"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public static LogModelWithRequestId<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            Guid traceId,
            LogLevel logLevel,
            string message = "",
            params string[] sensitivePropertyNames)
            where T : class
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                message += " ";
            }

            var resultWithTraceId = new LogModelWithRequestId<T>(model, traceId);
            if (logger is null)
            {
                return resultWithTraceId;
            }

            T requestToLog = model;

            bool hasSensitiveData = sensitivePropertyNames != null && sensitivePropertyNames.Length > 0;
            if (hasSensitiveData)
            {
                requestToLog = model.DeepCopy();
                requestToLog.RedactSensitiveData(sensitivePropertyNames!);
            }

            var logModelWithTraceId = hasSensitiveData
                    ? new LogModelWithRequestId<T>(requestToLog, traceId)
                    : resultWithTraceId;

            var logModel = string.Format("{0}{1}", message, logModelWithTraceId.ToJSON());
            logger.Log(logLevel, logModel);

            return resultWithTraceId;
        }
        #endregion
    }
}
