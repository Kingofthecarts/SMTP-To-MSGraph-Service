using SMTPServiceUpdater.Models;
using System.Diagnostics;

namespace SMTPServiceUpdater.Services;

/// <summary>
/// Manages version detection, parsing, and comparison for updates
/// </summary>
public class VersionManager
{
    /// <summary>
    /// Parses a version string into a VersionInfo object
    /// </summary>
    /// <param name="versionString">Version string to parse (e.g., "4.2.0")</param>
    /// <returns>Parsed VersionInfo object</returns>
    /// <exception cref="ArgumentException">Thrown when version string is invalid</exception>
    public static VersionInfo ParseVersion(string versionString)
    {
        return VersionInfo.Parse(versionString);
    }

    /// <summary>
    /// Scans the updates folder and returns the latest version available
    /// </summary>
    /// <param name="updatesFolder">Path to updates folder containing ZIP files</param>
    /// <returns>Latest version string, or null if no valid versions found</returns>
    public static string? GetLatestVersion(string updatesFolder)
    {
        if (string.IsNullOrWhiteSpace(updatesFolder))
        {
            return null;
        }

        if (!Directory.Exists(updatesFolder))
        {
            return null;
        }

        // Get all ZIP files in the updates folder
        var zipFiles = Directory.GetFiles(updatesFolder, "*.zip");

        if (zipFiles.Length == 0)
        {
            return null;
        }

        VersionInfo? latestVersion = null;

        foreach (var zipFile in zipFiles)
        {
            // Extract filename without extension (e.g., "4.0.0.zip" -> "4.0.0")
            var fileName = Path.GetFileNameWithoutExtension(zipFile);

            // Try to parse as version
            if (VersionInfo.TryParse(fileName, out var version) && version != null)
            {
                // Track highest version
                if (latestVersion == null || version > latestVersion)
                {
                    latestVersion = version;
                }
            }
        }

        return latestVersion?.ToString();
    }

    /// <summary>
    /// Gets the currently installed version from version.txt or executable
    /// </summary>
    /// <param name="rootPath">Root path where SMTP Service is installed</param>
    /// <returns>Current version string, or "0.0.0" if not found</returns>
    public static string GetCurrentVersion(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return "0.0.0";
        }

        // Try to read from version.txt first
        var versionFilePath = Path.Combine(rootPath, "version.txt");

        if (File.Exists(versionFilePath))
        {
            try
            {
                var versionText = File.ReadAllText(versionFilePath).Trim();
                
                // Try to parse the version to validate it (this will strip build metadata like +commit)
                if (VersionInfo.TryParse(versionText, out var version) && version != null)
                {
                    return version.ToString();
                }
            }
            catch
            {
                // If reading version.txt fails, fall through to exe check
            }
        }

        // Fallback: Try to get version from SMTP Service.exe FileVersionInfo
        var exePath = Path.Combine(rootPath, "SMTP Service.exe");

        if (File.Exists(exePath))
        {
            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(exePath);
                var fileVersion = fileVersionInfo.FileVersion;

                if (!string.IsNullOrWhiteSpace(fileVersion))
                {
                    // FileVersion might include build metadata (e.g., "4.2.1+68c0b3531c143")
                    // or be in format "4.0.0.0" - the Parse method will handle both
                    // Try parsing the full version first (handles build metadata)
                    if (VersionInfo.TryParse(fileVersion, out var version) && version != null)
                    {
                        return version.ToString();
                    }
                    
                    // Fallback: Try taking first 3 parts if it has 4 parts (e.g., "4.0.0.0")
                    var parts = fileVersion.Split('.');
                    if (parts.Length >= 3)
                    {
                        var versionString = $"{parts[0]}.{parts[1]}.{parts[2]}";
                        
                        if (VersionInfo.TryParse(versionString, out version) && version != null)
                        {
                            return version.ToString();
                        }
                    }
                }
            }
            catch
            {
                // If getting file version fails, return default
            }
        }

        // Default version if nothing found
        return "0.0.0";
    }

    /// <summary>
    /// Determines if configuration migration is needed based on version numbers
    /// </summary>
    /// <param name="currentVersion">Current installed version</param>
    /// <param name="targetVersion">Target version to install</param>
    /// <returns>True if migration is needed, false otherwise</returns>
    public static bool IsMigrationNeeded(string currentVersion, string targetVersion)
    {
        // Try to parse both versions
        if (!VersionInfo.TryParse(currentVersion, out var current) || current == null)
        {
            return false;
        }

        if (!VersionInfo.TryParse(targetVersion, out var target) || target == null)
        {
            return false;
        }

        // Migration needed if: Current < 4.0.1 AND Target >= 4.0.0
        var version401 = VersionInfo.Parse("4.0.1");
        var version400 = VersionInfo.Parse("4.0.0");

        bool currentBelowMigrationThreshold = current < version401;
        bool targetRequiresMigration = target >= version400;

        return currentBelowMigrationThreshold && targetRequiresMigration;
    }
}
