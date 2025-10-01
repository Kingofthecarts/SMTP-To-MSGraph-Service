using System.Windows.Forms;
using System.Diagnostics;

namespace SMTP_Service.UI
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon = null!;
        private ContextMenuStrip _contextMenu = null!;

        public TrayApplicationContext()
        {
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenuStrip();
            
            var configMenuItem = new ToolStripMenuItem("Configuration", null, ShowConfiguration);
            var statusMenuItem = new ToolStripMenuItem("Service Status", null, ShowStatus);
            var logsMenuItem = new ToolStripMenuItem("View Logs", null, ViewLogs);
            var separatorItem = new ToolStripSeparator();
            var startServiceMenuItem = new ToolStripMenuItem("Start Service", null, StartService);
            var stopServiceMenuItem = new ToolStripMenuItem("Stop Service", null, StopService);
            var restartServiceMenuItem = new ToolStripMenuItem("Restart Service", null, RestartService);
            var separator2Item = new ToolStripSeparator();
            var exitMenuItem = new ToolStripMenuItem("Exit", null, Exit);

            _contextMenu.Items.Add(configMenuItem);
            _contextMenu.Items.Add(statusMenuItem);
            _contextMenu.Items.Add(logsMenuItem);
            _contextMenu.Items.Add(separatorItem);
            _contextMenu.Items.Add(startServiceMenuItem);
            _contextMenu.Items.Add(stopServiceMenuItem);
            _contextMenu.Items.Add(restartServiceMenuItem);
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

        private void ShowConfiguration(object? sender, EventArgs e)
        {
            var configForm = new ConfigurationForm();
            configForm.ShowDialog();
        }

        private void ShowStatus(object? sender, EventArgs e)
        {
            try
            {
                var serviceName = "SMTP to Graph Relay";
                var service = System.ServiceProcess.ServiceController.GetServices()
                    .FirstOrDefault(s => s.ServiceName == serviceName);

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
                        "To install the service, run as administrator:\n" +
                        "sc create \"SMTP to Graph Relay\" binPath= \"<path-to-exe>\"",
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
                var logFilePath = config.LogSettings.LogFilePath;
                var logDirectory = Path.GetDirectoryName(logFilePath);
                
                if (string.IsNullOrEmpty(logDirectory))
                {
                    // Fallback to base directory + logs
                    logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
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
                        $"Expected log file: {logFilePath}\n\n" +
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
                var serviceName = "SMTP to Graph Relay";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"{command} \"{serviceName}\"",
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

        private void Exit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
