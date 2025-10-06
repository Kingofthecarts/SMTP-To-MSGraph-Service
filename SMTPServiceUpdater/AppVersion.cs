namespace SMTPServiceUpdater;

/// <summary>
/// Centralized application version information.
/// Update this single location when bumping versions.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Current application version (semantic versioning: Major.Minor.Patch)
    /// </summary>
    public const string Version = "4.2.7";

    /// <summary>
    /// Application name with version for display purposes
    /// </summary>
    public static string FullName => $"SMTP Service Updater v{Version}";

    /// <summary>
    /// Application header for console/log output
    /// </summary>
    public static string Header => $"SMTP SERVICE UPDATER v{Version}";
}
