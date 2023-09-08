using Microsoft.Extensions.Configuration;

namespace elando.ELK.TraceLogging.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetHeaderName(this IConfiguration configuration)
        {
            string? value = configuration.GetSection("TraceIdKey").Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return "X-Default-TraceId";
        }
    }
}
