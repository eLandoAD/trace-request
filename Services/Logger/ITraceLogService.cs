using elando.ELK.TraceLogging.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace elando.ELK.TraceLogging.Services.Logger;

public interface ITraceLogService
{
    Guid GetTraceId(HttpContext httpContext);
    LogModelWithRequestId<T> LogModelWithDepthOne<T>(T model, Guid traceId, LogLevel logLevel = LogLevel.Information, params string[] sensitivePropertyNames) where T : class;
    LogModelWithRequestId<T> LogModelWithDepthOne<T>(T model, Guid traceId, params string[] sensitivePropertyNames) where T : class;
    string LogResponseWithDepthOne<T>(HBOResponseWrapper<T> response, Guid traceId, LogLevel logLevel = LogLevel.Information, params string[] sensitivePropertyNames) where T : class;
    string LogResponseWithDepthOne<T>(HBOResponseWrapper<T> response, Guid traceId, params string[] sensitivePropertyNames) where T : class;
    string LogResponseWithDepthOne<T>(SSPResponseWrapper<T> response, Guid traceId, LogLevel logLevel = LogLevel.Information, params string[] sensitivePropertyNames) where T : class;
    string LogResponseWithDepthOne<T>(SSPResponseWrapper<T> response, Guid traceId, params string[] sensitivePropertyNames) where T : class;
}