using elando.ELK.TraceLogging.Constants;
using Microsoft.Extensions.Logging;

namespace elando.ELK.TraceLogging.Extensions
{
    public record LogModelWithTraceData<T>(T Model, string RequestId, string? UserId = null) where T : class;
    public record LogMessage(string Message);
    public record LogModelWithMessageAndTraceData<T>(LogMessage Message, LogModelWithTraceData<T> Model) where T : class;

    /// <summary>
    /// Extends Microsoft.Extensions.Logger.ILogger
    /// </summary>
    public static class LoggerTraceExtensions
    {
        #region LogWithTraceId overloads
        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="logMessage"></param>
        /// <param name="traceId"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public static LogModelWithTraceData<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            LogMessage logMessage,
            string traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => logger.LogWithTraceId(model, logMessage, traceId, LogLevel.Information, sensitivePropertyNames);

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
        public static LogModelWithTraceData<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            string traceId,
            LogLevel logLevel,
            params string[] sensitivePropertyNames)
            where T : class
                => logger.LogWithTraceId(model, null!, traceId, logLevel, sensitivePropertyNames);

        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="traceId"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public static LogModelWithTraceData<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            string traceId,
            params string[] sensitivePropertyNames)
            where T : class
                => logger.LogWithTraceId(model, null!, traceId, LogLevel.Information, sensitivePropertyNames);

        /// <summary>
        /// Logs only objects with depth-1 and if logger is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="logger"></param>
        /// <param name="model"></param>
        /// <param name="logMessage"></param>
        /// <param name="traceId"></param>
        /// <param name="logLevel"></param>
        /// <param name="sensitivePropertyNames"></param>
        /// <returns>New object with model and traceId.</returns>
        public static LogModelWithTraceData<T> LogWithTraceId<T>(
            this ILogger logger,
            T model,
            LogMessage logMessage,
            string traceId,
            LogLevel logLevel,
            params string[] sensitivePropertyNames)
            where T : class
        {
            LogModelWithTraceData<T> resultWithTraceData = PopulateLogModelWithTraceData(model, traceId);

            if (logger is null)
            {
                return resultWithTraceData;
            }

            T requestToLog = model;

            bool hasSensitiveData = sensitivePropertyNames?.Length > 0;
            if (hasSensitiveData)
            {
                requestToLog = model.DeepCopy();
                requestToLog.RedactSensitiveData(sensitivePropertyNames!);
            }

            var logModelWithTraceId = hasSensitiveData
                    ? new LogModelWithTraceData<T>(requestToLog, resultWithTraceData.RequestId, resultWithTraceData.UserId)
                    : resultWithTraceData;

            if (!string.IsNullOrWhiteSpace(logMessage?.Message))
            {
                var logAll = new LogModelWithMessageAndTraceData<T>(logMessage!, logModelWithTraceId);
                logger.Log(logLevel, logAll.ToJSON());
            }
            else
            {
                logger.Log(logLevel, logModelWithTraceId.ToJSON());
            }

            return resultWithTraceData;
        }
        #endregion

        #region Privates
        /// <summary>
        /// Populates the object. Population differs based on the user's Authorization token.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="traceId"></param>
        /// <returns></returns>
        private static LogModelWithTraceData<T> PopulateLogModelWithTraceData<T>(T model, string traceId)
             where T : class
        {
            if (traceId is not null && traceId.Contains(ELKConstants.SPLITTER))
            {
                var traceArgs = traceId.Split(ELKConstants.SPLITTER).ToList();
                var requestId = traceArgs.FirstOrDefault();
                var userId = traceArgs.Skip(1).FirstOrDefault();

                return new LogModelWithTraceData<T>(model, requestId ?? "", userId);
            }
            else
            {
                return new LogModelWithTraceData<T>(model, traceId!);
            }
        }
        #endregion
    }
}
