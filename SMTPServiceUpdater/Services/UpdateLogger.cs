using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services;

/// <summary>
/// Handles logging for the update process with file, console, and GUI output
/// </summary>
public class UpdateLogger
{
private readonly string _logFilePath;
private readonly string _rootPath;
private readonly object _fileLock = new object();
        private readonly bool _isAppending;

/// <summary>
/// Event raised when a log message is written (for GUI updates)
/// </summary>
        public event EventHandler<LogMessage>? LogMessageReceived;

/// <summary>
/// Initializes a new instance of UpdateLogger
/// </summary>
/// <param name="rootPath">Root path where SMTP Service is installed</param>
/// <param name="existingLogPath">Optional: existing log file path to append to</param>
/// <exception cref="ArgumentException">Thrown when rootPath is null or empty</exception>
/// <exception cref="DirectoryNotFoundException">Thrown when logs folder doesn't exist</exception>
public UpdateLogger(string rootPath, string? existingLogPath = null)
{
// Validate rootPath
if (string.IsNullOrWhiteSpace(rootPath))
{
                throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
}

_rootPath = rootPath;

            // Build path to logs folder
var logsFolder = Path.Combine(rootPath, "logs");

// Create logs folder if it doesn't exist
if (!Directory.Exists(logsFolder))
{
try
{
    Directory.CreateDirectory(logsFolder);
}
catch (Exception ex)
{
        throw new DirectoryNotFoundException($"Unable to create logs folder at: {logsFolder}. Error: {ex.Message}");
                }
}

            // Use existing log if provided and valid, otherwise generate new
if (!string.IsNullOrWhiteSpace(existingLogPath))
{
                // Validate the log path
    ValidateLogPath(existingLogPath, rootPath);
    
    if (File.Exists(existingLogPath))
{
        _logFilePath = existingLogPath;
        _isAppending = true;
    }
else
{
    // File doesn't exist, create new
    _logFilePath = GenerateLogFilePath(logsFolder);
        _isAppending = false;
        }
            }
            else
            {
                // Generate incremental log file name
                _logFilePath = GenerateLogFilePath(logsFolder);
                _isAppending = false;
            }

            // Validate the generated log path is within rootPath (security check)
            ValidateLogPath(_logFilePath, rootPath);

            // Create or append to the log file
            try
            {
                if (_isAppending)
                {
                    // Add separator for resumed logs
                    File.AppendAllText(_logFilePath, Environment.NewLine);
                    File.AppendAllText(_logFilePath, "=== RESUMED AFTER SELF-UPDATE ===" + Environment.NewLine);
                    File.AppendAllText(_logFilePath, Environment.NewLine);
                }
                else
                {
                    // Create new empty log file
                    File.WriteAllText(_logFilePath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                // If we can't create the log file, we can still log to console
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to create log file: {ex.Message}");
                Console.ResetColor();
            }
        }

    /// <summary>
    /// Writes a log message with specified level
    /// </summary>
    /// <param name="message">Message to log</param>
    /// <param name="level">Log level (default: Info)</param>
    public void WriteLog(string message, LogLevel level = LogLevel.Info)
    {
        var logMessage = new LogMessage(message, level);
        var formattedMessage = logMessage.ToString();

        // Write to file (thread-safe)
        WriteToFile(formattedMessage);

        // Write to console with color coding
        WriteToConsole(message, level);

        // Raise event for GUI (thread-safe)
        RaiseLogEvent(logMessage);
    }

    /// <summary>
    /// Writes a formatted header to the log
    /// </summary>
    /// <param name="header">Header text</param>
    public void WriteHeader(string header)
    {
        var separator = new string('=', 60);
        WriteLog(separator, LogLevel.Info);
        WriteLog(header, LogLevel.Info);
        WriteLog(separator, LogLevel.Info);
    }

    /// <summary>
    /// Writes a visual separator line to the log
    /// </summary>
    public void WriteSeparator()
    {
        var separator = new string('-', 60);
        WriteLog(separator, LogLevel.Info);
    }

    /// <summary>
    /// Generates an incremental log file path for the current date
    /// </summary>
    /// <param name="logsFolder">Path to logs folder</param>
    /// <returns>Full path to new log file</returns>
    private string GenerateLogFilePath(string logsFolder)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var counter = 1;

        // Find existing log files for today
        var pattern = $"update_{today}_*.txt";
        var existingFiles = Directory.GetFiles(logsFolder, pattern);

        // Find highest counter
        foreach (var file in existingFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('_');
            
            if (parts.Length >= 3 && int.TryParse(parts[^1], out int fileCounter))
            {
                if (fileCounter >= counter)
                {
                    counter = fileCounter + 1;
                }
            }
        }

        // Generate new file name with incremented counter
        var logFileName = $"update_{today}_{counter:D2}.txt";
        return Path.Combine(logsFolder, logFileName);
    }

    /// <summary>
    /// Validates that the log file path is within the root path (prevents directory traversal)
    /// </summary>
    /// <param name="logPath">Log file path to validate</param>
    /// <param name="rootPath">Root path that must contain the log file</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when log path is outside root path</exception>
    private void ValidateLogPath(string logPath, string rootPath)
    {
        var fullLogPath = Path.GetFullPath(logPath);
        var fullRootPath = Path.GetFullPath(rootPath);

        if (!fullLogPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Log path '{fullLogPath}' is outside root path '{fullRootPath}'");
        }
    }

    /// <summary>
    /// Writes a message to the log file (thread-safe)
    /// </summary>
    /// <param name="message">Formatted message to write</param>
    private void WriteToFile(string message)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // If file write fails, log to console only
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Writes a message to console with color coding based on log level
    /// </summary>
    /// <param name="message">Message to write</param>
    /// <param name="level">Log level for color selection</param>
    private void WriteToConsole(string message, LogLevel level)
    {
        try
        {
            // Set console color based on log level
            Console.ForegroundColor = level switch
            {
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };

            // Write timestamp and level prefix
            Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ");
            Console.Write($"[{level.ToString().ToUpper()}] ");
            
            // Reset color for message text
            Console.ResetColor();
            Console.WriteLine(message);
        }
        catch
        {
            // If console output fails, just continue (we still wrote to file)
            // This can happen when running as a GUI app without console
        }
    }

    /// <summary>
    /// Raises the LogMessageReceived event (thread-safe)
    /// </summary>
    /// <param name="logMessage">Log message to send to subscribers</param>
    private void RaiseLogEvent(LogMessage logMessage)
    {
        // Create local copy of event handler to avoid race conditions
        var handler = LogMessageReceived;
        
        try
        {
            handler?.Invoke(this, logMessage);
        }
        catch
        {
            // If event handler throws, don't crash the logger
            // The GUI will handle its own errors
        }
    }

    /// <summary>
    /// Gets the path to the current log file
    /// </summary>
    public string LogFilePath => _logFilePath;
}
