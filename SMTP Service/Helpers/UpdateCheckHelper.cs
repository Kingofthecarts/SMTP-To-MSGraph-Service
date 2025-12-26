using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using Serilog;

namespace SMTP_Service.Helpers
{
    public static class UpdateCheckHelper
    {
        public static void CheckForDownloadedUpdates()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var updateDir = Path.Combine(baseDir, "updates");

                if (!Directory.Exists(updateDir))
                {
                    return; // No updates directory, nothing to check
                }

                // Get all zip files in updates directory
                var updateFiles = Directory.GetFiles(updateDir, "*.zip");

                if (updateFiles.Length == 0)
                {
                    return; // No update files found
                }

                // Get current version
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

                // Check each file to see if it's a higher version
                foreach (var updateFile in updateFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(updateFile);
                    
                    // Try to parse version from filename (e.g., "1.5.0.zip" -> Version(1, 5, 0))
                    if (Version.TryParse(fileName, out var updateVersion))
                    {
                        if (updateVersion > currentVersion)
                        {
                            Log.Information($"Found downloaded update: {fileName} (Current: {currentVersion})");
                            PromptForInstallation(updateFile, fileName, currentVersion.ToString());
                            return; // Only prompt for the first newer version found
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for downloaded updates");
            }
        }

        private static void PromptForInstallation(string updateFilePath, string version, string currentVersion)
        {
            var result = MessageBox.Show(
                $"A new version ({version}) has been downloaded and is ready to install.\n\n" +
                $"Current version: {currentVersion}\n" +
                $"New version: {version}\n\n" +
                "The installer will:\n" +
                "• Stop the SMTP Service if running\n" +
                "• Backup existing files\n" +
                "• Install the update\n" +
                "• Restart the service if it was running\n\n" +
                "Do you want to install this update now?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                InstallUpdate();
            }
            else
            {
                Log.Information("User postponed update installation");
                MessageBox.Show(
                    "Update installation postponed.\n\n" +
                    "You can install the update later by selecting 'Check for Updates' from the system tray menu.",
                    "Update Postponed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private static void InstallUpdate()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                var updaterPath = Path.Combine(baseDir, "SMTPServiceUpdater.exe");

                if (!File.Exists(updaterPath))
                {
                    MessageBox.Show(
                        $"SMTPServiceUpdater.exe not found at:\n{updaterPath}\n\n" +
                        "Please ensure the updater exists in the application directory.",
                        "Updater Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Log.Information("Launching SMTPServiceUpdater");

                // Launch SMTPServiceUpdater.exe
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        WorkingDirectory = baseDir,
                        UseShellExecute = true
                    }
                };

                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to launch update installer");
                MessageBox.Show(
                    $"Failed to launch update installer:\n{ex.Message}\n\n" +
                    $"You can manually run SMTPServiceUpdater.exe from:\n{baseDir}",
                    "Update Installation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
