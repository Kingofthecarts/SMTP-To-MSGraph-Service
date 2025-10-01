using SMTP_Service;
using SMTP_Service.Services;
using SMTP_Service.UI;
using Serilog;
using Serilog.Events;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting.WindowsServices;

// Load configuration to check run mode
var initialConfigManager = new SMTP_Service.Managers.ConfigurationManager();
var initialConfig = initialConfigManager.LoadConfiguration();

// Determine run mode: command line args override config setting
bool runAsTray = args.Contains("--tray");
bool runAsConsoleWithTray = args.Contains("--console");

// If no command line args, use config setting
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

// Check if running as tray application
if (runAsTray)
{
    Console.WriteLine("Starting in TRAY mode...");
    Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
    
    // Initialize logging even in tray mode
    var trayConfigManager = new SMTP_Service.Managers.ConfigurationManager();
    var trayConfig = trayConfigManager.LoadConfiguration();
    
    Console.WriteLine($"Configuration loaded from: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "smtp-config.json")}");
    Console.WriteLine($"Logs will be written to: {trayConfig.LogSettings.LogFilePath}");
    
    // Ensure logs directory exists
    var trayLogDirectory = Path.GetDirectoryName(trayConfig.LogSettings.LogFilePath);
    if (!string.IsNullOrEmpty(trayLogDirectory) && !Directory.Exists(trayLogDirectory))
    {
        Directory.CreateDirectory(trayLogDirectory);
    }
    
    // Configure Serilog for tray mode
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(Enum.Parse<LogEventLevel>(trayConfig.LogSettings.LogLevel))
        .Enrich.FromLogContext()
        .WriteTo.File(
            trayConfig.LogSettings.LogFilePath,
            rollingInterval: Enum.Parse<RollingInterval>(trayConfig.LogSettings.RollingInterval),
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
    
    Log.Information("SMTP to Graph Relay - Tray Application Started");
    
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new TrayApplicationContext());
    
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
    Console.WriteLine("Starting in SERVICE mode...");
}

Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Running as Administrator: {new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}");
Console.WriteLine("============================================");
Console.WriteLine("Press Ctrl+C to stop the service");
Console.WriteLine("============================================\n");

// Otherwise, run as service
// Initialize Configuration Manager
var configManager = new SMTP_Service.Managers.ConfigurationManager();
var config = configManager.LoadConfiguration();

// Ensure logs directory exists
var logDirectory = Path.GetDirectoryName(config.LogSettings.LogFilePath);
if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
    Console.WriteLine($"Created log directory: {logDirectory}");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(Enum.Parse<LogEventLevel>(config.LogSettings.LogLevel))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        config.LogSettings.LogFilePath,
        rollingInterval: Enum.Parse<RollingInterval>(config.LogSettings.RollingInterval),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Console.WriteLine("Configuring Serilog...");
    Log.Information("Starting SMTP to MS Graph Relay Service");
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
