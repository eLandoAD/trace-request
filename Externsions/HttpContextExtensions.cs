using Microsoft.AspNetCore.Http;

namespace elando.ELK.TraceLogging.Externsions
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
    }
}
