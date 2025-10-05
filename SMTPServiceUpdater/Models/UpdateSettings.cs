using System.Text.Json.Serialization;

namespace SMTPServiceUpdater.Models;

/// <summary>
/// Represents update configuration settings from smtp-config.json
/// </summary>
public class UpdateSettings
{
    /// <summary>
    /// Whether automatic updates are enabled
    /// </summary>
    [JsonPropertyName("AutoUpdateEnabled")]
    public bool AutoUpdateEnabled { get; set; }

    /// <summary>
    /// How often to check for updates (0=Daily, 1=Weekly)
    /// </summary>
    [JsonPropertyName("CheckFrequency")]
    public int CheckFrequency { get; set; }

    /// <summary>
    /// Time of day to check for updates (HH:mm:ss format)
    /// </summary>
    [JsonPropertyName("CheckTime")]
    public string CheckTime { get; set; } = "02:00:00";

    /// <summary>
    /// Day of week for weekly checks (0=Sunday, 1=Monday, etc.)
    /// </summary>
    [JsonPropertyName("WeeklyCheckDay")]
    public int WeeklyCheckDay { get; set; }

    /// <summary>
    /// Whether to automatically download updates when found
    /// </summary>
    [JsonPropertyName("AutoDownload")]
    public bool AutoDownload { get; set; }

    /// <summary>
    /// Whether to automatically install downloaded updates
    /// </summary>
    [JsonPropertyName("AutoInstall")]
    public bool AutoInstall { get; set; }

    /// <summary>
    /// Whether to check for updates on application startup
    /// </summary>
    [JsonPropertyName("CheckOnStartup")]
    public bool CheckOnStartup { get; set; }

    /// <summary>
    /// Last date updates were checked (ISO format)
    /// </summary>
    [JsonPropertyName("LastCheckDate")]
    public string? LastCheckDate { get; set; }

    /// <summary>
    /// Last date an update was installed (ISO format)
    /// </summary>
    [JsonPropertyName("LastUpdateDate")]
    public string? LastUpdateDate { get; set; }

    /// <summary>
    /// Version number of last installed update
    /// </summary>
    [JsonPropertyName("LastInstalledVersion")]
    public string? LastInstalledVersion { get; set; }

    /// <summary>
    /// Indicates if updates are fully automatic (both download and install enabled)
    /// </summary>
    [JsonIgnore]
    public bool IsFullyAutomatic => AutoDownload && AutoInstall;

    /// <summary>
    /// Gets the frequency as a readable string
    /// </summary>
    [JsonIgnore]
    public string CheckFrequencyText => CheckFrequency switch
    {
        0 => "Daily",
        1 => "Weekly",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the weekly check day as a readable string
    /// </summary>
    [JsonIgnore]
    public string WeeklyCheckDayText => WeeklyCheckDay switch
    {
        0 => "Sunday",
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        _ => "Unknown"
    };
}

/// <summary>
/// Root configuration structure matching smtp-config.json
/// </summary>
public class SmtpConfigRoot
{
    /// <summary>
    /// Update configuration settings
    /// </summary>
    [JsonPropertyName("UpdateSettings")]
    public UpdateSettings? UpdateSettings { get; set; }

    /// <summary>
    /// Auto-update configuration settings (legacy support)
    /// </summary>
    [JsonPropertyName("AutoUpdateSettings")]
    public UpdateSettings? AutoUpdateSettings { get; set; }
}
