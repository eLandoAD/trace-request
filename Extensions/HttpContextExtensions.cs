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
            traceId = RemoveDashes(traceId);

            // Indexer rather than Add (ASP0019): IHeaderDictionary.Add throws ArgumentException if
            // the key is already present, so a request that already carried the trace header - or a
            // second call on the same context - would have thrown out of the middleware.
            httpContext.Request.Headers[traceName] = traceId;
            httpContext.Response.Headers[traceName] = traceId;
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
        private static string RemoveDashes(string traceId)
        {
            return traceId.Replace("-", "");
        }

        /// <summary>
        /// Adds UserId based on Authorization token.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="traceId"></param>
        /// <returns></returns>
        private static string AddUserIdIfAuthenticated(HttpContext httpContext, string traceId)
        {
            var authorizationHeaderValue = httpContext.Request.Headers["Authorization"].ToString();
            var jwtAsString = authorizationHeaderValue
               .Split(" ")
               .Skip(1)
               .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(jwtAsString))
            {
                traceId = $"{traceId}{ELKConstants.SPLITTER}{ReadUserId(jwtAsString)}";
            }

            return traceId;
        }

        /// <summary>
        /// Best-effort user id from a bearer token, for tagging the trace id.
        /// <para>
        /// Deliberately cannot throw. Callers invoke this while composing a log message, very often
        /// from inside a catch block, so any exception raised here would replace the error being
        /// reported with an unrelated one. Every failure degrades to a marker string instead.
        /// </para>
        /// </summary>
        private static string ReadUserId(string jwtAsString)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(jwtAsString);

                // FirstOrDefault, not First: a token with no "userId" claim is a normal thing to
                // receive, and First throws on no match.
                return jwt.Claims
                    .FirstOrDefault(c => c.Type.Equals("userId", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? "UserId claim missing in token.";
            }
            catch (Exception)
            {
                // ReadJwtToken rejects anything that is not a well-formed JWT - a non-JWT bearer
                // scheme, a truncated header, an opaque API token. Caught broadly on purpose: the
                // contract of this method is that it never throws.
                return "Unreadable token in Authorization header.";
            }
        }
        #endregion
    }
}
