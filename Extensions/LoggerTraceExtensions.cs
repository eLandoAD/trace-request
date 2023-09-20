using Microsoft.Extensions.Logging;

namespace elando.ELK.TraceLogging.Extensions
{
    public record LogModelWithRequestId<T>(T Model, Guid requestId);
    public record LogModelWithMessageAndTraceId<T>(string Message, LogModelWithRequestId<T>? LogModelWithRequestId);

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
        /// <param name="message"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        // - logLevel
        public static LogModelWithRequestId<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            Guid traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => logger.LogWithTraceId(model, traceId, LogLevel.Information, sensitivePropertyNames);

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
            params string[] sensitivePropertyNames)
            where T : class
        {
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

            logger.Log(logLevel, logModelWithTraceId.ToJSON());

            return resultWithTraceId;
        }
        #endregion
    }
}
