namespace SMTPServiceUpdater.Models;

/// <summary>
/// Represents information about a backup created during the update process
/// </summary>
public class BackupInfo
{
    /// <summary>
    /// Full path to the backup directory
    /// </summary>
    public string BackupPath { get; }

    /// <summary>
    /// When the backup was created
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Version number that was backed up
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Initializes a new backup information object
    /// </summary>
    /// <param name="backupPath">Full path to backup directory</param>
    /// <param name="timestamp">When the backup was created</param>
    /// <param name="version">Version that was backed up</param>
    public BackupInfo(string backupPath, DateTime timestamp, string version)
    {
        BackupPath = backupPath ?? throw new ArgumentNullException(nameof(backupPath));
        Timestamp = timestamp;
        Version = version ?? "Unknown";
    }

    /// <summary>
    /// Returns a string representation of the backup info
    /// </summary>
    public override string ToString()
    {
        return $"Backup of v{Version} at {BackupPath} (created {Timestamp:yyyy-MM-dd HH:mm:ss})";
    }
}
