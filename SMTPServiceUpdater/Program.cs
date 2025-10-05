using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SMTPServiceUpdater.Models;
using SMTPServiceUpdater.Services;
using SMTPServiceUpdater.UI;

namespace SMTPServiceUpdater
{
    internal class Program
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            // Get root path (where executable is running)
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;

            // Parse command-line arguments
            bool autoMode = false;
            bool consoleMode = false;
            bool noRestart = false;
            bool showHelp = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                
                switch (arg)
                {
                    case "-a":
                    case "--auto":
                        autoMode = true;
                        break;
                    
                    case "-c":
                    case "--console":
                        consoleMode = true;
                        break;
                    
                    case "-n":
                    case "--no-restart":
                        noRestart = true;
                        break;
                    
                    case "-h":
                    case "--help":
                        showHelp = true;
                        break;
                }
            }

            // Show help and exit
            if (showHelp)
            {
                ShowHelp();
                return 0;
            }

            // Auto mode - headless automatic execution
            if (autoMode)
            {
                return await RunAutoModeAsync(rootPath, noRestart);
            }

            // Console mode - headless with user confirmation
            if (consoleMode)
            {
                return await RunConsoleModeAsync(rootPath, noRestart);
            }

            // Default - GUI mode
            return RunGuiMode(rootPath, noRestart);
        }

        /// <summary>
        /// Shows help information to console.
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("SMTP Service Updater v4.2.0");
            Console.WriteLine();
            Console.WriteLine("Usage: SMTPServiceUpdater.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -a, --auto                 Run automatically (no GUI, smart restart)");
            Console.WriteLine("                             Downloads from GitHub and installs without prompts");
            Console.WriteLine("  -n, --no-restart           Don't restart service/app after update");
            Console.WriteLine("  -c, --console              Run in console mode with output");
            Console.WriteLine("                             Downloads and prompts for confirmation before installing");
            Console.WriteLine("  -h, --help                 Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SMTPServiceUpdater.exe           (GUI mode - click Download then Install)");
            Console.WriteLine("  SMTPServiceUpdater.exe -a        (Auto mode - for scheduled tasks)");
            Console.WriteLine("  SMTPServiceUpdater.exe -a -n     (Auto mode - no restart)");
            Console.WriteLine("  SMTPServiceUpdater.exe -c -a     (Console mode - auto-run)");
            Console.WriteLine();
            Console.WriteLine("Default behavior (no arguments): Launches GUI");
        }

        /// <summary>
        /// Runs in automatic mode - downloads and installs without user interaction.
        /// Used for scheduled tasks from SMTP Service.
        /// </summary>
        private static async Task<int> RunAutoModeAsync(string rootPath, bool noRestart)
        {
            try
            {
                Console.WriteLine("SMTP Service Updater - Automatic Mode");
                Console.WriteLine("======================================");
                Console.WriteLine();

                // Create logger
                var logger = new UpdateLogger(rootPath);
                logger.WriteLog("Auto mode started", LogLevel.Info);

                // Create GitHub downloader
                var downloader = new GitHubDownloader(logger, rootPath);

                // Check for updates
                Console.WriteLine("Checking for updates from GitHub...");
                GitHubRelease? release = await downloader.CheckForUpdateAsync();

                if (release == null)
                {
                    Console.WriteLine("No updates available - already running latest version");
                    logger.WriteLog("No updates available", LogLevel.Info);
                    return 0;
                }

                Console.WriteLine($"Update available: {release.Version}");
                logger.WriteLog($"Downloading version {release.Version}", LogLevel.Info);

                // Download update
                Console.WriteLine("Downloading update...");
                string? downloadPath = await downloader.DownloadUpdateAsync(release, null);

                if (downloadPath == null)
                {
                    Console.WriteLine("ERROR: Download failed");
                    logger.WriteLog("Download failed", LogLevel.Error);
                    return 1;
                }

                Console.WriteLine($"Download complete: {downloadPath}");
                logger.WriteLog("Download successful", LogLevel.Success);

                // Install update
                Console.WriteLine("Installing update...");
                var installer = new UpdateInstaller(rootPath, null);
                var result = await installer.RunAsync(release.Version, noRestart);

                if (result.Success)
                {
                    Console.WriteLine("Update completed successfully!");
                    logger.WriteLog("Auto mode completed successfully", LogLevel.Success);
                    return 0;
                }
                else
                {
                    Console.WriteLine("Update completed with errors - check log for details");
                    logger.WriteLog("Auto mode completed with errors", LogLevel.Error);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Runs in console mode - downloads and prompts for confirmation before installing.
        /// </summary>
        private static async Task<int> RunConsoleModeAsync(string rootPath, bool noRestart)
        {
            try
            {
                Console.WriteLine("SMTP Service Updater - Console Mode");
                Console.WriteLine("====================================");
                Console.WriteLine();

                // Create logger
                var logger = new UpdateLogger(rootPath);
                logger.WriteLog("Console mode started", LogLevel.Info);

                // Create GitHub downloader
                var downloader = new GitHubDownloader(logger, rootPath);

                // Check for updates
                Console.WriteLine("Checking for updates from GitHub...");
                GitHubRelease? release = await downloader.CheckForUpdateAsync();

                if (release == null)
                {
                    Console.WriteLine("No updates available - already running latest version");
                    logger.WriteLog("No updates available", LogLevel.Info);
                    return 0;
                }

                Console.WriteLine($"Update available: {release.Version}");
                string currentVersion = downloader.GetCurrentVersion();
                Console.WriteLine($"Current version: {currentVersion}");
                Console.WriteLine();

                // Download update
                Console.WriteLine("Downloading update...");
                string? downloadPath = await downloader.DownloadUpdateAsync(release, null);

                if (downloadPath == null)
                {
                    Console.WriteLine("ERROR: Download failed");
                    logger.WriteLog("Download failed", LogLevel.Error);
                    return 1;
                }

                Console.WriteLine($"Download complete: {downloadPath}");
                Console.WriteLine();

                // Prompt for confirmation
                Console.Write($"Install version {release.Version}? (Y/N): ");
                string? response = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(response) || 
                    (!response.Equals("Y", StringComparison.OrdinalIgnoreCase) && 
                     !response.Equals("Yes", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("Update cancelled by user");
                    logger.WriteLog("User cancelled installation", LogLevel.Warning);
                    return 2; // User cancelled
                }

                // Install update
                Console.WriteLine();
                Console.WriteLine("Installing update...");
                var installer = new UpdateInstaller(rootPath, null);
                var result = await installer.RunAsync(release.Version, noRestart);

                if (result.Success)
                {
                    Console.WriteLine();
                    Console.WriteLine("Update completed successfully!");
                    logger.WriteLog("Console mode completed successfully", LogLevel.Success);
                    return 0;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Update completed with errors - check log for details");
                    logger.WriteLog("Console mode completed with errors", LogLevel.Error);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Runs in GUI mode - default behavior.
        /// </summary>
        private static int RunGuiMode(string rootPath, bool noRestart)
        {
            try
            {
                // Enable visual styles
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Create and run main form
                var mainForm = new UpdaterMainForm(noRestart, rootPath);
                Application.Run(mainForm);

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fatal error: {ex.Message}",
                    "SMTP Service Updater Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                return 1;
            }
        }
    }
}
