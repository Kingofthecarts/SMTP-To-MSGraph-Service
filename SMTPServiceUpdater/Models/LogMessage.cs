namespace SMTPServiceUpdater.Models;

/// <summary>
/// Log level enumeration for categorizing log messages
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// General informational messages
    /// </summary>
    Info,

    /// <summary>
    /// Successful operation messages
    /// </summary>
    Success,

    /// <summary>
    /// Warning messages for non-critical issues
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages that don't stop execution
    /// </summary>
    Error,

    /// <summary>
    /// Critical error messages that stop execution
    /// </summary>
    Critical
}

/// <summary>
/// Represents a log message with timestamp and severity level
/// Used for passing log data from UpdateInstaller to GUI via IProgress pattern
/// </summary>
public class LogMessage
{
    /// <summary>
    /// The log message text
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The severity level of the message
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new log message
    /// </summary>
    /// <param name="message">The message text</param>
    /// <param name="level">The severity level</param>
    public LogMessage(string message, LogLevel level = LogLevel.Info)
    {
        Message = message ?? string.Empty;
        Level = level;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Returns the formatted log message with timestamp and level
    /// </summary>
    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level.ToString().ToUpper()}] {Message}";
    }
}
