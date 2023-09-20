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
        
        public static string GetPrefix(this IConfiguration configuration)
        {
            string? value = configuration.GetSection("LoggerPrefix").Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return "Default";
        }
        public static string GetElasticUriUri(this IConfiguration configuration)
        {
            string? value = configuration.GetSection("ElasticUri").Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            throw new NullReferenceException("ElasticSearch Uri does not provide to appsettings");
        }
    }
}
