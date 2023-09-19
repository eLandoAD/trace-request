using System.Reflection;

namespace elando.ELK.TraceLogging.Constants
{
    public class ELKConstants
    {
        public static readonly string COPY_RIGHT = "Copyright ©";
        public static readonly string VERSION_NUMBER = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public static readonly string UNKNOWN = "Unknown";
        public const string REDACTED = "[REDACTED]";
    }
}
