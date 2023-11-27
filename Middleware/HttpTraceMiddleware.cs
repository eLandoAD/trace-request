#region Usings
using elando.ELK.TraceLogging.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
#endregion

namespace elando.ELK.TraceLogging.Middleware
{
    //
    // Summary:
    //     Middleware. Add Trace Header with TraceHeaderName and Guid and UserId if Auth.
    //     Set Default Key: "X-Default-TraceId" or custom - read from appsettings.json "TraceIdKey": "your_customKey"
    // Type parameters:
    //   ContextAccessor:
    //     The type of IHttpContextAccessor.
    //
    //   TraceHeaderName:
    //     The type of string.
    public class HttpTraceMiddleware
    {
        #region Fields
        private readonly RequestDelegate _next;
        private readonly string _headerName;
        #endregion

        #region Ctor
        /// <summary>
        ///  Set as `st middleware in the app to insert the Trace-Header. Set Default Key: "X-Default-TraceId" or custom - read from appsettings.json "TraceIdKey": "your_customKey"
        /// </summary>
        /// <param name="next"></param>
        /// <param name="traceHeaderName"></param>
        public HttpTraceMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _headerName = configuration.GetHeaderName();
        }

        #endregion

        #region Methods
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                context.AddTraceHeader(_headerName);
                await _next.Invoke(context);
                return;

            }
            catch (Exception)
            {
                await _next.Invoke(context);
            }
        }
        #endregion
    }
}
