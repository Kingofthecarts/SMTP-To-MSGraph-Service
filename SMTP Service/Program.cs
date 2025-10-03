using SMTP_Service;
using SMTP_Service.Services;
using SMTP_Service.UI;
using Serilog;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting.WindowsServices;

// Check for pending update script replacement
CheckAndReplaceUpdateScript();

// Load configuration to check run mode
var initialConfigManager = new SMTP_Service.Managers.ConfigurationManager();
var initialConfig = initialConfigManager.LoadConfiguration();

// Initialize logging first
var logManager = new SMTP_Service.Managers.LogManager(
    initialConfig.LogSettings.LogLocation,
    initialConfig.LogSettings.LogLevel);

string logFilePath = logManager.InitializeLogging();

// Determine run mode: command line args override config setting
bool runAsTray = args.Contains("--tray");
bool runAsConsoleWithTray = args.Contains("--console");

// If no command line args, use config setting (default is 1 if config is missing)
if (!runAsTray && !runAsConsoleWithTray)
{
    switch (initialConfig.ApplicationSettings.RunMode)
    {
        case 1:
            runAsConsoleWithTray = true;
            break;
        case 2:
            runAsTray = true;
            break;
        // case 0 or default: stay as service/console mode
    }
}

// Determine the final run mode for display and logging
string runModeName;
string runModeDescription;
string runModeSource;

if (runAsTray)
{
    runModeName = "Tray Only";
    runModeDescription = "RunMode 2: System tray icon only";
}
else if (runAsConsoleWithTray)
{
    runModeName = "Console + Tray";
    runModeDescription = "RunMode 1: Console with system tray icon (DEFAULT)";
}
else
{
    runModeName = "Service/Console";
    runModeDescription = "RunMode 0: Service or console mode";
}

// Determine source of run mode
if (args.Contains("--tray") || args.Contains("--console"))
{
    runModeSource = "Command line argument (overriding config)";
}
else
{
    runModeSource = $"Configuration file (RunMode={initialConfig.ApplicationSettings.RunMode})";
}

// Display run mode information
Console.WriteLine($"\n========== RUN MODE: {runModeName} ==========");
Console.WriteLine($"{runModeDescription}");
Console.WriteLine($"Source: {runModeSource}");
Console.WriteLine("==========================================\n");

// Display log information
Console.WriteLine($"Log File: {logFilePath}");
Console.WriteLine($"Log Level: {initialConfig.LogSettings.LogLevel}");
Console.WriteLine("==========================================\n");

// Log startup information
Log.Information("\n\n");
Log.Information("================================================================================");
Log.Information("===                        APPLICATION START                                ===");
Log.Information("================================================================================");
Log.Information("SMTP to Graph Relay - Application Started");
Log.Information($"Run Mode: {runModeName} ({runModeDescription})");
Log.Information($"Source: {runModeSource}");
Log.Information($"Log File: {logFilePath}");
Log.Information("================================================================================");

// Check if running as tray application
if (runAsTray)
{
    Console.WriteLine("Starting in TRAY mode...");
    Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
    Console.WriteLine($"Configuration loaded from: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "smtp-config.json")}");
    
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new TrayApplicationContext());
    
    Log.Information("Application shutting down");
    Log.CloseAndFlush();
    return;
}

// Check if running in console mode with tray icon
bool showTray = runAsConsoleWithTray;

if (showTray)
{
    Console.WriteLine("Starting in CONSOLE + TRAY mode...");
    Console.WriteLine("Console window will remain visible with system tray icon");
    Console.WriteLine("============================================\n");
}
else
{
    Console.WriteLine("Starting in SERVICE/CONSOLE mode...");
}

Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Running as Administrator: {new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}");
Console.WriteLine("============================================");
Console.WriteLine("Press Ctrl+C to stop the service");
Console.WriteLine("============================================\n");

// Initialize Configuration Manager
var configManager = new SMTP_Service.Managers.ConfigurationManager();
var config = configManager.LoadConfiguration();

try
{
    Console.WriteLine("Configuring services...");
    Log.Information($"SMTP Port: {config.SmtpSettings.Port}");
    Log.Information($"Authentication Required: {config.SmtpSettings.RequireAuthentication}");
    Log.Information($"Configured Users: {config.SmtpSettings.Credentials.Count}");
    
    Console.WriteLine("Creating host builder...");
    
    var builder = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            Console.WriteLine("Configuring services...");
            
            // Register configuration
            services.AddSingleton(config);
            services.AddSingleton(config.SmtpSettings);
            services.AddSingleton(config.GraphSettings);
            services.AddSingleton(config.QueueSettings);
            services.AddSingleton(configManager);

            // Register services
            services.AddSingleton<SMTP_Service.Managers.QueueManager>();
            services.AddSingleton<SMTP_Service.Managers.StatisticsManager>();
            services.AddSingleton<GraphEmailService>();
            services.AddSingleton<SmtpServerService>();

            // Register background services
            services.AddHostedService<Worker>();
            services.AddHostedService<QueueProcessorService>();
            
            Console.WriteLine("Services configured");
        });

    // Configure as Windows Service only if running as a service
    Console.WriteLine("Checking if running as Windows Service...");
    if (WindowsServiceHelpers.IsWindowsService())
    {
        Console.WriteLine("Running as Windows Service - configuring service support");
        builder.UseWindowsService(options =>
        {
            options.ServiceName = "SMTP to Graph Relay";
        });
    }
    else
    {
        Console.WriteLine("Running as console application (not as service)");
    }

    Console.WriteLine("Building host...");
    var host = builder.Build();
    Console.WriteLine("Host built successfully");

    // Set the logger factory for SmtpServerService
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
    SmtpLoggerFactory.Factory = loggerFactory;
    Console.WriteLine("Logger factory configured");

    // If --console mode, start the tray icon in a separate thread
    if (showTray)
    {
        Console.WriteLine("Starting system tray icon...");
        var trayThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        });
        trayThread.SetApartmentState(ApartmentState.STA);
        trayThread.IsBackground = true;
        trayThread.Start();
        Console.WriteLine("System tray icon started. You can right-click it to access configuration.");
    }

    Console.WriteLine("Starting host... (SMTP server should start now)");
    await host.RunAsync();
    Console.WriteLine("Host stopped");
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: {ex.Message}");
    Console.WriteLine($"Exception Type: {ex.GetType().Name}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Local function to handle update script replacement
void CheckAndReplaceUpdateScript()
{
    try
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var newScriptPath = Path.Combine(baseDir, "Install-Update-NEW.ps1");
        var oldScriptPath = Path.Combine(baseDir, "Install-Update.ps1");

        if (File.Exists(newScriptPath))
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("   PENDING UPDATE SCRIPT REPLACEMENT");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("Detected new update script from previous update.");
            Console.WriteLine("Replacing Install-Update.ps1...");

            // Delete the old script if it exists
            if (File.Exists(oldScriptPath))
            {
                File.Delete(oldScriptPath);
                Console.WriteLine("[OK] Removed old update script");
            }

            // Rename the new script
            File.Move(newScriptPath, oldScriptPath);
            Console.WriteLine("[OK] New update script activated");
            Console.WriteLine();
            Console.WriteLine("Update script replacement complete!");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            // Small delay to let user see the message
            Thread.Sleep(1000);
        }
    }
    catch (Exception ex)
    {
        // Don't crash the application if this fails, just log it
        Console.WriteLine($"WARNING: Could not replace update script: {ex.Message}");
        Console.WriteLine("This will not affect normal operation.");
        Console.WriteLine();
    }
}
