using System.Windows.Forms;
using System.Diagnostics;
using Serilog;
using SMTP_Service.Services;

namespace SMTP_Service.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon = null!;
        private ContextMenuStrip _contextMenu = null!;
        private const string ServiceName = "SMTP to Graph Relay";
        
        // Static instance to allow other forms to refresh the menu
        public static TrayApplicationContext? Instance { get; private set; }

        public TrayApplicationContext()
        {
            Instance = this;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenuStrip();
            
            var configMenuItem = new ToolStripMenuItem("Configuration", null, ShowConfiguration);
            var statusMenuItem = new ToolStripMenuItem("Service Status", null, ShowStatus);
            var logsMenuItem = new ToolStripMenuItem("View Logs", null, ViewLogs);
            var updateMenuItem = new ToolStripMenuItem("Check for Updates", null, CheckForUpdates);
            var separatorItem = new ToolStripSeparator();

            _contextMenu.Items.Add(configMenuItem);
            _contextMenu.Items.Add(statusMenuItem);
            _contextMenu.Items.Add(logsMenuItem);
            _contextMenu.Items.Add(updateMenuItem);
            _contextMenu.Items.Add(separatorItem);

            // Check if service is installed and add appropriate menu items
            if (IsServiceInstalled())
            {
                var startServiceMenuItem = new ToolStripMenuItem("Start Service", null, StartService);
                var stopServiceMenuItem = new ToolStripMenuItem("Stop Service", null, StopService);
                var restartServiceMenuItem = new ToolStripMenuItem("Restart Service", null, RestartService);
                
                _contextMenu.Items.Add(startServiceMenuItem);
                _contextMenu.Items.Add(stopServiceMenuItem);
                _contextMenu.Items.Add(restartServiceMenuItem);
            }
            else
            {
                var installServiceMenuItem = new ToolStripMenuItem("Install Service", null, InstallService);
                _contextMenu.Items.Add(installServiceMenuItem);
            }

            var separator2Item = new ToolStripSeparator();
            var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);

            _contextMenu.Items.Add(separator2Item);
            _contextMenu.Items.Add(exitMenuItem);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "SMTP to Graph Relay Service"
            };

            _trayIcon.DoubleClick += (s, e) => ShowConfiguration(s, e);
        }

        private bool IsServiceInstalled()
        {
            try
            {
                var service = System.ServiceProcess.ServiceController.GetServices()
                    .FirstOrDefault(s => s.ServiceName == ServiceName);
                return service != null;
            }
            catch
            {
                return false;
            }
        }

        private void ShowConfiguration(object? sender, EventArgs e)
        {
            var configForm = new ConfigurationForm();
            configForm.ShowDialog();
        }

        private void ShowStatus(object? sender, EventArgs e)
        {
            try
            {
                var service = System.ServiceProcess.ServiceController.GetServices()
                    .FirstOrDefault(s => s.ServiceName == ServiceName);

                if (service != null)
                {
                    MessageBox.Show(
                        $"Service Status: {service.Status}\n" +
                        $"Service Name: {service.ServiceName}\n" +
                        $"Display Name: {service.DisplayName}",
                        "Service Status",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Service is not installed.\n\n" +
                        "To install the service, use the 'Install Service' option in the system tray menu.",
                        "Service Status",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking service status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewLogs(object? sender, EventArgs e)
        {
            try
            {
                // Load configuration to get the actual log path
                var configManager = new Managers.ConfigurationManager();
                var config = configManager.LoadConfiguration();
                
                // Get the log directory from configuration
                var logLocation = config.LogSettings.LogLocation;
                string logDirectory;
                
                if (string.IsNullOrWhiteSpace(logLocation))
                {
                    // Fallback to base directory + logs
                    logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                }
                else if (!Path.IsPathRooted(logLocation))
                {
                    // Relative path - make it absolute
                    logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logLocation);
                }
                else
                {
                    // Already absolute path
                    logDirectory = logLocation;
                }
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // Check if there are any log files
                var logFiles = Directory.GetFiles(logDirectory, "*.log");
                var txtFiles = Directory.GetFiles(logDirectory, "*.txt");
                
                if (logFiles.Length == 0 && txtFiles.Length == 0)
                {
                    MessageBox.Show(
                        $"Log directory exists but no log files found yet.\n\n" +
                        $"Location: {logDirectory}\n\n" +
                        $"Logs will be created when the service runs or tests are performed.\n\n" +
                        $"Tip: Use 'Show File Locations' to see all paths.",
                        "No Logs Yet",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                
                // Open the logs folder
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDirectory,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening logs: {ex.Message}\n\n" +
                    $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}\n\n" +
                    $"If you can't find the logs, look in:\n" +
                    $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void InstallService(object? sender, EventArgs e)
        {
            try
            {
                // Check if service is already installed
                if (IsServiceInstalled())
                {
                    MessageBox.Show(
                        "Service is already installed.\n\n" +
                        "Use the Start/Stop/Restart options to control the service.",
                        "Service Already Installed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    "This will install the SMTP to Graph Relay as a Windows Service.\n\n" +
                    "The installation requires administrator privileges.\n\n" +
                    "Do you want to continue?",
                    "Install Service",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Get the path to the executable
                    var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SMTP Service.exe");
                    
                    if (!File.Exists(exePath))
                    {
                        MessageBox.Show(
                            $"Service executable not found at:\n{exePath}\n\n" +
                            "Please ensure 'SMTP Service.exe' exists in the application directory.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    // Install the service using sc create
                    var createProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sc",
                            Arguments = $"create \"{ServiceName}\" binPath= \"{exePath}\" start= auto DisplayName= \"{ServiceName}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            Verb = "runas" // Run as administrator
                        }
                    };

                    createProcess.Start();
                    string output = createProcess.StandardOutput.ReadToEnd();
                    string error = createProcess.StandardError.ReadToEnd();
                    createProcess.WaitForExit();

                    if (createProcess.ExitCode == 0 || output.Contains("SUCCESS"))
                    {
                        Serilog.Log.Information($"Service '{ServiceName}' installed successfully");
                        
                        // Set the service description
                        var descProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "sc",
                                Arguments = $"description \"{ServiceName}\" \"Relays SMTP emails to Microsoft 365 via MS Graph API\"",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };
                        descProcess.Start();
                        descProcess.WaitForExit();

                        // Refresh the menu to show service controls
                        RefreshMenu();

                        MessageBox.Show(
                            "Service installed successfully!\n\n" +
                            "The system tray menu has been updated with service controls.\n\n" +
                            "You can now start the service from the system tray menu.",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        Serilog.Log.Error($"Service installation failed: {error}");
                        MessageBox.Show(
                            $"Service installation failed.\n\n" +
                            $"Error: {error}\n\n" +
                            "Common causes:\n" +
                            "- Service already exists\n" +
                            "- Insufficient permissions (run as administrator)\n" +
                            "- Invalid executable path",
                            "Installation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    "Administrator privileges are required to install the service.\n\n" +
                    "Please run this application as administrator and try again.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error installing service: {ex.Message}\n\n" +
                    "Make sure you have administrator privileges and try again.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void RefreshMenu()
        {
            // Clear existing menu items
            _contextMenu.Items.Clear();
            
            // Rebuild the menu with updated service status
            var configMenuItem = new ToolStripMenuItem("Configuration", null, ShowConfiguration);
            var statusMenuItem = new ToolStripMenuItem("Service Status", null, ShowStatus);
            var logsMenuItem = new ToolStripMenuItem("View Logs", null, ViewLogs);
            var updateMenuItem = new ToolStripMenuItem("Check for Updates", null, CheckForUpdates);
            var separatorItem = new ToolStripSeparator();

            _contextMenu.Items.Add(configMenuItem);
            _contextMenu.Items.Add(statusMenuItem);
            _contextMenu.Items.Add(logsMenuItem);
            _contextMenu.Items.Add(updateMenuItem);
            _contextMenu.Items.Add(separatorItem);

            // Check if service is installed and add appropriate menu items
            if (IsServiceInstalled())
            {
                var startServiceMenuItem = new ToolStripMenuItem("Start Service", null, StartService);
                var stopServiceMenuItem = new ToolStripMenuItem("Stop Service", null, StopService);
                var restartServiceMenuItem = new ToolStripMenuItem("Restart Service", null, RestartService);
                
                _contextMenu.Items.Add(startServiceMenuItem);
                _contextMenu.Items.Add(stopServiceMenuItem);
                _contextMenu.Items.Add(restartServiceMenuItem);
            }
            else
            {
                var installServiceMenuItem = new ToolStripMenuItem("Install Service", null, InstallService);
                _contextMenu.Items.Add(installServiceMenuItem);
            }

            var separator2Item = new ToolStripSeparator();
            var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);

            _contextMenu.Items.Add(separator2Item);
            _contextMenu.Items.Add(exitMenuItem);
        }

        private void StartService(object? sender, EventArgs e)
        {
            ExecuteServiceCommand("start");
        }

        private void StopService(object? sender, EventArgs e)
        {
            ExecuteServiceCommand("stop");
        }

        private void RestartService(object? sender, EventArgs e)
        {
            ExecuteServiceCommand("stop");
            System.Threading.Thread.Sleep(2000);
            ExecuteServiceCommand("start");
        }

        private void ExecuteServiceCommand(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"{command} \"{ServiceName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = "runas" // Run as administrator
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    MessageBox.Show($"Service {command} command executed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Service {command} failed:\n{error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing service command: {ex.Message}\n\nYou may need to run this application as administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CheckForUpdates(object? sender, EventArgs e)
        {
            try
            {
                var updateService = new UpdateService();
                
                // Check for updates
                var result = await updateService.CheckForUpdateAsync();
                
                if (!string.IsNullOrEmpty(result.Error))
                {
                    MessageBox.Show(
                        $"Unable to check for updates:\n{result.Error}\n\n" +
                        "Make sure you have configured GitHub updates using Setup-GitHubToken.ps1",
                        "Update Check Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                
                if (result.Available)
                {
                    var message = $"A new version is available!\n\n" +
                                 $"Current Version: {result.CurrentVersion}\n" +
                                 $"Latest Version: {result.LatestVersion}\n" +
                                 $"File: {result.FileName} ({result.FileSize / 1024 / 1024:N1} MB)\n\n";
                    
                    if (!string.IsNullOrEmpty(result.ReleaseNotes))
                    {
                        var notes = result.ReleaseNotes;
                        if (notes.Length > 200)
                            notes = notes.Substring(0, 197) + "...";
                        message += $"Release Notes:\n{notes}\n\n";
                    }
                    
                    message += "Would you like to download the update?";
                    
                    if (MessageBox.Show(message, "Update Available",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        // Show progress dialog
                        var progressDialog = new UpdateProgressDialog();
                        
                        // Start download in background
                        _ = Task.Run(async () =>
                        {
                            var progress = new Progress<int>(percent => progressDialog.UpdateProgress(percent));
                            var success = await updateService.DownloadUpdateAsync(
                                result.DownloadUrl!, result.FileName!, progress);
                            
                            if (success)
                            {
                                progressDialog.CompleteDownload();
                            }
                            else
                            {
                                progressDialog.ShowError("Download failed");
                            }
                        });
                        
                        // Show dialog
                        var dialogResult = progressDialog.ShowDialog();
                        
                        if (progressDialog.InstallRequested)
                        {
                            // Get the downloaded file path
                            var downloadedFile = updateService.GetDownloadedFilePath();
                            
                            if (!string.IsNullOrEmpty(downloadedFile) && File.Exists(downloadedFile))
                            {
                                // Extract version from filename (e.g., "1.4.3.zip" -> "1.4.3")
                                var fileName = Path.GetFileNameWithoutExtension(downloadedFile);
                                
                                try
                                {
                                    // Path to the Install-Update.ps1 script
                                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                    var scriptPath = Path.Combine(baseDir, "Install-Update.ps1");
                                    
                                    if (!File.Exists(scriptPath))
                                    {
                                        MessageBox.Show(
                                            $"Install-Update.ps1 not found at:\n{scriptPath}\n\n" +
                                            "Please ensure the update script exists in the application directory.",
                                            "Update Script Missing",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        return;
                                    }
                                    
                                    var confirmResult = MessageBox.Show(
                                        $"The update installer will now run.\n\n" +
                                        $"Version: {fileName}\n\n" +
                                        "The installer will:\n" +
                                        "• Stop the SMTP Service if running\n" +
                                        "• Backup existing files\n" +
                                        "• Install the update\n" +
                                        "• Restart the service if it was running\n\n" +
                                        "A PowerShell window will open to show the installation progress.\n\n" +
                                        "Do you want to continue?",
                                        "Confirm Update Installation",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question);
                                    
                                    if (confirmResult == DialogResult.Yes)
                                    {
                                        Log.Information($"Launching update installer for version {fileName}");
                                        
                                        // Launch PowerShell with the Install-Update.ps1 script
                                        var process = new Process
                                        {
                                            StartInfo = new ProcessStartInfo
                                            {
                                                FileName = "powershell.exe",
                                                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                                                WorkingDirectory = baseDir,
                                                UseShellExecute = true,
                                                Verb = "runas" // Run as administrator
                                            }
                                        };
                                        
                                        process.Start();
                                        
                                        MessageBox.Show(
                                            "Update installer launched!\n\n" +
                                            "Follow the prompts in the PowerShell window.\n\n" +
                                            "The application will restart automatically if the service was running.",
                                            "Update In Progress",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                                    }
                                }
                                catch (System.ComponentModel.Win32Exception)
                                {
                                    MessageBox.Show(
                                        "Administrator privileges are required to install the update.\n\n" +
                                        "Please run the application as administrator and try again.",
                                        "Administrator Required",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Failed to launch update installer");
                                    MessageBox.Show(
                                        $"Failed to launch update installer:\n{ex.Message}\n\n" +
                                        $"You can manually run Install-Update.ps1 from:\n{AppDomain.CurrentDomain.BaseDirectory}",
                                        "Update Installation Failed",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"You have the latest version ({result.CurrentVersion})\n\n" +
                        "No updates are available at this time.",
                        "No Updates Available",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking for updates");
                MessageBox.Show(
                    $"An error occurred while checking for updates:\n{ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            try
            {
                Log.Information("Application exit requested from system tray");
                
                _trayIcon.Visible = false;
                
                // Dispose resources
                _trayIcon?.Dispose();
                _contextMenu?.Dispose();
                
                // Exit the application context
                ExitThread();
                
                // Force terminate the entire process to ensure complete shutdown
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application exit");
                // Force exit even if there's an error
                Environment.Exit(1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _contextMenu?.Dispose();
                
                // Clear the static instance
                if (Instance == this)
                {
                    Instance = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
