using elando.ELK.TraceLogging.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class HttpContextExtensions
    {
        public static void AddTraceHeader(this HttpContext httpContext, string traceName)
        {
            var traceId = Guid.NewGuid().ToString();
            traceId = AddUserIdIfAuthenticated(httpContext, traceId);

            httpContext.Request.Headers.Add(traceName, traceId);
            httpContext.Response.Headers.Add(traceName, traceId);
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

        public static string GetTraceId(this HttpContext httpContext, IConfiguration configuration)
        {
            var success = false;
            StringValues requestIdAsString = "";
            if (httpContext is not null && configuration is not null)
            {
                success = httpContext.Request.Headers
                    .TryGetValue(configuration.GetHeaderName(), out requestIdAsString);
            }

            string requestId = string.Empty;
            if (success)
            {
                requestId = requestIdAsString.ToString();
            }

            return requestId;
        }

        #region Privates
        /// <summary>
        /// Adds UserId based on Authorization token.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="traceId"></param>
        /// <returns></returns>
        private static string AddUserIdIfAuthenticated(HttpContext httpContext, string traceId)
        {
            var authorizationHeaderValue = httpContext.Request.Headers["Authorization"].ToString();
            var jwtAsString = authorizationHeaderValue.Split(" ").GetValue(1)?.ToString();
            if (string.IsNullOrWhiteSpace(jwtAsString))
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(jwtAsString);
                var userId = jwt.Claims.First(c => c.Type.Equals("userId", StringComparison.OrdinalIgnoreCase))?.Value;

                traceId = traceId += $"{ELKConstants.SPLITTER}{userId ?? "UserId claim missing in token."}";
            }

            return traceId;
        }
        #endregion
    }
}
