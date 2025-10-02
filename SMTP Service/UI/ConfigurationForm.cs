using System.Windows.Forms;
using SMTP_Service.Models;
using Serilog;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.UI
{
    public class ConfigurationForm : Form
    {
        private readonly Managers.ConfigurationManager _configManager;
        private AppConfig _config;

        // SMTP Settings Controls
        private TextBox txtSmtpPort = null!;
        private CheckBox chkRequireAuth = null!;
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

        // Test Email Controls
        private TextBox txtTestTo = null!;
        private TextBox txtTestSubject = null!;
        private TextBox txtTestBody = null!;
        private CheckBox chkTestHtml = null!;
        private Button btnSendTest = null!;

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
            
            InitializeComponents();
            LoadConfiguration();
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
            this.Text = "SMTP to Graph Relay - Configuration";
            this.Size = new System.Drawing.Size(600, 700);
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
            int y = 20;

            // Port
            var lblPort = new Label { Text = "SMTP Port:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtSmtpPort = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(100, 20) };
            tab.Controls.Add(lblPort);
            tab.Controls.Add(txtSmtpPort);

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

            // User Management
            var lblUsers = new Label { Text = "Authorized Users:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(200, 20) };
            tab.Controls.Add(lblUsers);

            y += 30;

            lstUsers = new ListBox 
            { 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(350, 150) 
            };
            tab.Controls.Add(lstUsers);

            btnRemoveUser = new Button
            {
                Text = "Remove",
                Location = new System.Drawing.Point(380, y),
                Size = new System.Drawing.Size(80, 30)
            };
            btnRemoveUser.Click += BtnRemoveUser_Click;
            tab.Controls.Add(btnRemoveUser);

            y += 160;

            // Add user section
            var lblUsername = new Label { Text = "Username:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtUsername = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(150, 20) };
            tab.Controls.Add(lblUsername);
            tab.Controls.Add(txtUsername);

            y += 30;

            var lblPassword = new Label { Text = "Password:", Location = new System.Drawing.Point(20, y), Size = new System.Drawing.Size(100, 20) };
            txtPassword = new TextBox { Location = new System.Drawing.Point(130, y), Size = new System.Drawing.Size(150, 20), UseSystemPasswordChar = true };
            tab.Controls.Add(lblPassword);
            tab.Controls.Add(txtPassword);

            btnAddUser = new Button
            {
                Text = "Add User",
                Location = new System.Drawing.Point(290, y - 15),
                Size = new System.Drawing.Size(80, 30)
            };
            btnAddUser.Click += BtnAddUser_Click;
            tab.Controls.Add(btnAddUser);
        }

        private void InitializeGraphTab(TabPage tab)
        {
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

            // Instructions
            var lblInstructions = new Label
            {
                Text = "To configure MS Graph:\n" +
                       "1. Register an app in Azure AD\n" +
                       "2. Grant Mail.Send permission\n" +
                       "3. Create a client secret\n" +
                       "4. Copy Tenant ID, Client ID, and Secret here\n\n" +
                       "Use 'Test Connection' above to verify your configuration.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 120),
                AutoSize = false
            };
            tab.Controls.Add(lblInstructions);
        }

        private void InitializeQueueTab(TabPage tab)
        {
            int y = 20;

            // Run Mode Section
            var lblRunModeHeader = new Label 
            { 
                Text = "Application Run Mode:", 
                Location = new System.Drawing.Point(20, y), 
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 9, System.Drawing.FontStyle.Bold)
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
            cmbRunMode.Items.Add("Service/Console Mode (Default)");
            cmbRunMode.Items.Add("Console with Tray Icon");
            cmbRunMode.Items.Add("Tray Only");
            cmbRunMode.SelectedIndex = 0;
            
            tab.Controls.Add(lblRunMode);
            tab.Controls.Add(cmbRunMode);

            y += 30;

            var lblRunModeInfo = new Label
            {
                Text = "This setting determines how the application runs when started without command line arguments.\n" +
                       "• Service/Console Mode: Shows console with logs (good for debugging)\n" +
                       "• Console with Tray: Console logs + system tray icon (best for monitoring)\n" +
                       "• Tray Only: System tray icon only (minimal interface)\n\n" +
                       "Note: Command line arguments (--console or --tray) will override this setting.",
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
                Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 9, System.Drawing.FontStyle.Bold)
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
        }

        private void InitializeTestEmailTab(TabPage tab)
        {
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
                       "Make sure your MS Graph settings are configured and saved first.",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(520, 40),
                AutoSize = false,
                ForeColor = System.Drawing.Color.Gray
            };
            tab.Controls.Add(lblNote);
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
            txtSmtpPort.Text = _config.SmtpSettings.Port.ToString();
            chkRequireAuth.Checked = _config.SmtpSettings.RequireAuthentication;
            
            lstUsers.Items.Clear();
            foreach (var cred in _config.SmtpSettings.Credentials)
            {
                lstUsers.Items.Add(cred.Username);
            }

            // Load Graph settings - store actual values and display masked versions
            _actualTenantId = _config.GraphSettings.TenantId;
            _actualClientId = _config.GraphSettings.ClientId;
            
            txtTenantId.Text = MaskGuid(_config.GraphSettings.TenantId);
            txtClientId.Text = MaskGuid(_config.GraphSettings.ClientId);
            txtClientSecret.Text = _config.GraphSettings.ClientSecret;
            txtSenderEmail.Text = _config.GraphSettings.SenderEmail;

            // Load Queue settings
            numMaxRetry.Value = _config.QueueSettings.MaxRetryAttempts;
            numRetryDelay.Value = _config.QueueSettings.RetryDelayMinutes;

            // Load Application settings
            cmbRunMode.SelectedIndex = _config.ApplicationSettings.RunMode;

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
            }
        }

        private void WireUpChangeTracking()
        {
            // SMTP Settings
            txtSmtpPort.TextChanged += (s, e) => MarkAsChanged();
            chkRequireAuth.CheckedChanged += (s, e) => MarkAsChanged();
            txtUsername.TextChanged += (s, e) => MarkAsChanged();
            txtPassword.TextChanged += (s, e) => MarkAsChanged();

            // Graph Settings
            txtTenantId.TextChanged += (s, e) => MarkAsChanged();
            txtClientId.TextChanged += (s, e) => MarkAsChanged();
            txtClientSecret.TextChanged += (s, e) => MarkAsChanged();
            txtSenderEmail.TextChanged += (s, e) => MarkAsChanged();

            // Queue Settings
            numMaxRetry.ValueChanged += (s, e) => MarkAsChanged();
            numRetryDelay.ValueChanged += (s, e) => MarkAsChanged();

            // Application Settings
            cmbRunMode.SelectedIndexChanged += (s, e) => MarkAsChanged();
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

            _config.SmtpSettings.Credentials.Add(new SmtpCredential
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
                _config.SmtpSettings.Credentials.RemoveAll(c => c.Username == username);
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
                var logPath = _config.LogSettings.LogFilePath;
                var logDir = Path.GetDirectoryName(logPath) ?? Path.Combine(baseDir, "logs");
                
                var message = "Application File Locations:\n\n" +
                              $"Application Directory:\n{baseDir}\n\n" +
                              $"Configuration File:\n{configPath}\n" +
                              $"Exists: {File.Exists(configPath)}\n\n" +
                              $"Log Directory:\n{logDir}\n" +
                              $"Exists: {Directory.Exists(logDir)}\n\n" +
                              $"Log File:\n{logPath}\n" +
                              $"Exists: {File.Exists(logPath)}\n\n" +
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
                // Validate inputs
                if (!int.TryParse(txtSmtpPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535)", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Update configuration
                _config.SmtpSettings.Port = port;
                _config.SmtpSettings.RequireAuthentication = chkRequireAuth.Checked;

                // Use actual values for Tenant ID and Client ID (not the masked display values)
                // If actual values are empty but textbox has masked values, keep the original config values
                if (!string.IsNullOrWhiteSpace(_actualTenantId))
                {
                    _config.GraphSettings.TenantId = _actualTenantId;
                }
                else if (IsMasked(txtTenantId.Text))
                {
                    // Keep existing value - it's still masked, user didn't change it
                    // Don't update _config.GraphSettings.TenantId
                }
                else
                {
                    _config.GraphSettings.TenantId = txtTenantId.Text.Trim();
                }
                
                if (!string.IsNullOrWhiteSpace(_actualClientId))
                {
                    _config.GraphSettings.ClientId = _actualClientId;
                }
                else if (IsMasked(txtClientId.Text))
                {
                    // Keep existing value - it's still masked, user didn't change it
                    // Don't update _config.GraphSettings.ClientId
                }
                else
                {
                    _config.GraphSettings.ClientId = txtClientId.Text.Trim();
                }
                
                _config.GraphSettings.ClientSecret = txtClientSecret.Text;
                _config.GraphSettings.SenderEmail = txtSenderEmail.Text.Trim();

                _config.QueueSettings.MaxRetryAttempts = (int)numMaxRetry.Value;
                _config.QueueSettings.RetryDelayMinutes = (int)numRetryDelay.Value;

                _config.ApplicationSettings.RunMode = cmbRunMode.SelectedIndex;

                // Save configuration
                _configManager.SaveConfiguration(_config);

                // Reset change tracking
                _hasUnsavedChanges = false;
                btnSave.Enabled = false;
                btnCancel.Text = "Close";

                MessageBox.Show("Configuration saved successfully!\n\nNote: You may need to restart the service for changes to take effect.", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                var settings = new GraphSettings
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
                    $"Error details have been logged to: {_config.LogSettings.LogFilePath}",
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

                // Create logger for GraphEmailService
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog(Log.Logger);
                });
                var logger = loggerFactory.CreateLogger<Services.GraphEmailService>();

                // Create GraphEmailService with current settings
                var graphSettings = new GraphSettings
                {
                    TenantId = string.IsNullOrWhiteSpace(_actualTenantId) ? _config.GraphSettings.TenantId : _actualTenantId,
                    ClientId = string.IsNullOrWhiteSpace(_actualClientId) ? _config.GraphSettings.ClientId : _actualClientId,
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
                    MessageBox.Show(
                        $"Test email sent successfully!\n\n" +
                        $"To: {txtTestTo.Text}\n" +
                        $"Subject: {testEmail.Subject}\n\n" +
                        $"Check the recipient's inbox to verify delivery.",
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
    }
}
