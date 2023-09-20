using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class HttpContextExtensions
    {
        public static void AddTraceHeader(this HttpContext httpContext, string traceName)
        {
            httpContext.Request.Headers.Add(traceName, Guid.NewGuid().ToString());
        }

        public static string? GetTraceHeader(this HttpContext httpContext, string traceName)
        {
            bool isExistTraceId = httpContext.Request.Headers.TryGetValue(traceName, out var traceId);

            if (isExistTraceId)
            {
                return traceId;
            }

            return null;
        }

        public static Guid GetTraceId(this HttpContext httpContext, IConfiguration configuration)
        {
            var success = false;
            StringValues requestIdAsString = "";
            if (httpContext is not null && configuration is not null)
            {
                success = httpContext.Request.Headers
                    .TryGetValue(configuration.GetHeaderName(), out requestIdAsString);
            }

            Guid requestId = Guid.Empty;
            if (success)
            {
                requestId = new Guid(requestIdAsString!);
            }

            return requestId;
        }
    }
}
