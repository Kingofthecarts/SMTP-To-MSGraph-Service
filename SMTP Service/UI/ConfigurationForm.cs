using EmailService.Flow;
using Microsoft.Extensions.Logging;
using Serilog;
using SMTP_Service.Helpers;
using SMTP_Service.Models;
using System.Diagnostics;
using System.Windows.Forms;

namespace SMTP_Service.UI
{
    public partial class ConfigurationForm : Form
    {
        private readonly Managers.ConfigurationManager _configManager;
        private AppConfig _config;
        private SmtpConfiguration _smtpConfig;
        private UserConfiguration _userConfig;
        private GraphConfiguration _graphConfig;

        // SMTP Settings Controls
        private TextBox txtSmtpPort = null!;
        private ComboBox cmbBindAddress = null!;
        private CheckBox chkRequireAuth = null!;
        private NumericUpDown numMaxMessageSizeKb = null!;
        
        // Flow Control
        private Button btnToggleFlow = null!;
        private Label lblFlowStatus = null!;
        private NumericUpDown numSendDelay = null!;
        private bool _isFlowing = true;
        private bool _isUiOnlyMode = false;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnAddUser = null!;
        private ListBox lstUsers = null!;
        private Button btnRemoveUser = null!;

        // Graph Settings Controls
        private TextBox txtTenantId = null!;
        private TextBox txtClientId = null!;
        private TextBox txtClientSecret = null!;
        private TextBox txtSenderEmail = null!;

        // Queue Settings Controls
        private NumericUpDown numMaxRetry = null!;
        private NumericUpDown numRetryDelay = null!;

        // Application Settings Controls
        private ComboBox cmbRunMode = null!;
        private TextBox txtLogLocation = null!;
        private Button btnBrowseLog = null!;
        private Button btnOpenLogs = null!;

        // Update Settings Controls
        private CheckBox chkAutoUpdateEnabled = null!;
        private ComboBox cmbCheckFrequency = null!;
        private DateTimePicker dtpCheckTime = null!;
        private ComboBox cmbWeeklyCheckDay = null!;
        private Label lblWeeklyDay = null!;
        private CheckBox chkAutoDownload = null!;
        private CheckBox chkAutoInstall = null!;
        private CheckBox chkCheckOnStartup = null!;
        private Label lblLastCheckDate = null!;
        private Label lblLastUpdateDate = null!;
        private Label lblLastInstalledVersion = null!;

        // Test Email Controls
        private TextBox txtTestTo = null!;
        private TextBox txtTestSubject = null!;
        private TextBox txtTestBody = null!;
        private CheckBox chkTestHtml = null!;
        private Button btnSendTest = null!;
        private Button btnBrowseAttachment = null!;
        private Button btnClearAttachment = null!;
        private Label lblAttachmentInfo = null!;
        private string? _attachmentFilePath = null;

        // Statistics Controls
        private Label lblTotalSuccess = null!;
        private Label lblTotalFailed = null!;
        private Label lblLastSuccess = null!;
        private Label lblLastFailure = null!;
        private Label lblQueueCount = null!;
        private Label lblMemoryUsage = null!;
        private Label lblCpuUsage = null!;
        private Label lblUptime = null!;
        private DataGridView dgvUserStats = null!;
        private Button btnRefreshStats = null!;
        private Button btnResetStats = null!;
        private System.Windows.Forms.Timer? _cpuRefreshTimer = null!;
        private System.Windows.Forms.Timer? _memoryRefreshTimer = null!;
        private System.Windows.Forms.Timer? _statsRefreshTimer = null!;
        private System.Windows.Forms.Timer? _statsLoggingTimer = null!;
        
        // Track min/max for CPU and Memory
        private double _minCpuUsage = double.MaxValue;
        private double _maxCpuUsage = double.MinValue;
        private double _minMemoryMB = double.MaxValue;
        private double _maxMemoryMB = double.MinValue;
        private ToolTip _statsToolTip = null!;

        // Buttons
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Button btnTest = null!;
        private Button btnExit = null!;

        // Track changes
        private bool _hasUnsavedChanges = false;

        // Store actual values separately from displayed values
        private string _actualTenantId = string.Empty;
        private string _actualClientId = string.Empty;

        public ConfigurationForm()
        {
            _configManager = new Managers.ConfigurationManager();
            _config = _configManager.LoadConfiguration();
            _smtpConfig = _configManager.LoadSmtpConfiguration();
            _userConfig = _configManager.LoadUserConfiguration();
            _graphConfig = _configManager.LoadGraphConfiguration();
            
            // Initialize tooltip for stats
            _statsToolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100
            };
            
            // Detect if running in UI-only mode
            _isUiOnlyMode = ServiceMutexManager.IsServiceRunning();
            
            InitializeComponents();
            LoadConfiguration();
            
            // Handle form closing to cleanup timer
            this.FormClosing += ConfigurationForm_FormClosing;
        }

        private void ConfigurationForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Stop and dispose all timers
            if (_cpuRefreshTimer != null)
            {
                _cpuRefreshTimer.Stop();
                _cpuRefreshTimer.Dispose();
                _cpuRefreshTimer = null;
            }
            
            if (_memoryRefreshTimer != null)
            {
                _memoryRefreshTimer.Stop();
                _memoryRefreshTimer.Dispose();
                _memoryRefreshTimer = null;
            }
            
            if (_statsRefreshTimer != null)
            {
                _statsRefreshTimer.Stop();
                _statsRefreshTimer.Dispose();
                _statsRefreshTimer = null;
            }
            
            if (_statsLoggingTimer != null)
            {
                _statsLoggingTimer.Stop();
                _statsLoggingTimer.Dispose();
                _statsLoggingTimer = null;
            }
            
            // Dispose tooltip
            _statsToolTip?.Dispose();
        }

        // Helper method to mask GUID after third dash
        private string MaskGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return guid;

            // Count dashes to find the third one
            int dashCount = 0;
            int thirdDashIndex = -1;
            
            for (int i = 0; i < guid.Length; i++)
            {
                if (guid[i] == '-')
                {
                    dashCount++;
                    if (dashCount == 3)
                    {
                        thirdDashIndex = i;
                        break;
                    }
                }
            }

            // If we found the third dash, mask everything after it
            if (thirdDashIndex > 0 && thirdDashIndex < guid.Length - 1)
            {
                return guid.Substring(0, thirdDashIndex + 1) + new string('*', guid.Length - thirdDashIndex - 1);
            }

            return guid;
        }

        // Helper method to check if a string is already masked
        private bool IsMasked(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Contains('*');
        }

        private void InitializeComponents()
        {
            this.Text = $"SMTP to Graph Relay - Configuration v{VersionHelper.GetVersion()}";
            this.Size = new System.Drawing.Size(620, 780);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // SMTP Settings Tab
            var smtpTab = new TabPage("SMTP Settings");
            InitializeSmtpTab(smtpTab);
            tabControl.TabPages.Add(smtpTab);

            // Users Tab
            var usersTab = new TabPage("Users");
            InitializeUsersTab(usersTab);
            tabControl.TabPages.Add(usersTab);

            // Graph Settings Tab
            var graphTab = new TabPage("MS Graph Settings");
            InitializeGraphTab(graphTab);
            tabControl.TabPages.Add(graphTab);

            // Queue Settings Tab
            var queueTab = new TabPage("Application Settings");
            InitializeQueueTab(queueTab);
            tabControl.TabPages.Add(queueTab);

            // Test Email Tab
            var testEmailTab = new TabPage("Test Email");
            InitializeTestEmailTab(testEmailTab);
            tabControl.TabPages.Add(testEmailTab);

            // Statistics Tab
            var statsTab = new TabPage("Statistics");
            InitializeStatisticsTab(statsTab);
            tabControl.TabPages.Add(statsTab);

            // Changelog Tab
            var changelogTab = new TabPage("Changelog");
            InitializeChangelogTab(changelogTab);
            tabControl.TabPages.Add(changelogTab);

            this.Controls.Add(tabControl);

            // Bottom panel with buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            btnSave = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(250, 10),
                Size = new System.Drawing.Size(80, 30),
                Enabled = false // Disabled until changes are made
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(340, 10),
                Size = new System.Drawing.Size(80, 30)
            };
            btnCancel.Click += BtnCancel_Click;

            btnExit = new Button
            {
                Text = "Exit Application",
                Location = new System.Drawing.Point(430, 10),
                Size = new System.Drawing.Size(120, 30)
            };
            btnExit.Click += BtnExit_Click;

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnExit);

            this.Controls.Add(bottomPanel);
        }

        private void InitializeSmtpTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // === FLOW CONTROL SECTION ===
            var lblFlowHeader = new Label 
            { 
                Text = "SMTP Flow Control:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblFlowHeader);

            y += 30;

            btnToggleFlow = new Button
            {
                Text = "Halt SMTP",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(120, 30)
            };
            btnToggleFlow.Click += BtnToggleFlow_Click;
            tab.Controls.Add(btnToggleFlow);

            lblFlowStatus = new Label
            {
                Text = "Status: FLOWING",
                Location = new System.Drawing.Point(150, y + 5),
                Size = new System.Drawing.Size(200, 20),
                ForeColor = System.Drawing.Color.DarkGreen,
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblFlowStatus);

            y += 40;

            var lblSendDelay = new Label 
            { 
                Text = "Send Delay (ms):", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(120, 20) 
            };
            numSendDelay = new NumericUpDown
            {
                Location = new System.Drawing.Point(150, y),
                Size = new System.Drawing.Size(100, 20),
                Minimum = 100,
                Maximum = 10000,
                Value = 1000,
                Increment = 100
            };
            tab.Controls.Add(lblSendDelay);
            tab.Controls.Add(numSendDelay);

            y += 30;

            var lblFlowInfo = new Label
            {
                Text = "Flow Control: Halt stops accepting new SMTP connections. Messages are queued and sent when resumed.\n" +
                       "Send Delay: Milliseconds to wait between each email send (prevents overwhelming recipients).",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 40),
                AutoSize = false,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblFlowInfo);

            y += 50;

            // Port
            var lblPort = new Label { Text = "SMTP Port:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtSmtpPort = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(100, 20) };
            tab.Controls.Add(lblPort);
            tab.Controls.Add(txtSmtpPort);

            y += 40;

            // Bind Address
            var lblBindAddress = new Label { Text = "Bind Address:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            cmbBindAddress = new ComboBox 
            { 
                Location = new System.Drawing.Point(130, y), 
                Size = new System.Drawing.Size(300, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBindAddress.Items.Add("0.0.0.0 - All Interfaces (Default)");
            cmbBindAddress.Items.Add("127.0.0.1 - Localhost Only");
            cmbBindAddress.SelectedIndex = 0;
            tab.Controls.Add(lblBindAddress);
            tab.Controls.Add(cmbBindAddress);

            y += 40;

            // Max Message Size
            var lblMaxSize = new Label { Text = "Max Message Size (MB):", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(150, 20) };
            numMaxMessageSizeKb = new NumericUpDown 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(120, 20),
                Minimum = 1,
                Maximum = 100,
                Value = 50,
                Increment = 1,
                DecimalPlaces = 0
            };
            tab.Controls.Add(lblMaxSize);
            tab.Controls.Add(numMaxMessageSizeKb);

            y += 40;

            // Require Authentication
            chkRequireAuth = new CheckBox 
            { 
                Text = "Require Authentication", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20) 
            };
            tab.Controls.Add(chkRequireAuth);

            y += 40;

            // Info text
            var lblInfo = new Label
            {
                Text = "Port Configuration:\n" +
                       "• Default SMTP port is 25 for standard SMTP relay\n\n" +
                       "Bind Address Configuration:\n" +
                       "• 0.0.0.0 - Listen on ALL network interfaces (allows remote connections)\n" +
                       "• 127.0.0.1 - Listen on localhost ONLY (local connections only, more secure)\n" +
                       "• Requires service restart to take effect\n\n" +
                       "Message Size Limit:\n" +
                       "• Default is 50 MB. Range: 1 MB to 100 MB\n" +
                       "• Messages exceeding this limit will be rejected\n\n" +
                       "Authentication Modes:\n" +
                       "• When ENABLED: Authentication is REQUIRED - only authorized users can send emails\n" +
                       "• When DISABLED: Authentication is OPTIONAL - both authenticated and unauthenticated connections are allowed\n\n" +
                       "Note: Configure authorized users in the Users tab.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 240),
                AutoSize = false,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInfo);
        }

        private void InitializeUsersTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // Header
            var lblHeader = new Label
            {
                Text = "Authorized SMTP Users",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 25),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 10, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblHeader);

            y += 35;

            // Info
            var lblInfo = new Label
            {
                Text = "Manage users who can authenticate and send emails through the SMTP relay.\n" +
                       "Note: Authentication must be enabled in SMTP Settings for user credentials to be required.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 40),
                AutoSize = false,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInfo);

            y += 50;

            // User List
            var lblUsers = new Label { Text = "Current Authorized Users:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(200, 20) };
            tab.Controls.Add(lblUsers);

            y += 30;

            lstUsers = new ListBox 
            { 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(400, 200) 
            };
            tab.Controls.Add(lstUsers);

            btnRemoveUser = new Button
            {
                Text = "Remove Selected",
                Location = new System.Drawing.Point(430, y),
                Size = new System.Drawing.Size(120, 30)
            };
            btnRemoveUser.Click += BtnRemoveUser_Click;
            tab.Controls.Add(btnRemoveUser);

            y += 220;

            // Add new user section
            var lblAddHeader = new Label
            {
                Text = "Add New User",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblAddHeader);

            y += 30;

            var lblUsername = new Label { Text = "Username:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtUsername = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(250, 20) };
            tab.Controls.Add(lblUsername);
            tab.Controls.Add(txtUsername);

            y += 30;

            var lblPassword = new Label { Text = "Password:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtPassword = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(250, 20), UseSystemPasswordChar = true };
            tab.Controls.Add(lblPassword);
            tab.Controls.Add(txtPassword);

            btnAddUser = new Button
            {
                Text = "Add User",
                Location = new System.Drawing.Point(390, y - 15),
                Size = new System.Drawing.Size(100, 30)
            };
            btnAddUser.Click += BtnAddUser_Click;
            tab.Controls.Add(btnAddUser);
        }

        private void InitializeGraphTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // Tenant ID
            var lblTenant = new Label { Text = "Tenant ID:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtTenantId = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(400, 20) };
            tab.Controls.Add(lblTenant);
            tab.Controls.Add(txtTenantId);

            y += 40;

            // Client ID
            var lblClient = new Label { Text = "Client ID:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtClientId = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(400, 20) };
            tab.Controls.Add(lblClient);
            tab.Controls.Add(txtClientId);

            y += 40;

            // Client Secret
            var lblSecret = new Label { Text = "Client Secret:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtClientSecret = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(400, 20), UseSystemPasswordChar = true };
            tab.Controls.Add(lblSecret);
            tab.Controls.Add(txtClientSecret);

            y += 40;

            // Sender Email
            var lblSender = new Label { Text = "Sender Email:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtSenderEmail = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(400, 20) };
            tab.Controls.Add(lblSender);
            tab.Controls.Add(txtSenderEmail);

            y += 60;

            // Test Connection Button
            btnTest = new Button
            {
                Text = "Test Connection",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(130, 35)
            };
            btnTest.Click += BtnTest_Click;
            tab.Controls.Add(btnTest);

            y += 50;

            // Instructions with clickable link
            var lblInstructions = new Label
            {
                Text = "To configure MS Graph:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                AutoSize = false,
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblInstructions);

            y += 25;

            // Step 1 with clickable link
            var lblStep1 = new Label
            {
                Text = "1. Register an app in Azure AD at:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(183, 20),
                AutoSize = false
            };
            tab.Controls.Add(lblStep1);

            var linkEntra = new LinkLabel
            {
                Text = "https://entra.microsoft.com/",
                Location = new System.Drawing.Point(203, y),
                Size = new System.Drawing.Size(250, 20),
                AutoSize = true
            };
            linkEntra.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://entra.microsoft.com/",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            tab.Controls.Add(linkEntra);

            y += 25;

            var lblStep2 = new Label
            {
                Text = "2. Grant Mail.Send permission",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                AutoSize = false
            };
            tab.Controls.Add(lblStep2);

            y += 25;

            var lblStep3 = new Label
            {
                Text = "3. Create a client secret",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                AutoSize = false
            };
            tab.Controls.Add(lblStep3);

            y += 25;

            var lblStep4 = new Label
            {
                Text = "4. Copy Tenant ID, Client ID, and Secret here",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                AutoSize = false
            };
            tab.Controls.Add(lblStep4);

            y += 30;

            var lblTestNote = new Label
            {
                Text = "Use 'Test Connection' above to verify your configuration.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                AutoSize = false,
                ForeColor = System.Drawing.Color.DarkBlue,
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Italic)
            };
            tab.Controls.Add(lblTestNote);
        }

        private void InitializeQueueTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // Run Mode Section
            var lblRunModeHeader = new Label 
            { 
                Text = "Application Run Mode:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblRunModeHeader);

            y += 30;

            var lblRunMode = new Label 
            { 
                Text = "Default Run Mode:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(150, 20) 
            };
            cmbRunMode = new ComboBox 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRunMode.Items.Add("Service Mode (no UI)");
            cmbRunMode.Items.Add("Console with Tray");
            cmbRunMode.Items.Add("Tray Only");
            cmbRunMode.SelectedIndex = 0;
            
            tab.Controls.Add(lblRunMode);
            tab.Controls.Add(cmbRunMode);

            y += 30;

            var lblRunModeInfo = new Label
            {
                Text = "This setting determines how the application runs when started without command line arguments.\n" +
                       "• Service Mode: Pure background service (no console, no tray) - for production\n" +
                       "• Console with Tray: Shows console + system tray (can close console from tray)\n" +
                       "• Tray Only: System tray icon only, no console (clean interface)\n\n" +
                       "Note: If service is running, launching app again shows UI-only mode for configuration.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(540, 110),
                AutoSize = false,
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblRunModeInfo);

            y += 120;

            // File Locations Button
            var btnShowPaths = new Button
            {
                Text = "Show File Locations",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(150, 30)
            };
            btnShowPaths.Click += BtnShowPaths_Click;
            tab.Controls.Add(btnShowPaths);

            // Remove Service Button
            var btnRemoveService = new Button
            {
                Text = "Remove Service",
                Location = new System.Drawing.Point(180, y),
                Size = new System.Drawing.Size(150, 30)
            };
            btnRemoveService.Click += BtnRemoveService_Click;
            tab.Controls.Add(btnRemoveService);

            y += 50;

            // Queue Settings Section
            var lblQueueHeader = new Label 
            { 
                Text = "Email Queue Settings:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblQueueHeader);

            y += 30;

            // Max Retry Attempts
            var lblRetry = new Label { Text = "Max Retry Attempts:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(150, 20) };
            numMaxRetry = new NumericUpDown 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(100, 20),
                Minimum = 0,
                Maximum = 10,
                Value = 3
            };
            tab.Controls.Add(lblRetry);
            tab.Controls.Add(numMaxRetry);

            y += 40;

            // Retry Delay
            var lblDelay = new Label { Text = "Retry Delay (minutes):", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(150, 20) };
            numRetryDelay = new NumericUpDown 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(100, 20),
                Minimum = 1,
                Maximum = 60,
                Value = 5
            };
            tab.Controls.Add(lblDelay);
            tab.Controls.Add(numRetryDelay);

            y += 50;

            // Log Location Section
            var lblLogHeader = new Label 
            { 
                Text = "Log Location:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblLogHeader);

            y += 30;

            var lblLogPath = new Label { Text = "Log Directory:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtLogLocation = new TextBox 
            { 
                Location = new System.Drawing.Point(130, y), 
                Size = new System.Drawing.Size(300, 20),
                ReadOnly = true
            };
            btnBrowseLog = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(440, y - 2),
                Size = new System.Drawing.Size(80, 24)
            };
            btnBrowseLog.Click += BtnBrowseLog_Click;
            
            tab.Controls.Add(lblLogPath);
            tab.Controls.Add(txtLogLocation);
            tab.Controls.Add(btnBrowseLog);

            y += 35;

            btnOpenLogs = new Button
            {
                Text = "Open Logs Folder",
                Location = new System.Drawing.Point(130, y),
                Size = new System.Drawing.Size(130, 30)
            };
            btnOpenLogs.Click += BtnOpenLogs_Click;
            tab.Controls.Add(btnOpenLogs);

            y += 50;

            // Update Settings Section
            var lblUpdateHeader = new Label 
            { 
                Text = "Auto-Update Settings:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblUpdateHeader);

            y += 30;

            // Enable Auto-Update
            chkAutoUpdateEnabled = new CheckBox 
            { 
                Text = "Enable Automatic Updates", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20) 
            };
            chkAutoUpdateEnabled.CheckedChanged += ChkAutoUpdateEnabled_CheckedChanged;
            tab.Controls.Add(chkAutoUpdateEnabled);

            y += 30;

            // Check Frequency
            var lblFrequency = new Label 
            { 
                Text = "Check Frequency:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(150, 20) 
            };
            cmbCheckFrequency = new ComboBox 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(150, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCheckFrequency.Items.Add("Daily");
            cmbCheckFrequency.Items.Add("Weekly");
            cmbCheckFrequency.SelectedIndex = 0;
            cmbCheckFrequency.SelectedIndexChanged += CmbCheckFrequency_SelectedIndexChanged;
            tab.Controls.Add(lblFrequency);
            tab.Controls.Add(cmbCheckFrequency);

            y += 30;

            // Check Time
            var lblCheckTime = new Label 
            { 
                Text = "Check Time:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(150, 20) 
            };
            dtpCheckTime = new DateTimePicker 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(150, 20),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(2) // Default to 2 AM
            };
            tab.Controls.Add(lblCheckTime);
            tab.Controls.Add(dtpCheckTime);

            y += 30;

            // Weekly Check Day (initially hidden)
            lblWeeklyDay = new Label 
            { 
                Text = "Check on Day:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(150, 20),
                Visible = false
            };
            cmbWeeklyCheckDay = new ComboBox 
            { 
                Location = new System.Drawing.Point(180, y), 
                Size = new System.Drawing.Size(150, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            cmbWeeklyCheckDay.Items.AddRange(new object[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
            cmbWeeklyCheckDay.SelectedIndex = 0;
            tab.Controls.Add(lblWeeklyDay);
            tab.Controls.Add(cmbWeeklyCheckDay);

            y += 30;

            // Auto Download
            chkAutoDownload = new CheckBox 
            { 
                Text = "Automatically download updates", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(250, 20) 
            };
            chkAutoDownload.CheckedChanged += ChkAutoDownload_CheckedChanged;
            tab.Controls.Add(chkAutoDownload);

            y += 30;

            // Auto Install
            chkAutoInstall = new CheckBox 
            { 
                Text = "Automatically install updates (requires auto-download)", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(350, 20),
                Enabled = false
            };
            tab.Controls.Add(chkAutoInstall);

            y += 30;

            // Check On Startup
            chkCheckOnStartup = new CheckBox 
            { 
                Text = "Check for updates on startup", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(250, 20) 
            };
            tab.Controls.Add(chkCheckOnStartup);

            y += 40;

            // Update Status Info
            var lblUpdateInfo = new Label
            {
                Text = "Update Status:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblUpdateInfo);

            y += 25;

            lblLastCheckDate = new Label
            {
                Text = "Last Check: Never",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblLastCheckDate);

            y += 25;

            lblLastUpdateDate = new Label
            {
                Text = "Last Download: Never",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblLastUpdateDate);

            y += 25;

            lblLastInstalledVersion = new Label
            {
                Text = "Last Installed Version: None",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 20),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblLastInstalledVersion);
        }

        private void InitializeTestEmailTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // Instructions
            var lblInstructions = new Label
            {
                Text = "Send a test email through MS Graph to verify your configuration.\n" +
                       "This simulates receiving an email on port 25 and relaying it via MS Graph.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 40),
                AutoSize = false
            };
            tab.Controls.Add(lblInstructions);

            y += 50;

            // To Address
            var lblTo = new Label { Text = "To (Email Address):", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(120, 20) };
            txtTestTo = new TextBox { Location = new System.Drawing.Point(150, y), Size = new System.Drawing.Size(380, 20) };
            tab.Controls.Add(lblTo);
            tab.Controls.Add(txtTestTo);

            y += 40;

            // Subject
            var lblSubject = new Label { Text = "Subject:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(120, 20) };
            txtTestSubject = new TextBox 
            { 
                Location = new System.Drawing.Point(150, y), 
                Size = new System.Drawing.Size(380, 20),
                Text = "Test Email from SMTP Relay Service"
            };
            tab.Controls.Add(lblSubject);
            tab.Controls.Add(txtTestSubject);

            y += 40;

            // Body
            var lblBody = new Label { Text = "Message Body:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(120, 20) };
            txtTestBody = new TextBox 
            { 
                Location = new System.Drawing.Point(150, y), 
                Size = new System.Drawing.Size(380, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "This is a test email sent from the SMTP to MS Graph Relay Service.\n\n" +
                       "If you receive this email, your configuration is working correctly!\n\n" +
                       $"Sent at: {DateTime.Now}"
            };
            tab.Controls.Add(lblBody);
            tab.Controls.Add(txtTestBody);

            y += 210;

            // HTML Checkbox
            chkTestHtml = new CheckBox 
            { 
                Text = "Send as HTML", 
                Location = new System.Drawing.Point(150, y), 
                Size = new System.Drawing.Size(150, 20),
                Checked = false
            };
            tab.Controls.Add(chkTestHtml);

            y += 40;

            // Attachment Section
            var lblAttachment = new Label 
            { 
                Text = "Attachment (optional):", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(120, 20) 
            };
            tab.Controls.Add(lblAttachment);

            btnBrowseAttachment = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(150, y - 2),
                Size = new System.Drawing.Size(100, 24)
            };
            btnBrowseAttachment.Click += BtnBrowseAttachment_Click;
            tab.Controls.Add(btnBrowseAttachment);

            btnClearAttachment = new Button
            {
                Text = "Clear",
                Location = new System.Drawing.Point(260, y - 2),
                Size = new System.Drawing.Size(70, 24),
                Enabled = false
            };
            btnClearAttachment.Click += BtnClearAttachment_Click;
            tab.Controls.Add(btnClearAttachment);

            y += 30;

            lblAttachmentInfo = new Label
            {
                Text = "No file selected",
                Location = new System.Drawing.Point(150, y),
                Size = new System.Drawing.Size(380, 40),
                AutoSize = false,
                ForeColor = System.Drawing.Color.Gray
            };
            tab.Controls.Add(lblAttachmentInfo);

            y += 50;

            // Send Test Button
            btnSendTest = new Button
            {
                Text = "Send Test Email",
                Location = new System.Drawing.Point(150, y),
                Size = new System.Drawing.Size(150, 35)
            };
            btnSendTest.Click += BtnSendTest_Click;
            tab.Controls.Add(btnSendTest);

            y += 50;

            // Note
            var lblNote = new Label
            {
                Text = "Note: This sends directly via MS Graph API, bypassing the SMTP server.\n" +
                       "Make sure your MS Graph settings are configured and saved first.\n" +
                       "You can attach any file up to 100 MB in size.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 60),
                AutoSize = false,
                ForeColor = System.Drawing.Color.Gray
            };
            tab.Controls.Add(lblNote);
        }

        private void InitializeStatisticsTab(TabPage tab)
        {
            // Enable scrolling for this tab
            tab.AutoScroll = true;

            int y = 20;

            // Header
            var lblHeader = new Label
            {
                Text = "Service Statistics",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 25),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 12, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblHeader);

            y += 35;

            // Global Stats Section
#pragma warning disable CS8602
            var lblGlobalHeader = new Label
            {
                Text = "Global Statistics",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 10, System.Drawing.FontStyle.Bold)
            };
#pragma warning restore CS8602
            tab.Controls.Add(lblGlobalHeader);

            y += 30;

            var panelGlobal = new Panel
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(540, 180),
                BorderStyle = BorderStyle.FixedSingle
            };

#pragma warning disable CS8602
            lblTotalSuccess = new Label
            {
                Text = "Total Successful: 0",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblTotalSuccess);

#pragma warning disable CS8602
            lblTotalFailed = new Label
            {
                Text = "Total Failed: 0",
                Location = new System.Drawing.Point(270, 10),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblTotalFailed);

#pragma warning disable CS8602
            lblLastSuccess = new Label
            {
                Text = "Last Success: Never",
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblLastSuccess);

#pragma warning disable CS8602
            lblLastFailure = new Label
            {
                Text = "Last Failure: Never",
                Location = new System.Drawing.Point(270, 40),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblLastFailure);

#pragma warning disable CS8602
            lblQueueCount = new Label
            {
                Text = "Messages in Queue: 0",
                Location = new System.Drawing.Point(10, 70),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Bold)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblQueueCount);

            // System Stats - Row 4
#pragma warning disable CS8602
            lblMemoryUsage = new Label
            {
                Text = "Active Memory: 0 MB",
                Location = new System.Drawing.Point(10, 100),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblMemoryUsage);

#pragma warning disable CS8602
            lblCpuUsage = new Label
            {
                Text = "CPU Usage: 0%",
                Location = new System.Drawing.Point(270, 100),
                Size = new System.Drawing.Size(250, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblCpuUsage);

            // System Stats - Row 5
#pragma warning disable CS8602
            lblUptime = new Label
            {
                Text = "Uptime: Not available",
                Location = new System.Drawing.Point(10, 130),
                Size = new System.Drawing.Size(520, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 9, System.Drawing.FontStyle.Regular)
            };
#pragma warning restore CS8602
            panelGlobal.Controls.Add(lblUptime);

            tab.Controls.Add(panelGlobal);

            y += 190;

            // User Stats Section
            var lblUserHeader = new Label
            {
                Text = "Per-User Statistics",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont?.FontFamily ?? System.Drawing.FontFamily.GenericSansSerif, 10, System.Drawing.FontStyle.Bold)
            };
            tab.Controls.Add(lblUserHeader);

            y += 30;

            dgvUserStats = new DataGridView
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(540, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            dgvUserStats.Columns.Add("Username", "Username");
            dgvUserStats.Columns.Add("Authenticated", "Auth");
            dgvUserStats.Columns.Add("Success", "Successful");
            dgvUserStats.Columns.Add("Failed", "Failed");
            dgvUserStats.Columns.Add("LastSuccess", "Last Success");
            dgvUserStats.Columns.Add("LastFailure", "Last Failure");

            // Set column widths
            dgvUserStats.Columns["Username"].FillWeight = 25;
            dgvUserStats.Columns["Authenticated"].FillWeight = 10;
            dgvUserStats.Columns["Success"].FillWeight = 12;
            dgvUserStats.Columns["Failed"].FillWeight = 12;
            dgvUserStats.Columns["LastSuccess"].FillWeight = 20;
            dgvUserStats.Columns["LastFailure"].FillWeight = 20;

            tab.Controls.Add(dgvUserStats);

            y += 260;

            // Buttons
            btnRefreshStats = new Button
            {
                Text = "Refresh Statistics",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(130, 30)
            };
            btnRefreshStats.Click += BtnRefreshStats_Click;
            tab.Controls.Add(btnRefreshStats);

            btnResetStats = new Button
            {
                Text = "Reset Statistics",
                Location = new System.Drawing.Point(160, y),
                Size = new System.Drawing.Size(130, 30)
            };
            btnResetStats.Click += BtnResetStats_Click;
            tab.Controls.Add(btnResetStats);

            // Setup separate auto-refresh timers with different intervals
            
            // CPU updates every 1 second (more responsive)
            _cpuRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };
            _cpuRefreshTimer.Tick += (s, e) => UpdateCpuStats();
            _cpuRefreshTimer.Start();
            
            // Memory and uptime update every 5 seconds
            _memoryRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // 5 seconds
            };
            _memoryRefreshTimer.Tick += (s, e) => UpdateMemoryAndUptimeStats();
            _memoryRefreshTimer.Start();
            
            // Email statistics update every 10 seconds (to catch new events)
            _statsRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 10000 // 10 seconds
            };
            _statsRefreshTimer.Tick += (s, e) => LoadEmailStatistics();
            _statsRefreshTimer.Start();
            
            // Log CPU and Memory stats every 10 minutes
            _statsLoggingTimer = new System.Windows.Forms.Timer
            {
                Interval = 600000 // 10 minutes (600,000 ms)
            };
            _statsLoggingTimer.Tick += (s, e) => LogSystemStats();
            _statsLoggingTimer.Start();

            // Load initial stats
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            // Load all statistics at once (initial load)
            LoadEmailStatistics();
            UpdateMemoryAndUptimeStats();
            UpdateCpuStats();
        }

        private void LoadEmailStatistics()
        {
            try
            {
                var statsManager = new Managers.StatisticsManager();
                var stats = statsManager.GetStatistics();

                // Update global stats
                if (lblTotalSuccess != null)
                {
                    lblTotalSuccess.Text = $"Total Successful: {stats.Global.TotalSuccess:N0}";
                    lblTotalSuccess.ForeColor = System.Drawing.Color.DarkGreen;
                }

                if (lblTotalFailed != null)
                {
                    lblTotalFailed.Text = $"Total Failed: {stats.Global.TotalFailed:N0}";
                    lblTotalFailed.ForeColor = System.Drawing.Color.DarkRed;
                }

                if (lblLastSuccess != null)
                {
                    lblLastSuccess.Text = stats.Global.LastSuccessDate.HasValue 
                        ? $"Last Success: {stats.Global.LastSuccessDate.Value:yyyy-MM-dd HH:mm:ss}"
                        : "Last Success: Never";
                }

                if (lblLastFailure != null)
                {
                    lblLastFailure.Text = stats.Global.LastFailureDate.HasValue
                        ? $"Last Failure: {stats.Global.LastFailureDate.Value:yyyy-MM-dd HH:mm:ss}"
                        : "Last Failure: Never";
                }

                // Get queue count
                try
                {
                    var queueManager = new Managers.QueueManager(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<Managers.QueueManager>.Instance,
                        _config.QueueSettings
                    );
                    if (lblQueueCount != null)
                    {
                        lblQueueCount.Text = $"Messages in Queue: {queueManager.GetQueueCount()}";
                    }
                }
                catch
                {
                    if (lblQueueCount != null)
                    {
                        lblQueueCount.Text = "Messages in Queue: N/A";
                    }
                }


                // Update user stats grid
                dgvUserStats.Rows.Clear();
                foreach (var userStat in stats.UserStats.Values.OrderBy(u => u.Username))
                {
                    var isAuthenticated = !userStat.Username.StartsWith("IP:");
                    var displayName = isAuthenticated ? userStat.Username : userStat.Username.Substring(3); // Remove "IP:" prefix
                    
                    dgvUserStats.Rows.Add(
                        displayName,
                        isAuthenticated ? "Yes" : "No",
                        userStat.TotalSuccess.ToString("N0"),
                        userStat.TotalFailed.ToString("N0"),
                        userStat.LastSuccessDate.HasValue ? userStat.LastSuccessDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never",
                        userStat.LastFailureDate.HasValue ? userStat.LastFailureDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never"
                    );
                    
                    // Color code the authenticated column
                    var lastRow = dgvUserStats.Rows[dgvUserStats.Rows.Count - 1];
                    lastRow.Cells["Authenticated"].Style.ForeColor = isAuthenticated ? System.Drawing.Color.DarkGreen : System.Drawing.Color.DarkOrange;
                    lastRow.Cells["Authenticated"].Style.Font = new System.Drawing.Font(dgvUserStats.Font ?? System.Drawing.SystemFonts.DefaultFont ?? new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 9F), System.Drawing.FontStyle.Bold);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateMemoryAndUptimeStats()
        {
            try
            {
                // Get current process
                var currentProcess = Process.GetCurrentProcess();

                // Active Memory Usage (WorkingSet64 = physical RAM actively in use)
                var memoryMB = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                if (lblMemoryUsage != null)
                {
                    // Track min/max
                    if (memoryMB < _minMemoryMB) _minMemoryMB = memoryMB;
                    if (memoryMB > _maxMemoryMB) _maxMemoryMB = memoryMB;
                    
                    lblMemoryUsage.Text = $"Active Memory: {memoryMB:N2} MB";
                    
                    // Update tooltip with min/max
                    _statsToolTip.SetToolTip(lblMemoryUsage, 
                        $"Current: {memoryMB:N2} MB\n" +
                        $"Min: {_minMemoryMB:N2} MB\n" +
                        $"Max: {_maxMemoryMB:N2} MB");
                    
                    // Color code based on usage
                    if (memoryMB > 500)
                        lblMemoryUsage.ForeColor = System.Drawing.Color.DarkRed;
                    else if (memoryMB > 200)
                        lblMemoryUsage.ForeColor = System.Drawing.Color.DarkOrange;
                    else
                        lblMemoryUsage.ForeColor = System.Drawing.Color.DarkGreen;
                }

                // Uptime
                if (lblUptime != null)
                {
                    var uptime = DateTime.Now - currentProcess.StartTime;
                    
                    string uptimeText;
                    if (uptime.TotalDays >= 1)
                        uptimeText = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                    else if (uptime.TotalHours >= 1)
                        uptimeText = $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
                    else if (uptime.TotalMinutes >= 1)
                        uptimeText = $"{uptime.Minutes}m {uptime.Seconds}s";
                    else
                        uptimeText = $"{uptime.Seconds}s";
                    
                    lblUptime.Text = $"Uptime: {uptimeText}";
                    lblUptime.ForeColor = System.Drawing.Color.DarkBlue;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating memory and uptime statistics");
                
                if (lblMemoryUsage != null)
                    lblMemoryUsage.Text = "Active Memory: Error";
                if (lblUptime != null)
                    lblUptime.Text = "Uptime: Error";
            }
        }

        private void UpdateCpuStats()
        {
            try
            {
                // CPU Usage for this specific application process
                if (lblCpuUsage != null)
                {
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        var processName = currentProcess.ProcessName;
                        
                        // Create performance counter for this specific process
                        var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
                        cpuCounter.NextValue(); // First call returns 0
                        System.Threading.Thread.Sleep(100); // Wait a bit
                        var cpuUsage = cpuCounter.NextValue();
                        
                        // Normalize to percentage (can be > 100% on multi-core systems)
                        var normalizedCpuUsage = cpuUsage / Environment.ProcessorCount;
                        
                        // Track min/max
                        if (normalizedCpuUsage < _minCpuUsage) _minCpuUsage = normalizedCpuUsage;
                        if (normalizedCpuUsage > _maxCpuUsage) _maxCpuUsage = normalizedCpuUsage;
                        
                        lblCpuUsage.Text = $"CPU Usage: {normalizedCpuUsage:N1}%";
                        
                        // Update tooltip with min/max
                        _statsToolTip.SetToolTip(lblCpuUsage,
                            $"Current: {normalizedCpuUsage:N1}%\n" +
                            $"Min: {_minCpuUsage:N1}%\n" +
                            $"Max: {_maxCpuUsage:N1}%");
                        
                        // Color code based on usage
                        if (normalizedCpuUsage > 80)
                            lblCpuUsage.ForeColor = System.Drawing.Color.DarkRed;
                        else if (normalizedCpuUsage > 50)
                            lblCpuUsage.ForeColor = System.Drawing.Color.DarkOrange;
                        else
                            lblCpuUsage.ForeColor = System.Drawing.Color.DarkGreen;
                            
                        cpuCounter.Dispose();
                    }
                    catch
                    {
                        lblCpuUsage.Text = "CPU Usage: N/A";
                        lblCpuUsage.ForeColor = System.Drawing.Color.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating CPU statistics");
                
                if (lblCpuUsage != null)
                    lblCpuUsage.Text = "CPU Usage: Error";
            }
        }

        private void UpdateSystemStats()
        {
            // Legacy method - now calls the split methods
            UpdateMemoryAndUptimeStats();
            UpdateCpuStats();
        }

        private void LogSystemStats()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var uptime = DateTime.Now - currentProcess.StartTime;
                var currentMemoryMB = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                
                // Get current CPU (need to sample it)
                double currentCpuUsage = 0;
                try
                {
                    var processName = currentProcess.ProcessName;
                    var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
                    cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    var cpuValue = cpuCounter.NextValue();
                    currentCpuUsage = cpuValue / Environment.ProcessorCount;
                    cpuCounter.Dispose();
                }
                catch
                {
                    // If we can't get CPU, just use 0
                }
                
                string uptimeText;
                if (uptime.TotalDays >= 1)
                    uptimeText = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                else if (uptime.TotalHours >= 1)
                    uptimeText = $"{uptime.Hours}h {uptime.Minutes}m";
                else
                    uptimeText = $"{uptime.Minutes}m";
                
                Log.Information(
                    "System Stats - CPU: {CurrentCpu:N1}% (Min: {MinCpu:N1}%, Max: {MaxCpu:N1}%) | " +
                    "Memory: {CurrentMemory:N2} MB (Min: {MinMemory:N2} MB, Max: {MaxMemory:N2} MB) | " +
                    "Uptime: {Uptime}",
                    currentCpuUsage,
                    _minCpuUsage == double.MaxValue ? 0 : _minCpuUsage,
                    _maxCpuUsage == double.MinValue ? 0 : _maxCpuUsage,
                    currentMemoryMB,
                    _minMemoryMB == double.MaxValue ? 0 : _minMemoryMB,
                    _maxMemoryMB == double.MinValue ? 0 : _maxMemoryMB,
                    uptimeText);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error logging system statistics");
            }
        }

        private void BtnRefreshStats_Click(object? sender, EventArgs e)
        {
            LoadStatistics();
            MessageBox.Show("Statistics refreshed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetStats_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset ALL statistics?\n\nThis will clear:\n" +
                "- Global success/failure counts\n" +
                "- All per-user statistics\n" +
                "- Last success/failure dates\n\n" +
                "This action cannot be undone!",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var statsManager = new Managers.StatisticsManager();
                    statsManager.ResetStatistics();
                    LoadStatistics();
                    MessageBox.Show("All statistics have been reset.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting statistics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InitializeChangelogTab(TabPage tab)
        {
            // Use RichTextBox for better formatting control
            var rtbChangelog = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point),
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.None,
                WordWrap = true
            };

            // Try to load and format changelog from docs folder
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var changelogPath = Path.Combine(baseDirectory, "docs", "CHANGELOG.md");
                
                if (File.Exists(changelogPath))
                {
                    var changelogText = File.ReadAllText(changelogPath);
                    FormatChangelog(rtbChangelog, changelogText);
                    rtbChangelog.Select(0, 0); // Deselect and scroll to top
                }
                else
                {
                    rtbChangelog.Text = "CHANGELOG.md not found.\n\n" +
                                       $"Expected location: {changelogPath}\n\n" +
                                       "The changelog file should be located in the 'docs' folder " +
                                       "next to the application executable.";
                    rtbChangelog.SelectionColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                rtbChangelog.Text = $"Error loading changelog:\n\n{ex.Message}";
                rtbChangelog.SelectionColor = System.Drawing.Color.Red;
            }

            tab.Controls.Add(rtbChangelog);

            // Add a bottom panel with a refresh button
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = System.Drawing.Color.WhiteSmoke
            };

            var btnRefresh = new Button
            {
                Text = "Refresh Changelog",
                Location = new System.Drawing.Point(10, 5),
                Size = new System.Drawing.Size(120, 30)
            };
            btnRefresh.Click += (s, e) =>
            {
                try
                {
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var changelogPath = Path.Combine(baseDirectory, "docs", "CHANGELOG.md");
                    
                    if (File.Exists(changelogPath))
                    {
                        var changelogText = File.ReadAllText(changelogPath);
                        rtbChangelog.Clear();
                        FormatChangelog(rtbChangelog, changelogText);
                        rtbChangelog.Select(0, 0);
                        MessageBox.Show("Changelog refreshed successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"CHANGELOG.md not found at:\n{changelogPath}", "File Not Found", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error refreshing changelog:\n\n{ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            bottomPanel.Controls.Add(btnRefresh);
            tab.Controls.Add(bottomPanel);
        }

        private void FormatChangelog(RichTextBox rtb, string text)
        {
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            
            foreach (var line in lines)
            {
                // Skip empty lines but preserve spacing
                if (string.IsNullOrWhiteSpace(line))
                {
                    rtb.AppendText("\n");
                    continue;
                }
                
                // Main title (# Header)
                if (line.StartsWith("# ") && !line.StartsWith("## "))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
                    rtb.SelectionColor = System.Drawing.Color.DarkBlue;
                    rtb.AppendText(line.Substring(2) + "\n");
                    continue;
                }
                
                // Version headers (## VERSION)
                if (line.StartsWith("## VERSION"))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
                    rtb.SelectionColor = System.Drawing.Color.DarkGreen;
                    rtb.AppendText("\n" + line.Substring(3) + "\n");
                    continue;
                }
                
                // Section headers (## HEADER or ### HEADER)
                if (line.StartsWith("### "))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                    rtb.SelectionColor = System.Drawing.Color.DarkOrange;
                    rtb.AppendText("\n" + line.Substring(4) + "\n");
                    continue;
                }
                
                if (line.StartsWith("## ") && !line.StartsWith("## VERSION"))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
                    rtb.SelectionColor = System.Drawing.Color.DarkRed;
                    rtb.AppendText("\n" + line.Substring(3) + "\n");
                    continue;
                }
                
                // Horizontal rules (---)
                if (line.Trim().StartsWith("---"))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
                    rtb.SelectionColor = System.Drawing.Color.LightGray;
                    rtb.AppendText("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
                    continue;
                }
                
                // Bold items (- **Bold text**)
                if (line.TrimStart().StartsWith("- **"))
                {
                    var content = line.TrimStart().Substring(2); // Remove "- "
                    
                    // Parse **bold** sections
                    var parts = content.Split(new[] { "**" }, StringSplitOptions.None);
                    
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                    rtb.SelectionColor = System.Drawing.Color.Black;
                    rtb.AppendText("  • ");
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i % 2 == 1) // Odd indices are bold
                        {
                            rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                            rtb.SelectionColor = System.Drawing.Color.Black;
                            rtb.AppendText(parts[i]);
                        }
                        else
                        {
                            rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                            rtb.SelectionColor = System.Drawing.Color.Black;
                            rtb.AppendText(parts[i]);
                        }
                    }
                    rtb.AppendText("\n");
                    continue;
                }
                
                // Regular bullet points (- item)
                if (line.TrimStart().StartsWith("- "))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                    rtb.SelectionColor = System.Drawing.Color.Black;
                    rtb.AppendText("  • " + line.TrimStart().Substring(2) + "\n");
                    continue;
                }
                
                // Emoji bullets (❌, 🔜, etc.)
                if (line.TrimStart().StartsWith("❌") || line.TrimStart().StartsWith("✅") || 
                    line.TrimStart().StartsWith("🔜"))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                    rtb.SelectionColor = System.Drawing.Color.DimGray;
                    rtb.AppendText("  " + line.TrimStart() + "\n");
                    continue;
                }
                
                // Sub-items (indented with spaces)
                if (line.StartsWith("  ") && !line.TrimStart().StartsWith("-"))
                {
                    rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
                    rtb.SelectionColor = System.Drawing.Color.DarkSlateGray;
                    rtb.AppendText(line + "\n");
                    continue;
                }
                
                // Regular text
                rtb.SelectionFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
                rtb.SelectionColor = System.Drawing.Color.Black;
                rtb.AppendText(line + "\n");
            }
        }

        private void LoadConfiguration()
        {
            // Load SMTP settings
            txtSmtpPort.Text = _smtpConfig.Port.ToString();
            
            // Load bind address
            if (_smtpConfig.BindAddress == "127.0.0.1")
                cmbBindAddress.SelectedIndex = 1;
            else
                cmbBindAddress.SelectedIndex = 0; // Default to 0.0.0.0
            
            numMaxMessageSizeKb.Value = _smtpConfig.MaxMessageSizeKb / 1024; // Convert KB to MB for display
            chkRequireAuth.Checked = _smtpConfig.RequireAuthentication;
            
            // Load flow control settings
            _isFlowing = _smtpConfig.SmtpFlowEnabled;
            numSendDelay.Value = _smtpConfig.SendDelayMs;
            UpdateFlowUI();
            
            lstUsers.Items.Clear();
            foreach (var cred in _userConfig.Credentials)
            {
                lstUsers.Items.Add(cred.Username);
            }

            // Load Graph settings - store actual values and display masked versions
            _actualTenantId = _graphConfig.TenantId;
            _actualClientId = _graphConfig.ClientId;
            
            txtTenantId.Text = MaskGuid(_graphConfig.TenantId);
            txtClientId.Text = MaskGuid(_graphConfig.ClientId);
            txtClientSecret.Text = _graphConfig.ClientSecret;
            txtSenderEmail.Text = _graphConfig.SenderEmail;

            // Load Queue settings
            numMaxRetry.Value = _config.QueueSettings.MaxRetryAttempts;
            numRetryDelay.Value = _config.QueueSettings.RetryDelayMinutes;

            // Load Application settings
            cmbRunMode.SelectedIndex = _config.ApplicationSettings.RunMode;
            
            // Convert log location to absolute path for display
            var logLocation = _config.LogSettings.LogLocation;
            if (string.IsNullOrWhiteSpace(logLocation))
            {
                txtLogLocation.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }
            else if (!Path.IsPathRooted(logLocation))
            {
                txtLogLocation.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logLocation);
            }
            else
            {
                txtLogLocation.Text = logLocation;
            }

            // Load Update Settings
            chkAutoUpdateEnabled.Checked = _config.UpdateSettings.AutoUpdateEnabled;
            cmbCheckFrequency.SelectedIndex = (int)_config.UpdateSettings.CheckFrequency - 1; // Enum starts at 1
            dtpCheckTime.Value = DateTime.Today.Add(_config.UpdateSettings.CheckTime);
            cmbWeeklyCheckDay.SelectedIndex = (int)_config.UpdateSettings.WeeklyCheckDay;
            chkAutoDownload.Checked = _config.UpdateSettings.AutoDownload;
            chkAutoInstall.Checked = _config.UpdateSettings.AutoInstall;
            chkCheckOnStartup.Checked = _config.UpdateSettings.CheckOnStartup;
            
            // Update status labels
            if (_config.UpdateSettings.LastCheckDate.HasValue)
            {
                lblLastCheckDate.Text = $"Last Check: {_config.UpdateSettings.LastCheckDate.Value:yyyy-MM-dd HH:mm:ss}";
            }
            if (_config.UpdateSettings.LastUpdateDate.HasValue)
            {
                lblLastUpdateDate.Text = $"Last Download: {_config.UpdateSettings.LastUpdateDate.Value:yyyy-MM-dd HH:mm:ss}";
            }
            if (!string.IsNullOrEmpty(_config.UpdateSettings.LastInstalledVersion))
            {
                lblLastInstalledVersion.Text = $"Last Installed Version: {_config.UpdateSettings.LastInstalledVersion}";
            }
            
            // Set initial visibility and enabled states for update controls
            CmbCheckFrequency_SelectedIndexChanged(null, EventArgs.Empty);
            ChkAutoUpdateEnabled_CheckedChanged(null, EventArgs.Empty);

            // Wire up change tracking after loading initial values
            WireUpChangeTracking();
            
            // Add special handlers for Tenant ID and Client ID to handle masking
            txtTenantId.Enter += TxtTenantId_Enter;
            txtTenantId.Leave += TxtTenantId_Leave;
            txtClientId.Enter += TxtClientId_Enter;
            txtClientId.Leave += TxtClientId_Leave;
        }

        private void TxtTenantId_Enter(object? sender, EventArgs e)
        {
            // When user clicks into the field, show the actual value (if it was masked)
            if (IsMasked(txtTenantId.Text) && !string.IsNullOrEmpty(_actualTenantId))
            {
                txtTenantId.Text = _actualTenantId;
                txtTenantId.SelectAll();
            }
        }

        private void TxtTenantId_Leave(object? sender, EventArgs e)
        {
            // When user leaves the field, update actual value and show masked version
            if (!IsMasked(txtTenantId.Text))
            {
                _actualTenantId = txtTenantId.Text.Trim();
                if (!string.IsNullOrEmpty(_actualTenantId))
                {
                    txtTenantId.Text = MaskGuid(_actualTenantId);
                }
                MarkAsChanged(); // Ensure save button enables when editing these fields
            }
        }

        private void TxtClientId_Enter(object? sender, EventArgs e)
        {
            // When user clicks into the field, show the actual value (if it was masked)
            if (IsMasked(txtClientId.Text) && !string.IsNullOrEmpty(_actualClientId))
            {
                txtClientId.Text = _actualClientId;
                txtClientId.SelectAll();
            }
        }

        private void TxtClientId_Leave(object? sender, EventArgs e)
        {
            // When user leaves the field, update actual value and show masked version
            if (!IsMasked(txtClientId.Text))
            {
                _actualClientId = txtClientId.Text.Trim();
                if (!string.IsNullOrEmpty(_actualClientId))
                {
                    txtClientId.Text = MaskGuid(_actualClientId);
                }
                MarkAsChanged(); // Ensure save button enables when editing these fields
            }
        }

        private void WireUpChangeTracking()
        {
            // SMTP Settings
            txtSmtpPort.TextChanged += (s, e) => MarkAsChanged();
            txtSmtpPort.Leave += (s, e) => MarkAsChanged();
            cmbBindAddress.SelectedIndexChanged += (s, e) => MarkAsChanged();
            numMaxMessageSizeKb.ValueChanged += (s, e) => MarkAsChanged();
            numMaxMessageSizeKb.Leave += (s, e) => MarkAsChanged();
            numMaxMessageSizeKb.KeyUp += (s, e) => MarkAsChanged();
            chkRequireAuth.CheckedChanged += (s, e) => MarkAsChanged();
            numSendDelay.ValueChanged += (s, e) => MarkAsChanged();
            txtUsername.TextChanged += (s, e) => MarkAsChanged();
            txtUsername.Leave += (s, e) => MarkAsChanged();
            txtPassword.TextChanged += (s, e) => MarkAsChanged();
            txtPassword.Leave += (s, e) => MarkAsChanged();

            // Graph Settings
            txtTenantId.TextChanged += (s, e) => MarkAsChanged();
            txtTenantId.Leave += (s, e) => MarkAsChanged();
            txtClientId.TextChanged += (s, e) => MarkAsChanged();
            txtClientId.Leave += (s, e) => MarkAsChanged();
            txtClientSecret.TextChanged += (s, e) => MarkAsChanged();
            txtClientSecret.Leave += (s, e) => MarkAsChanged();
            txtSenderEmail.TextChanged += (s, e) => MarkAsChanged();
            txtSenderEmail.Leave += (s, e) => MarkAsChanged();

            // Queue Settings
            numMaxRetry.ValueChanged += (s, e) => MarkAsChanged();
            numMaxRetry.Leave += (s, e) => MarkAsChanged();
            numMaxRetry.KeyUp += (s, e) => MarkAsChanged();
            numRetryDelay.ValueChanged += (s, e) => MarkAsChanged();
            numRetryDelay.Leave += (s, e) => MarkAsChanged();
            numRetryDelay.KeyUp += (s, e) => MarkAsChanged();

            // Application Settings
            cmbRunMode.SelectedIndexChanged += (s, e) => MarkAsChanged();
            txtLogLocation.TextChanged += (s, e) => MarkAsChanged();
            txtLogLocation.Leave += (s, e) => MarkAsChanged();

            // Update Settings
            // Note: chkAutoUpdateEnabled, chkAutoDownload, and cmbCheckFrequency already have custom handlers that call MarkAsChanged()
            dtpCheckTime.ValueChanged += (s, e) => MarkAsChanged();
            cmbWeeklyCheckDay.SelectedIndexChanged += (s, e) => MarkAsChanged();
            chkAutoInstall.CheckedChanged += (s, e) => MarkAsChanged();
            chkCheckOnStartup.CheckedChanged += (s, e) => MarkAsChanged();
        }

        private void MarkAsChanged()
        {
            if (!_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
                btnSave.Enabled = true;
                btnCancel.Text = "Cancel";
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to close without saving?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        private void BtnExit_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit the application?\n\nThis will terminate the SMTP service.",
                "Exit Application",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Log.Information("User requested application exit from Configuration form");
                Log.CloseAndFlush();
                Environment.Exit(0);
            }
        }

        private void BtnAddUser_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter both username and password", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _userConfig.Credentials.Add(new SmtpCredential
            {
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text
            });

            lstUsers.Items.Add(txtUsername.Text.Trim());
            txtUsername.Clear();
            txtPassword.Clear();
            
            MarkAsChanged();
        }

        private void BtnRemoveUser_Click(object? sender, EventArgs e)
        {
            if (lstUsers.SelectedIndex >= 0)
            {
                var username = lstUsers.SelectedItem?.ToString();
                _userConfig.Credentials.RemoveAll(c => c.Username == username);
                lstUsers.Items.RemoveAt(lstUsers.SelectedIndex);
                
                MarkAsChanged();
            }
        }

        private void BtnShowPaths_Click(object? sender, EventArgs e)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var configPath = Path.Combine(baseDir, "config", "smtp-config.json");
                var logLocation = _config.LogSettings.LogLocation;
                var logDir = string.IsNullOrWhiteSpace(logLocation) 
                    ? Path.Combine(baseDir, "logs")
                    : (Path.IsPathRooted(logLocation) ? logLocation : Path.Combine(baseDir, logLocation));
                
                var message = "Application File Locations:\n\n" +
                              $"Application Directory:\n{baseDir}\n\n" +
                              $"Configuration File:\n{configPath}\n" +
                              $"Exists: {File.Exists(configPath)}\n\n" +
                              $"Log Directory:\n{logDir}\n" +
                              $"Exists: {Directory.Exists(logDir)}\n\n" +
                              $"Click OK to open the application directory.";
                
                MessageBox.Show(message, "File Locations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Open the application directory
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = baseDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing file paths: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkAutoUpdateEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            // Enable/disable all update-related controls based on main checkbox
            var enabled = chkAutoUpdateEnabled.Checked;
            cmbCheckFrequency.Enabled = enabled;
            dtpCheckTime.Enabled = enabled;
            cmbWeeklyCheckDay.Enabled = enabled && cmbCheckFrequency.SelectedIndex == 1;
            lblWeeklyDay.Enabled = enabled && cmbCheckFrequency.SelectedIndex == 1;
            chkAutoDownload.Enabled = enabled;
            chkAutoInstall.Enabled = enabled && chkAutoDownload.Checked;
            chkCheckOnStartup.Enabled = enabled;
            
            MarkAsChanged();
        }

        private void CmbCheckFrequency_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Show/hide weekly day selector based on frequency
            var isWeekly = cmbCheckFrequency.SelectedIndex == 1; // 1 = Weekly
            lblWeeklyDay.Visible = isWeekly;
            cmbWeeklyCheckDay.Visible = isWeekly;
            lblWeeklyDay.Enabled = isWeekly && chkAutoUpdateEnabled.Checked;
            cmbWeeklyCheckDay.Enabled = isWeekly && chkAutoUpdateEnabled.Checked;
            
            MarkAsChanged();
        }

        private void ChkAutoDownload_CheckedChanged(object? sender, EventArgs e)
        {
            // Auto-install requires auto-download to be enabled
            chkAutoInstall.Enabled = chkAutoDownload.Checked && chkAutoUpdateEnabled.Checked;
            if (!chkAutoDownload.Checked)
            {
                chkAutoInstall.Checked = false;
            }
            
            MarkAsChanged();
        }

        private void BtnRemoveService_Click(object? sender, EventArgs e)
        {
            try
            {
                const string serviceName = "SMTP to Graph Relay";
                
                // Check if service is installed
                var service = System.ServiceProcess.ServiceController.GetServices()
                    .FirstOrDefault(s => s.ServiceName == serviceName);

                if (service == null)
                {
                    MessageBox.Show(
                        "Service is not currently installed.\n\n" +
                        "There is no service to remove.",
                        "Service Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Confirm removal
                var result = MessageBox.Show(
                    $"Are you sure you want to remove the '{serviceName}' service?\n\n" +
                    "This will uninstall the Windows Service.\n" +
                    "The application files will remain on your system.\n\n" +
                    "You can reinstall the service later using 'Install Service'.\n\n" +
                    "Do you want to continue?",
                    "Confirm Service Removal",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                // Try to stop the service if it's running
                if (service.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    var stopResult = MessageBox.Show(
                        "The service is currently running.\n\n" +
                        "It must be stopped before removal.\n\n" +
                        "Stop the service now?",
                        "Service Running",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (stopResult == DialogResult.Yes)
                    {
                        try
                        {
                            var stopProcess = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "sc",
                                    Arguments = $"stop \"{serviceName}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true,
                                    Verb = "runas"
                                }
                            };

                            stopProcess.Start();
                            stopProcess.WaitForExit();

                            // Wait a moment for the service to stop
                            System.Threading.Thread.Sleep(2000);
                        }
                        catch (Exception stopEx)
                        {
                            MessageBox.Show(
                                $"Error stopping service: {stopEx.Message}\n\n" +
                                "You may need to stop it manually before removing.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // Remove the service
                var removeProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"delete \"{serviceName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = "runas" // Run as administrator
                    }
                };

                removeProcess.Start();
                string output = removeProcess.StandardOutput.ReadToEnd();
                string error = removeProcess.StandardError.ReadToEnd();
                removeProcess.WaitForExit();

                if (removeProcess.ExitCode == 0 || output.Contains("SUCCESS"))
                {
                    Log.Information($"Service '{serviceName}' removed successfully");
                    
                    // Refresh the system tray menu to show "Install Service" option
                    TrayApplicationContext.Instance?.RefreshMenu();
                    
                    MessageBox.Show(
                        $"Service '{serviceName}' has been removed successfully!\n\n" +
                        "The application files remain on your system.\n" +
                        "You can reinstall the service anytime using 'Install Service'.\n\n" +
                        "The system tray menu has been updated.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    Log.Error($"Failed to remove service: {error}");
                    MessageBox.Show(
                        $"Failed to remove service:\n\n{error}\n\n" +
                        "You may need to run this application as administrator.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    "Administrator privileges are required to remove the service.\n\n" +
                    "Please run this application as administrator and try again.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing service");
                MessageBox.Show(
                    $"Error removing service: {ex.Message}\n\n" +
                    "Make sure you have administrator privileges.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                Log.Information("User clicked Save button - validating and saving configuration");

                // Validate port
                if (!int.TryParse(txtSmtpPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Invalid SMTP port. Please enter a port between 1 and 65535.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Capture old SMTP config values before changing them
                var oldRequireAuth = _smtpConfig.RequireAuthentication;
                var oldPort = _smtpConfig.Port;
                var oldBindAddress = _smtpConfig.BindAddress;

                // Update SMTP configuration
                _smtpConfig.Port = port;
                _smtpConfig.BindAddress = cmbBindAddress.SelectedIndex == 1 ? "127.0.0.1" : "0.0.0.0";
                _smtpConfig.RequireAuthentication = chkRequireAuth.Checked;
                _smtpConfig.MaxMessageSizeKb = (int)(numMaxMessageSizeKb.Value * 1024); // Convert MB to KB
                _smtpConfig.SendDelayMs = (int)numSendDelay.Value;

                // Save SMTP configuration first
                Log.Information($"Saving SMTP configuration - RequireAuthentication changing from {oldRequireAuth} to {_smtpConfig.RequireAuthentication}");
                _configManager.SaveSmtpConfiguration(_smtpConfig);

                // Save User configuration
                Log.Information($"Saving User configuration - {_userConfig.Credentials.Count} users");
                _configManager.SaveUserConfiguration(_userConfig);

                // Update Graph configuration with actual values (not masked)
                if (!IsMasked(txtTenantId.Text))
                {
                    _graphConfig.TenantId = txtTenantId.Text.Trim();
                }
                else if (!string.IsNullOrEmpty(_actualTenantId))
                {
                    _graphConfig.TenantId = _actualTenantId;
                }

                if (!IsMasked(txtClientId.Text))
                {
                    _graphConfig.ClientId = txtClientId.Text.Trim();
                }
                else if (!string.IsNullOrEmpty(_actualClientId))
                {
                    _graphConfig.ClientId = _actualClientId;
                }

                _graphConfig.ClientSecret = txtClientSecret.Text.Trim();
                _graphConfig.SenderEmail = txtSenderEmail.Text.Trim();

                Log.Information("Saving Graph configuration");
                _configManager.SaveGraphConfiguration(_graphConfig);

                // Update App configuration
                _config.QueueSettings.MaxRetryAttempts = (int)numMaxRetry.Value;
                _config.QueueSettings.RetryDelayMinutes = (int)numRetryDelay.Value;
                _config.ApplicationSettings.RunMode = cmbRunMode.SelectedIndex;

                // Handle log location
                var logLocationValue = txtLogLocation.Text.Trim();
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (logLocationValue.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    // Convert absolute path to relative path
                    var relativePath = Path.GetRelativePath(baseDirectory, logLocationValue);
                    _config.LogSettings.LogLocation = relativePath;
                }
                else if (Path.IsPathRooted(logLocationValue))
                {
                    // Use absolute path as-is
                    _config.LogSettings.LogLocation = logLocationValue;
                }
                else
                {
                    // Already relative
                    _config.LogSettings.LogLocation = logLocationValue;
                }

                // Update settings
                _config.UpdateSettings.AutoUpdateEnabled = chkAutoUpdateEnabled.Checked;
                _config.UpdateSettings.CheckFrequency = (UpdateCheckFrequency)(cmbCheckFrequency.SelectedIndex + 1);
                _config.UpdateSettings.CheckTime = dtpCheckTime.Value.TimeOfDay;
                _config.UpdateSettings.WeeklyCheckDay = (DayOfWeek)cmbWeeklyCheckDay.SelectedIndex;
                _config.UpdateSettings.AutoDownload = chkAutoDownload.Checked;
                _config.UpdateSettings.AutoInstall = chkAutoInstall.Checked;
                _config.UpdateSettings.CheckOnStartup = chkCheckOnStartup.Checked;

                Log.Information("Saving App configuration");
                _configManager.SaveConfiguration(_config);

                // Update flow control
                if (SmtpFlowControl.Instance.SendDelayMs != _smtpConfig.SendDelayMs)
                {
                    SmtpFlowControl.Instance.UpdateSendDelay(_smtpConfig.SendDelayMs);
                    Log.Information($"Updated send delay to {_smtpConfig.SendDelayMs}ms");
                }

                // Clear unsaved changes flag
                _hasUnsavedChanges = false;
                btnSave.Enabled = false;
                btnCancel.Text = "Close";

                // Determine if critical SMTP settings changed that require restart
                bool smtpRequiresRestart = oldPort != _smtpConfig.Port || oldBindAddress != _smtpConfig.BindAddress;

                // Show appropriate success message based on what changed
                if (_isUiOnlyMode)
                {
                    // UI-only mode - configuration saved, service will reload automatically via events
                    if (smtpRequiresRestart)
                    {
                        MessageBox.Show(
                            "Configuration saved successfully!\n\n" +
                            "⚠️ SMTP server settings changed:\n" +
                            $"  • Port: {oldPort} → {_smtpConfig.Port}\n" +
                            $"  • Bind: {oldBindAddress} → {_smtpConfig.BindAddress}\n\n" +
                            "The SMTP server is restarting with new settings...\n\n" +
                            "✅ Other settings (authentication, users) updated immediately.",
                            "Configuration Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else if (oldRequireAuth != _smtpConfig.RequireAuthentication)
                    {
                        MessageBox.Show(
                            "Configuration saved successfully!\n\n" +
                            $"✅ Authentication requirement changed: {(oldRequireAuth ? "Required" : "Optional")} → {(_smtpConfig.RequireAuthentication ? "Required" : "Optional")}\n\n" +
                            "This change takes effect immediately for new connections.",
                            "Configuration Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Configuration saved successfully!\n\n" +
                            "✅ All settings updated and applied.",
                            "Configuration Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                    Log.Information("Configuration saved in UI-only mode - service will reload automatically");
                }
                else
                {
                    // Service mode - restart required
                    MessageBox.Show(
                        "Configuration saved successfully!\n\n" +
                        "⚠️ Please restart the application for all changes to take effect.\n\n" +
                        "Changes saved:\n" +
                        $"  • SMTP Port: {_smtpConfig.Port}\n" +
                        $"  • SMTP Bind Address: {_smtpConfig.BindAddress}\n" +
                        $"  • Require Authentication: {(_smtpConfig.RequireAuthentication ? "Yes" : "No")}\n" +
                        $"  • Users: {_userConfig.Credentials.Count} configured\n" +
                        $"  • Send Delay: {_smtpConfig.SendDelayMs}ms",
                        "Configuration Saved - Restart Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    Log.Information("Configuration saved in service mode - restart required for changes");
                }

                Log.Information("All configuration changes saved successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving configuration");
                MessageBox.Show($"Error saving configuration:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            btnTest.Enabled = false;
            btnTest.Text = "Testing...";

            try
            {
                // Validate inputs first
                if (string.IsNullOrWhiteSpace(_actualTenantId))
                {
                    MessageBox.Show("Tenant ID is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(_actualClientId))
                {
                    MessageBox.Show("Client ID is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(txtClientSecret.Text))
                {
                    MessageBox.Show("Client Secret is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(txtSenderEmail.Text))
                {
                    MessageBox.Show("Sender Email is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var settings = new GraphConfiguration
                {
                    TenantId = _actualTenantId,
                    ClientId = _actualClientId,
                    ClientSecret = txtClientSecret.Text.Trim(),
                    SenderEmail = txtSenderEmail.Text.Trim()
                };

                Serilog.Log.Information("Testing MS Graph connection...");
                Serilog.Log.Information($"Tenant ID: {MaskGuid(settings.TenantId)}");
                Serilog.Log.Information($"Client ID: {MaskGuid(settings.ClientId)}");
                Serilog.Log.Information($"Sender Email: {settings.SenderEmail}");
                
                // Debug: Check if actual values are populated
                if (string.IsNullOrWhiteSpace(_actualTenantId))
                {
                    Serilog.Log.Warning("WARNING: _actualTenantId is empty! Using config value instead.");
                    settings.TenantId = _config.GraphSettings.TenantId;
                }
                if (string.IsNullOrWhiteSpace(_actualClientId))
                {
                    Serilog.Log.Warning("WARNING: _actualClientId is empty! Using config value instead.");
                    settings.ClientId = _config.GraphSettings.ClientId;
                }

                MessageBox.Show(
                    $"Testing with:\n" +
                    $"Tenant ID: {settings.TenantId.Substring(0, Math.Min(8, settings.TenantId.Length))}...\n" +
                    $"Client ID: {settings.ClientId.Substring(0, Math.Min(8, settings.ClientId.Length))}...\n" +
                    $"Secret: {new string('*', Math.Min(10, settings.ClientSecret.Length))}\n" +
                    $"Sender: {settings.SenderEmail}",
                    "Testing Connection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Create a logger factory for GraphEmailService
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog(Log.Logger);
                });
                
                var logger = loggerFactory.CreateLogger<Services.GraphEmailService>();

                // Test Graph connection
                var graphService = new Services.GraphEmailService(
                    logger,
                    settings
                );

                bool success = await graphService.TestConnectionAsync();

                if (success)
                {
                    Serilog.Log.Information("MS Graph connection test successful");
                    MessageBox.Show("MS Graph connection successful!\n\nYour credentials are valid and the service can send emails.", 
                        "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Serilog.Log.Error("MS Graph connection test failed");
                    MessageBox.Show(
                        "MS Graph connection failed.\n\n" +
                        "Possible issues:\n" +
                        "1. Invalid credentials (Tenant ID, Client ID, or Secret)\n" +
                        "2. App registration not found in Azure AD\n" +
                        "3. Mail.Send permission not granted\n" +
                        "4. Admin consent not provided\n" +
                        "5. Sender email doesn't exist in the tenant\n\n" +
                        "Check the logs for detailed error information.",
                        "Test Result",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Azure.Identity.AuthenticationFailedException ex)
            {
                Serilog.Log.Error(ex, "Authentication failed during Graph connection test");
                
                string specificError = "";
                if (ex.Message.Contains("AADSTS7000215") || ex.Message.Contains("Invalid client secret"))
                {
                    specificError = "\n\n⚠️ CLIENT SECRET IS INVALID OR EXPIRED!\n" +
                                   "Go to Azure Portal → App Registrations → Your App → " +
                                   "Certificates & secrets → Create a new client secret.";
                }
                else if (ex.Message.Contains("AADSTS90002"))
                {
                    specificError = "\n\n⚠️ TENANT ID NOT FOUND!\n" +
                                   "Double-check your Tenant ID from Azure Portal → " +
                                   "Azure Active Directory → Overview.";
                }
                else if (ex.Message.Contains("AADSTS700016"))
                {
                    specificError = "\n\n⚠️ CLIENT ID (APPLICATION ID) NOT FOUND!\n" +
                                   "Verify your Client ID from Azure Portal → " +
                                   "App Registrations → Your App → Overview.";
                }
                else if (ex.Message.Contains("unauthorized_client"))
                {
                    specificError = "\n\n⚠️ APP NOT AUTHORIZED!\n" +
                                   "Check API permissions (Mail.Send) and ensure " +
                                   "admin consent is granted.";
                }
                
                MessageBox.Show(
                    $"Authentication Failed!\n\n" +
                    $"Error: {ex.Message}\n" +
                    specificError +
                    $"\n\nCommon causes:\n" +
                    $"- Client Secret expired (most common)\n" +
                    $"- Tenant ID is incorrect\n" +
                    $"- Client ID is incorrect\n\n" +
                    $"Please verify your Azure AD credentials.\n\n" +
                    $"Error details have been logged.",
                    "Authentication Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
            {
                Serilog.Log.Error(ex, "Graph API error during connection test");
                MessageBox.Show(
                    $"Graph API Error!\n\n" +
                    $"Error: {ex.Error?.Message ?? ex.Message}\n\n" +
                    $"Code: {ex.Error?.Code}\n\n" +
                    $"This usually means:\n" +
                    $"- Missing API permissions (Mail.Send)\n" +
                    $"- Admin consent not granted\n" +
                    $"- Sender email doesn't exist\n\n" +
                    $"Please check your App Registration settings.\n\n" +
                    $"Error details have been logged.",
                    "Graph API Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Unexpected error during Graph connection test");
                MessageBox.Show(
                    $"Test failed with unexpected error:\n\n" +
                    $"Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n\n" +
                    $"Inner Exception: {ex.InnerException?.Message ?? "None"}\n\n" +
                    $"Error details have been logged.",
                    "Test Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test Connection";
            }
        }

        private void BtnBrowseAttachment_Click(object? sender, EventArgs e)
        {
            try
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Title = "Select File to Attach",
                    Filter = "All Files (*.*)|*.*",
                    Multiselect = false,
                    CheckFileExists = true
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var fileInfo = new FileInfo(openFileDialog.FileName);
                    
                    // Check file size (100MB limit)
                    const long maxSizeBytes = 100L * 1024 * 1024; // 100 MB
                    if (fileInfo.Length > maxSizeBytes)
                    {
                        MessageBox.Show(
                            $"File is too large!\n\n" +
                            $"Selected file: {fileInfo.Length / 1024.0 / 1024.0:N2} MB\n" +
                            $"Maximum allowed: 100 MB\n\n" +
                            $"Please select a smaller file.",
                            "File Too Large",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    _attachmentFilePath = openFileDialog.FileName;
                    
                    // Update UI
                    lblAttachmentInfo.Text = $"File: {fileInfo.Name}\n" +
                                            $"Size: {fileInfo.Length / 1024.0:N2} KB" +
                                            (fileInfo.Length > 1024 * 1024 ? $" ({fileInfo.Length / 1024.0 / 1024.0:N2} MB)" : "");
                    lblAttachmentInfo.ForeColor = System.Drawing.Color.DarkGreen;
                    btnClearAttachment.Enabled = true;

                    Serilog.Log.Information($"Attachment selected: {fileInfo.Name} ({fileInfo.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearAttachment_Click(object? sender, EventArgs e)
        {
            _attachmentFilePath = null;
            lblAttachmentInfo.Text = "No file selected";
            lblAttachmentInfo.ForeColor = System.Drawing.Color.Gray;
            btnClearAttachment.Enabled = false;
            Serilog.Log.Information("Test email attachment cleared");
        }

        private async void BtnSendTest_Click(object? sender, EventArgs e)
        {
            btnSendTest.Enabled = false;
            btnSendTest.Text = "Sending...";

            try
            {
                // Validate recipient email
                if (string.IsNullOrWhiteSpace(txtTestTo.Text))
                {
                    MessageBox.Show("Please enter a recipient email address", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate email format
                if (!txtTestTo.Text.Contains("@"))
                {
                    MessageBox.Show("Please enter a valid email address", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Serilog.Log.Information($"Sending test email to: {txtTestTo.Text}");

                // Create test email message
                var testEmail = new EmailMessage
                {
                    From = _config.GraphSettings.SenderEmail,
                    To = new List<string> { txtTestTo.Text.Trim() },
                    Subject = string.IsNullOrWhiteSpace(txtTestSubject.Text) ? "Test Email" : txtTestSubject.Text,
                    Body = string.IsNullOrWhiteSpace(txtTestBody.Text) ? "This is a test email." : txtTestBody.Text,
                    IsHtml = chkTestHtml.Checked,
                    ReceivedAt = DateTime.Now
                };

                // Add attachment if one is selected
                if (!string.IsNullOrEmpty(_attachmentFilePath) && File.Exists(_attachmentFilePath))
                {
                    try
                    {
                        Serilog.Log.Information($"Adding attachment: {_attachmentFilePath}");
                        
                        var fileInfo = new FileInfo(_attachmentFilePath);
                        var fileBytes = await File.ReadAllBytesAsync(_attachmentFilePath);
                        
                        // Determine content type
                        var contentType = GetContentType(fileInfo.Extension);
                        
                        var attachment = new EmailAttachment
                        {
                            FileName = fileInfo.Name,
                            ContentType = contentType,
                            Content = fileBytes,
                            Size = fileBytes.Length,
                            IsInline = false
                        };
                        
                        testEmail.Attachments.Add(attachment);
                        Serilog.Log.Information($"Attachment added: {fileInfo.Name} ({fileBytes.Length} bytes, {contentType})");
                    }
                    catch (Exception attachEx)
                    {
                        Serilog.Log.Error(attachEx, "Error reading attachment file");
                        MessageBox.Show(
                            $"Error reading attachment file:\n\n{attachEx.Message}\n\n" +
                            "The email will be sent without the attachment.",
                            "Attachment Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }

                // Create logger for GraphEmailService
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog(Log.Logger);
                });
                var logger = loggerFactory.CreateLogger<Services.GraphEmailService>();

                // Create GraphEmailService with current settings
                var graphSettings = new GraphConfiguration
                {
                    TenantId = string.IsNullOrWhiteSpace(_actualTenantId) ? _graphConfig.TenantId : _actualTenantId,
                    ClientId = string.IsNullOrWhiteSpace(_actualClientId) ? _graphConfig.ClientId : _actualClientId,
                    ClientSecret = txtClientSecret.Text.Trim(),
                    SenderEmail = txtSenderEmail.Text.Trim()
                };
                
                var graphService = new Services.GraphEmailService(
                    logger,
                    graphSettings
                );

                // Send the email
                bool success = await graphService.SendEmailAsync(testEmail);

                if (success)
                {
                    Serilog.Log.Information($"Test email sent successfully to {txtTestTo.Text}");
                    
                    var successMessage = $"Test email sent successfully!\n\n" +
                        $"To: {txtTestTo.Text}\n" +
                        $"Subject: {testEmail.Subject}\n";
                    
                    if (testEmail.Attachments.Count > 0)
                    {
                        var attachment = testEmail.Attachments[0];
                        successMessage += $"Attachment: {attachment.FileName} ({attachment.Size / 1024.0:N2} KB)\n";
                    }
                    
                    successMessage += "\nCheck the recipient's inbox to verify delivery.";
                    
                    MessageBox.Show(
                        successMessage,
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    Serilog.Log.Error("Test email failed to send");
                    MessageBox.Show(
                        $"Failed to send test email!\n\n" +
                        $"Check the logs for detailed error information.\n\n" +
                        $"Common issues:\n" +
                        $"- MS Graph credentials not configured\n" +
                        $"- Sender email doesn't exist\n" +
                        $"- API permissions not granted",
                        "Send Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error sending test email");
                MessageBox.Show(
                    $"Error sending test email:\n\n" +
                    $"Type: {ex.GetType().Name}\n" +
                    $"Message: {ex.Message}\n\n" +
                    $"Check the logs for full details.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSendTest.Enabled = true;
                btnSendTest.Text = "Send Test Email";
            }
        }

        private void BtnBrowseLog_Click(object? sender, EventArgs e)
        {
            // txtLogLocation.Text already contains the absolute path from LoadConfiguration
            string currentLogLocation = txtLogLocation.Text;
            
            // If empty, default to logs folder
            if (string.IsNullOrWhiteSpace(currentLogLocation))
            {
                currentLogLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            // Create directory if it doesn't exist so the browser can navigate to it
            if (!Directory.Exists(currentLogLocation))
            {
                Directory.CreateDirectory(currentLogLocation);
            }

            using var folderBrowser = new FolderBrowserDialog
            {
                Description = "Select log directory",
                ShowNewFolderButton = true,
                SelectedPath = currentLogLocation,
                RootFolder = Environment.SpecialFolder.MyComputer
            };

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtLogLocation.Text = folderBrowser.SelectedPath;
                MarkAsChanged();
            }
        }

        private void BtnOpenLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                string logLocation = string.IsNullOrWhiteSpace(txtLogLocation.Text) 
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
                    : txtLogLocation.Text;

                // If relative path, make it absolute
                if (!Path.IsPathRooted(logLocation))
                {
                    logLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logLocation);
                }

                // Create directory if it doesn't exist
                if (!Directory.Exists(logLocation))
                {
                    Directory.CreateDirectory(logLocation);
                }

                // Open the directory
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logLocation,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetContentType(string extension)
        {
            // Remove leading dot if present
            extension = extension.TrimStart('.').ToLower();

            return extension switch
            {
                // Documents
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ppt" => "application/vnd.ms-powerpoint",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "txt" => "text/plain",
                "csv" => "text/csv",
                "rtf" => "application/rtf",

                // Images
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "svg" => "image/svg+xml",
                "ico" => "image/x-icon",
                "tiff" or "tif" => "image/tiff",
                "webp" => "image/webp",

                // Archives
                "zip" => "application/zip",
                "rar" => "application/x-rar-compressed",
                "7z" => "application/x-7z-compressed",
                "tar" => "application/x-tar",
                "gz" => "application/gzip",

                // Audio
                "mp3" => "audio/mpeg",
                "wav" => "audio/wav",
                "ogg" => "audio/ogg",
                "m4a" => "audio/mp4",
                "flac" => "audio/flac",

                // Video
                "mp4" => "video/mp4",
                "avi" => "video/x-msvideo",
                "mov" => "video/quicktime",
                "wmv" => "video/x-ms-wmv",
                "flv" => "video/x-flv",
                "mkv" => "video/x-matroska",

                // Code/Web
                "html" or "htm" => "text/html",
                "css" => "text/css",
                "js" => "application/javascript",
                "json" => "application/json",
                "xml" => "application/xml",

                // Executables
                "exe" => "application/x-msdownload",
                "dll" => "application/x-msdownload",
                "msi" => "application/x-msi",

                // Default
                _ => "application/octet-stream"
            };
        }
        
        private void BtnToggleFlow_Click(object? sender, EventArgs e)
        {
            if (_isUiOnlyMode)
            {
                // UI-only mode - send command via IPC
                if (_isFlowing)
                {
                    if (ServiceCommandClient.TryHaltSmtp(out string error))
                    {
                        _isFlowing = false;
                        UpdateFlowUI();
                        MessageBox.Show("SMTP flow has been halted.\nNo new connections will be accepted.", 
                            "SMTP Halted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to halt SMTP:\n{error}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    if (ServiceCommandClient.TryResumeSmtp(out string error))
                    {
                        _isFlowing = true;
                        UpdateFlowUI();
                        MessageBox.Show("SMTP flow has been resumed.\nAccepting new connections.", 
                            "SMTP Resumed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to resume SMTP:\n{error}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                // Direct access mode - toggle state and save
                _isFlowing = !_isFlowing;
                _config.SmtpSettings.SmtpFlowEnabled = _isFlowing;
                _configManager.SaveConfiguration(_config);
                UpdateFlowUI();
                
                var message = _isFlowing 
                    ? "SMTP flow has been resumed.\nNote: You must restart the service for this to take effect."
                    : "SMTP flow has been halted.\nNote: You must restart the service for this to take effect.";
                MessageBox.Show(message, "Flow Control Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateFlowUI()
        {
            if (_isFlowing)
            {
                btnToggleFlow.Text = "Halt SMTP";
                lblFlowStatus.Text = "Status: FLOWING";
                lblFlowStatus.ForeColor = System.Drawing.Color.DarkGreen;
            }
            else
            {
                btnToggleFlow.Text = "Resume SMTP";
                lblFlowStatus.Text = "Status: HALTED";
                lblFlowStatus.ForeColor = System.Drawing.Color.DarkRed;
            }
        }
    }
}
