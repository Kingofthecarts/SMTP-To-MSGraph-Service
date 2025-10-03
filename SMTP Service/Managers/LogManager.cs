using Serilog;
using Serilog.Events;

namespace SMTP_Service.Managers
{
    public class LogManager
    {
        private readonly string _logLocation;
        private readonly string _logLevel;
        private string? _currentLogFile;

        public LogManager(string logLocation, string logLevel)
        {
            _logLocation = logLocation;
            _logLevel = logLevel;
        }

        public string InitializeLogging()
        {
            // Validate and create log directory
            string logDirectory = ValidateAndCreateLogDirectory(_logLocation);
            
            // Generate incremental log filename
            _currentLogFile = GenerateIncrementalLogFileName(logDirectory);
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(Enum.Parse<LogEventLevel>(_logLevel))
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    _currentLogFile,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            return _currentLogFile;
        }

        private string ValidateAndCreateLogDirectory(string configuredLocation)
        {
            try
            {
                // If location is relative or just a folder name, make it absolute
                string absolutePath;
                if (!Path.IsPathRooted(configuredLocation))
                {
                    absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredLocation);
                }
                else
                {
                    absolutePath = configuredLocation;
                }

                // Try to create the directory
                if (!Directory.Exists(absolutePath))
                {
                    Directory.CreateDirectory(absolutePath);
                }

                // Verify we can write to it
                string testFile = Path.Combine(absolutePath, ".test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return absolutePath;
            }
            catch
            {
                // If configured location fails, fall back to logs folder beside app
                string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                
                if (!Directory.Exists(fallbackPath))
                {
                    Directory.CreateDirectory(fallbackPath);
                }

                return fallbackPath;
            }
        }

        private string GenerateIncrementalLogFileName(string logDirectory)
        {
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            string baseFileName = $"smtp-relay-{dateStr}";
            int counter = 1;

            string logFilePath = Path.Combine(logDirectory, $"{baseFileName}.log");

            // Find the next available number
            while (File.Exists(logFilePath))
            {
                logFilePath = Path.Combine(logDirectory, $"{baseFileName}-{counter}.log");
                counter++;
            }

            return logFilePath;
        }

        public string GetCurrentLogFile()
        {
            return _currentLogFile ?? "Not initialized";
        }
    }
}
