using System.Reflection;

namespace SMTP_Service.Helpers
{
    /// <summary>
    /// Provides version information from the assembly.
    /// Single source of truth for application version.
    /// </summary>
    public static class VersionHelper
    {
        /// <summary>
        /// Gets the application version in format: Major.Minor.Patch (e.g., "2.0.0")
        /// </summary>
        public static string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return "1.0.0"; // Fallback
        }

        /// <summary>
        /// Gets the full version including revision (e.g., "2.0.0.0")
        /// </summary>
        public static string GetFullVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }

        /// <summary>
        /// Gets the Version object from the assembly
        /// </summary>
        public static Version GetVersionObject()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        }
    }
}
