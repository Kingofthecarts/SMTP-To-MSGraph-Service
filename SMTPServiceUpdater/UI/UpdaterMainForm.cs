using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using SMTPServiceUpdater.Models;
using SMTPServiceUpdater.Services;

namespace SMTPServiceUpdater.UI
{
    /// <summary>
    /// Main form for the SMTP Service Updater GUI.
    /// Displays update progress and configuration settings in a tabbed interface.
    /// </summary>
    public class UpdaterMainForm : Form
    {
        // Fields
        private readonly bool _noRestart;
        private readonly string _rootPath;
        private bool _updateCompleted;
        private string? _downloadedVersion;
        private GitHubRelease? _availableRelease;

        // Controls - Update Progress Tab
        private TabControl? tabControl;
        private TabPage? progressTab;
        private TabPage? configTab;
        private RichTextBox? logTextBox;
        private Label? statusLabel;
        private ProgressBar? downloadProgressBar;
        private Label? progressLabel;
        private Button? downloadButton;
        private Button? installButton;
        private Button? cleanButton;
        private Button? closeButton;
        private LinkLabel? logFileLinkLabel;
        private string? _currentLogFilePath;

        // Controls - Configuration Tab
        private Panel? configPanel;
        private Label? configTitleLabel;

        /// <summary>
        /// Handles the log file link click event.
        /// Opens the log file in Notepad.
        /// </summary>
        private void OnLogFileLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentLogFilePath) || !System.IO.File.Exists(_currentLogFilePath))
            {
                MessageBox.Show("Log file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("notepad.exe", _currentLogFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates the download progress bar.
        /// Thread-safe - can be called from background threads.
        /// </summary>
        private void UpdateDownloadProgress(DownloadProgress progress)
        {
            if (downloadProgressBar == null || progressLabel == null) return;

            if (downloadProgressBar.InvokeRequired)
            {
                downloadProgressBar.Invoke(new Action(() => UpdateDownloadProgress(progress)));
                return;
            }

            downloadProgressBar.Visible = true;
            progressLabel.Visible = true;
            downloadProgressBar.Value = Math.Min(progress.PercentComplete, 100);
            progressLabel.Text = progress.Message;
        }

        /// <summary>
        /// Hides the download progress bar.
        /// </summary>
        private void HideDownloadProgress()
        {
            if (downloadProgressBar == null || progressLabel == null) return;

            if (downloadProgressBar.InvokeRequired)
            {
                downloadProgressBar.Invoke(new Action(HideDownloadProgress));
                return;
            }

            downloadProgressBar.Visible = false;
            downloadProgressBar.Value = 0;
            progressLabel.Visible = false;
            progressLabel.Text = "";
        }

        /// <summary>
        /// Initializes a new instance of the UpdaterMainForm.
        /// </summary>
        /// <param name="noRestart">If true, don't restart service after update</param>
        /// <param name="rootPath">Root path where SMTP Service is installed</param>
        public UpdaterMainForm(bool noRestart, string rootPath)
        {
            _noRestart = noRestart;
            _rootPath = rootPath;
            _updateCompleted = false;
            _downloadedVersion = null;
            _availableRelease = null;

            InitializeComponent();
        }

        /// <summary>
        /// Initializes all UI components and configures the form.
        /// </summary>
        private void InitializeComponent()
        {
            // Form properties
            this.Text = "SMTP Service Updater";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // Create TabControl
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Create Progress Tab
            progressTab = new TabPage("Update Progress");
            CreateProgressTab();
            tabControl.TabPages.Add(progressTab);

            // Create Configuration Tab
            configTab = new TabPage("Configuration");
            CreateConfigurationTab();
            tabControl.TabPages.Add(configTab);

            // Add TabControl to form
            this.Controls.Add(tabControl);

            // Wire up events
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
        }

        /// <summary>
        /// Creates the Update Progress tab with log display and control buttons.
        /// </summary>
        private void CreateProgressTab()
        {
            if (progressTab == null) return;

            // Create main panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Create status label
            statusLabel = new Label
            {
                Text = "Click 'Download' to check for updates from GitHub",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Create progress bar
            downloadProgressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 25,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            // Create progress label
            progressLabel = new Label
            {
                Text = "",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DarkGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            // Create log text box
            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Courier New", 9),
                BackColor = Color.White,
                Margin = new Padding(0, 5, 0, 5)
            };

            // Create button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Create log file link label
            logFileLinkLabel = new LinkLabel
            {
                Text = "View log file",
                AutoSize = true,
                Location = new Point(10, 45),
                Font = new Font("Segoe UI", 9),
                Visible = false
            };
            logFileLinkLabel.LinkClicked += OnLogFileLinkClicked;

            // Create Download button
            downloadButton = new Button
            {
                Text = "Download",
                Size = new Size(120, 35),
                Location = new Point(10, 5),
                Enabled = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            downloadButton.Click += OnDownloadButtonClick;

            // Create Install button
            installButton = new Button
            {
                Text = "Install",
                Size = new Size(120, 35),
                Location = new Point(140, 5),
                Enabled = false, // Enabled after download
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            installButton.Click += OnInstallButtonClick;

            // Create Clean button
            cleanButton = new Button
            {
                Text = "Clean Updates",
                Size = new Size(130, 35),
                Location = new Point(270, 5),
                Enabled = true,
                Font = new Font("Segoe UI", 9)
            };
            cleanButton.Click += OnCleanButtonClick;

            // Create Close button
            closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 35),
                Location = new Point(410, 5),
                Enabled = true,
                Font = new Font("Segoe UI", 10)
            };
            closeButton.Click += OnCloseButtonClick;

            // Add controls to button panel
            buttonPanel.Controls.Add(logFileLinkLabel);
            buttonPanel.Controls.Add(downloadButton);
            buttonPanel.Controls.Add(installButton);
            buttonPanel.Controls.Add(cleanButton);
            buttonPanel.Controls.Add(closeButton);

            // Add controls to main panel
            mainPanel.Controls.Add(logTextBox);
            mainPanel.Controls.Add(progressLabel);
            mainPanel.Controls.Add(downloadProgressBar);
            mainPanel.Controls.Add(statusLabel);
            mainPanel.Controls.Add(buttonPanel);

            // Add main panel to tab
            progressTab.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Creates the Configuration tab displaying current update settings.
        /// </summary>
        private void CreateConfigurationTab()
        {
            if (configTab == null) return;

            // Create scroll panel
            configPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // Create title label
            configTitleLabel = new Label
            {
                Text = "Current Update Configuration",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            configPanel.Controls.Add(configTitleLabel);

            // Add config panel to tab
            configTab.Controls.Add(configPanel);

            // Load configuration will be called in OnFormLoad
        }

        /// <summary>
        /// Loads and displays configuration settings from smtp-config.json.
        /// </summary>
        private void LoadConfiguration()
        {
            if (configPanel == null) return;

            try
            {
                string configPath = System.IO.Path.Combine(_rootPath, "Config");
                SmtpConfigRoot? config = UpdateSettingsReader.ReadFullConfig(configPath);

                int yPosition = 60;
                int lineHeight = 30;

                if (config?.UpdateSettings != null)
                {
                    var settings = config.UpdateSettings;

                    AddConfigLabel("Update Settings", yPosition, true);
                    yPosition += lineHeight;

                    AddConfigLabel($"Auto Download: {settings.AutoDownload}", yPosition);
                    yPosition += lineHeight;

                    AddConfigLabel($"Auto Install: {settings.AutoInstall}", yPosition);
                    yPosition += lineHeight;

                    AddConfigLabel($"Fully Automatic: {settings.IsFullyAutomatic}", yPosition);
                    yPosition += lineHeight + 10;
                }

                if (config?.AutoUpdateSettings != null)
                {
                    var autoSettings = config.AutoUpdateSettings;

                    AddConfigLabel("Auto-Update Settings", yPosition, true);
                    yPosition += lineHeight;

                    AddConfigLabel($"Auto Update Enabled: {autoSettings.AutoUpdateEnabled}", yPosition);
                    yPosition += lineHeight;

                    if (autoSettings.AutoUpdateEnabled)
                    {
                        AddConfigLabel($"Update Schedule:", yPosition, true);
                        yPosition += lineHeight;
                        
                        AddConfigLabel($"  Check Frequency: {autoSettings.CheckFrequencyText}", yPosition);
                        yPosition += lineHeight;

                        AddConfigLabel($"  Check Time: {autoSettings.CheckTime}", yPosition);
                        yPosition += lineHeight;

                        if (autoSettings.CheckFrequency == 1) // Weekly
                        {
                            AddConfigLabel($"  Weekly Check Day: {autoSettings.WeeklyCheckDayText}", yPosition);
                            yPosition += lineHeight;
                        }

                        AddConfigLabel($"  Check on Startup: {autoSettings.CheckOnStartup}", yPosition);
                        yPosition += lineHeight + 10;
                    }
                    else
                    {
                        AddConfigLabel($"  Scheduled updates are disabled", yPosition);
                        yPosition += lineHeight + 10;
                    }

                    AddConfigLabel("Update History", yPosition, true);
                    yPosition += lineHeight;

                    AddConfigLabel($"Last Check Date: {autoSettings.LastCheckDate ?? "Never"}", yPosition);
                    yPosition += lineHeight;

                    AddConfigLabel($"Last Update Date: {autoSettings.LastUpdateDate ?? "Never"}", yPosition);
                    yPosition += lineHeight;

                    AddConfigLabel($"Last Installed Version: {autoSettings.LastInstalledVersion ?? "None"}", yPosition);
                }
                else if (config?.UpdateSettings == null)
                {
                    AddConfigLabel("Configuration file not found or invalid", yPosition);
                }
            }
            catch (Exception ex)
            {
                if (configPanel != null)
                {
                    var errorLabel = new Label
                    {
                        Text = $"Error loading configuration: {ex.Message}",
                        AutoSize = true,
                        ForeColor = Color.Red,
                        Location = new Point(20, 60)
                    };
                    configPanel.Controls.Add(errorLabel);
                }
            }
        }

        /// <summary>
        /// Helper method to add a configuration label to the config panel.
        /// </summary>
        private void AddConfigLabel(string text, int yPosition, bool isHeader = false)
        {
            if (configPanel == null) return;

            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(20, yPosition),
                Font = isHeader 
                    ? new Font("Segoe UI", 10, FontStyle.Bold) 
                    : new Font("Segoe UI", 9),
                ForeColor = isHeader ? Color.DarkBlue : Color.Black
            };

            configPanel.Controls.Add(label);
        }

        /// <summary>
        /// Appends a log message to the log text box with color coding.
        /// Thread-safe - can be called from background threads.
        /// </summary>
        private void AppendLog(LogMessage message)
        {
            if (logTextBox == null) return;

            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            // Determine color based on log level
            Color color = message.Level switch
            {
                LogLevel.Info => Color.Black,
                LogLevel.Success => Color.Green,
                LogLevel.Warning => Color.DarkOrange,
                LogLevel.Error => Color.Red,
                LogLevel.Critical => Color.DarkRed,
                _ => Color.Black
            };

            // Determine font style
            FontStyle fontStyle = message.Level == LogLevel.Critical 
                ? FontStyle.Bold 
                : FontStyle.Regular;

            // Format timestamp
            string timestamp = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            string formattedMessage = $"[{timestamp}] [{message.Level}] {message.Message}\n";

            // Append to text box
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.SelectionColor = color;
            logTextBox.SelectionFont = new Font(logTextBox.Font, fontStyle);
            logTextBox.AppendText(formattedMessage);
            logTextBox.SelectionColor = logTextBox.ForeColor;

            // Auto-scroll to bottom
            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.ScrollToCaret();

            // Update status label
            if (statusLabel != null)
            {
                statusLabel.Text = message.Message;
                statusLabel.ForeColor = color;
            }
        }

        /// <summary>
        /// Downloads the latest update from GitHub.
        /// Called when the Download button is clicked.
        /// </summary>
        private async void DownloadUpdateAsync()
        {
            if (downloadButton != null)
            {
                downloadButton.Enabled = false;
            }

            if (closeButton != null)
            {
                closeButton.Enabled = false;
            }

            try
            {
                AppendLog(new LogMessage("Starting download check...", LogLevel.Info));

                // Create GitHub downloader
                var logger = new UpdateLogger(_rootPath);
                
                // Store log file path and show link
                _currentLogFilePath = logger.LogFilePath;
                if (logFileLinkLabel != null)
                {
                    logFileLinkLabel.Text = $"View log file: {System.IO.Path.GetFileName(_currentLogFilePath)}";
                    logFileLinkLabel.Visible = true;
                }
                
                // Subscribe to logger events to mirror logs in UI
                logger.LogMessageReceived += (sender, logMessage) =>
                {
                    AppendLog(logMessage);
                };
                
                var downloader = new GitHubDownloader(logger, _rootPath);

                // Check for updates
                GitHubRelease? release = await downloader.CheckForUpdateAsync();

                if (release == null)
                {
                    AppendLog(new LogMessage("No updates available or already running latest version", LogLevel.Info));
                    
                    // Get and display current version info
                    string currentVer = downloader.GetCurrentVersion();
                    AppendLog(new LogMessage($"Current installed version: {currentVer}", LogLevel.Info));
                    
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = true;
                    }

                    if (closeButton != null)
                    {
                        closeButton.Enabled = true;
                    }
                    return;
                }

                _availableRelease = release;
                AppendLog(new LogMessage($"Update available: {release.Version}", LogLevel.Success));
                AppendLog(new LogMessage($"GitHub tag: {release.TagName}", LogLevel.Info));

                // Create progress reporter - track milestone logging
                int lastLoggedPercent = 0;
                var progress = new Progress<DownloadProgress>(p =>
                {
                    UpdateDownloadProgress(p);
                    
                    // Only log if there's a message (milestone percentages only)
                    if (!string.IsNullOrEmpty(p.Message))
                    {
                        int currentPercent = p.PercentComplete;
                        if ((currentPercent >= 25 && lastLoggedPercent < 25) ||
                            (currentPercent >= 50 && lastLoggedPercent < 50) ||
                            (currentPercent >= 75 && lastLoggedPercent < 75) ||
                            (currentPercent >= 100 && lastLoggedPercent < 100))
                        {
                            lastLoggedPercent = currentPercent;
                            AppendLog(new LogMessage(p.Message, LogLevel.Info));
                        }
                    }
                });

                // Download update
                string? downloadedPath = await downloader.DownloadUpdateAsync(release, progress);

                if (downloadedPath != null)
                {
                    HideDownloadProgress();
                    _downloadedVersion = release.Version;
                    AppendLog(new LogMessage($"Downloaded version: {_downloadedVersion}", LogLevel.Info));
                    AppendLog(new LogMessage($"Saved as: {System.IO.Path.GetFileName(downloadedPath)}", LogLevel.Info));
                    
                    // Verify the file exists and matches expected naming
                    string expectedPath = System.IO.Path.Combine(_rootPath, "updates", $"{_downloadedVersion}.zip");
                    if (downloadedPath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        AppendLog(new LogMessage("File location verified - ready to install", LogLevel.Success));
                    }
                    else
                    {
                        AppendLog(new LogMessage($"WARNING: Downloaded path mismatch", LogLevel.Warning));
                        AppendLog(new LogMessage($"Expected: {expectedPath}", LogLevel.Warning));
                        AppendLog(new LogMessage($"Actual: {downloadedPath}", LogLevel.Warning));
                    }
                    
                    if (statusLabel != null)
                    {
                        statusLabel.Text = $"Ready to install version {_downloadedVersion}";
                        statusLabel.ForeColor = Color.Green;
                    }

                    if (installButton != null)
                    {
                        installButton.Enabled = true;
                    }

                    if (closeButton != null)
                    {
                        closeButton.Enabled = true;
                    }
                }
                else
                {
                    HideDownloadProgress();
                    AppendLog(new LogMessage("Download failed - check log for details", LogLevel.Error));
                    
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = true;
                    }

                    if (closeButton != null)
                    {
                        closeButton.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                HideDownloadProgress();
                AppendLog(new LogMessage($"Download error: {ex.Message}", LogLevel.Critical));
                
                if (downloadButton != null)
                {
                    downloadButton.Enabled = true;
                }

                if (closeButton != null)
                {
                    closeButton.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Installs the downloaded update.
        /// Called when the Install button is clicked.
        /// </summary>
        private async void InstallUpdateAsync()
        {
            if (installButton != null)
            {
                installButton.Enabled = false;
            }

            if (downloadButton != null)
            {
                downloadButton.Enabled = false;
            }

            if (closeButton != null)
            {
                closeButton.Enabled = false;
            }

            try
            {
                // Verify we have a version to install
                if (string.IsNullOrWhiteSpace(_downloadedVersion))
                {
                    AppendLog(new LogMessage("ERROR: No version available for installation", LogLevel.Error));
                    AppendLog(new LogMessage("Please click Download first", LogLevel.Warning));
                    
                    if (installButton != null)
                    {
                        installButton.Enabled = false;
                    }
                    
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = true;
                    }

                    if (closeButton != null)
                    {
                        closeButton.Enabled = true;
                    }
                    
                    return;
                }

                // Verify the ZIP file exists before starting installation
                string zipPath = System.IO.Path.Combine(_rootPath, "updates", $"{_downloadedVersion}.zip");
                if (!System.IO.File.Exists(zipPath))
                {
                    AppendLog(new LogMessage($"ERROR: Update file not found: {_downloadedVersion}.zip", LogLevel.Error));
                    AppendLog(new LogMessage("The downloaded file may have been moved or deleted", LogLevel.Warning));
                    AppendLog(new LogMessage("Please download the update again", LogLevel.Warning));
                    
                    if (installButton != null)
                    {
                        installButton.Enabled = false;
                    }
                    
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = true;
                    }

                    if (closeButton != null)
                    {
                        closeButton.Enabled = true;
                    }
                    
                    _downloadedVersion = null;
                    return;
                }

                AppendLog(new LogMessage($"Installing version: {_downloadedVersion}", LogLevel.Info));
                AppendLog(new LogMessage($"Update package: {zipPath}", LogLevel.Info));

                // Create progress reporter
                var progress = new Progress<LogMessage>(AppendLog);

                // Create installer and run update
                var installer = new UpdateInstaller(_rootPath, progress);
                var result = await installer.RunAsync(_downloadedVersion, _noRestart);

                // Update complete
                _updateCompleted = true;

                if (closeButton != null)
                {
                    closeButton.Enabled = true;
                }

                if (statusLabel != null)
                {
                    statusLabel.Text = result.Success 
                        ? "Update completed successfully!" 
                        : "Update completed with errors - check log for details";
                    statusLabel.ForeColor = result.Success ? Color.Green : Color.Red;
                }
            }
            catch (Exception ex)
            {
                AppendLog(new LogMessage($"Fatal error: {ex.Message}", LogLevel.Critical));

                if (closeButton != null)
                {
                    closeButton.Enabled = true;
                }

                if (statusLabel != null)
                {
                    statusLabel.Text = "Update failed - see log for details";
                    statusLabel.ForeColor = Color.Red;
                }

                _updateCompleted = true;
            }
        }

        /// <summary>
        /// Handles the form load event.
        /// Loads configuration - does NOT auto-start download.
        /// </summary>
        private void OnFormLoad(object? sender, EventArgs e)
        {
            // Load configuration display
            LoadConfiguration();

            // Set initial UI state - do NOT auto-start
            if (downloadButton != null)
            {
                downloadButton.Enabled = true;
            }

            if (installButton != null)
            {
                installButton.Enabled = false;
            }

            if (closeButton != null)
            {
                closeButton.Enabled = true;
            }

            // Display initial message in log
            AppendLog(new LogMessage("Ready to check for updates", LogLevel.Info));
            AppendLog(new LogMessage("Click 'Download' to check GitHub for the latest release", LogLevel.Info));
        }

        /// <summary>
        /// Handles the Download button click event.
        /// </summary>
        private void OnDownloadButtonClick(object? sender, EventArgs e)
        {
            DownloadUpdateAsync();
        }

        /// <summary>
        /// Handles the Install button click event.
        /// </summary>
        private void OnInstallButtonClick(object? sender, EventArgs e)
        {
            InstallUpdateAsync();
        }

        /// <summary>
        /// Handles the Clean Updates button click event.
        /// Purges all files in the updates folder.
        /// </summary>
        private void OnCleanButtonClick(object? sender, EventArgs e)
        {
            try
            {
                string updatesDir = System.IO.Path.Combine(_rootPath, "updates");
                
                if (!System.IO.Directory.Exists(updatesDir))
                {
                    AppendLog(new LogMessage("Updates folder does not exist - nothing to clean", LogLevel.Info));
                    return;
                }

                var files = System.IO.Directory.GetFiles(updatesDir);
                
                if (files.Length == 0)
                {
                    AppendLog(new LogMessage("Updates folder is already empty", LogLevel.Info));
                    return;
                }

                var result = MessageBox.Show(
                    $"This will delete {files.Length} file(s) from the updates folder.\n\nAre you sure?",
                    "Confirm Clean Updates",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    int deletedCount = 0;
                    int errorCount = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            deletedCount++;
                            AppendLog(new LogMessage($"Deleted: {System.IO.Path.GetFileName(file)}", LogLevel.Info));
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            AppendLog(new LogMessage($"Failed to delete {System.IO.Path.GetFileName(file)}: {ex.Message}", LogLevel.Error));
                        }
                    }

                    if (errorCount == 0)
                    {
                        AppendLog(new LogMessage($"Successfully cleaned updates folder - {deletedCount} file(s) deleted", LogLevel.Success));
                    }
                    else
                    {
                        AppendLog(new LogMessage($"Cleaned with errors - {deletedCount} deleted, {errorCount} failed", LogLevel.Warning));
                    }

                    // Clear downloaded version since we just deleted it
                    _downloadedVersion = null;
                    if (installButton != null)
                    {
                        installButton.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog(new LogMessage($"Error cleaning updates folder: {ex.Message}", LogLevel.Error));
            }
        }

        /// <summary>
        /// Handles the Close button click event.
        /// </summary>
        private void OnCloseButtonClick(object? sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the form closing event.
        /// Confirms if user tries to close during download or install.
        /// </summary>
        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            bool isProcessing = (downloadButton?.Enabled == false && !_updateCompleted) ||
                               (installButton?.Enabled == false && !_updateCompleted);

            if (isProcessing)
            {
                var result = MessageBox.Show(
                    "Update is in progress. Are you sure you want to cancel?",
                    "Confirm Cancel",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
