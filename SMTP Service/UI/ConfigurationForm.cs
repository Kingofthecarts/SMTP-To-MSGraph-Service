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

        public ConfigurationForm()
        {
            _configManager = new Managers.ConfigurationManager();
            _config = _configManager.LoadConfiguration();
            
            InitializeComponents();
            LoadConfiguration();
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
                Location = new System.Drawing.Point(300, 10),
                Size = new System.Drawing.Size(80, 30)
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(390, 10),
                Size = new System.Drawing.Size(80, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            btnTest = new Button
            {
                Text = "Test Connection",
                Location = new System.Drawing.Point(480, 10),
                Size = new System.Drawing.Size(100, 30)
            };
            btnTest.Click += BtnTest_Click;

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnTest);

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

            // Instructions
            var lblInstructions = new Label
            {
                Text = "To configure MS Graph:\n" +
                       "1. Register an app in Azure AD\n" +
                       "2. Grant Mail.Send permission\n" +
                       "3. Create a client secret\n" +
                       "4. Copy Tenant ID, Client ID, and Secret here",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(500, 100),
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

            // Load Graph settings
            txtTenantId.Text = _config.GraphSettings.TenantId;
            txtClientId.Text = _config.GraphSettings.ClientId;
            txtClientSecret.Text = _config.GraphSettings.ClientSecret;
            txtSenderEmail.Text = _config.GraphSettings.SenderEmail;

            // Load Queue settings
            numMaxRetry.Value = _config.QueueSettings.MaxRetryAttempts;
            numRetryDelay.Value = _config.QueueSettings.RetryDelayMinutes;

            // Load Application settings
            cmbRunMode.SelectedIndex = _config.ApplicationSettings.RunMode;
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
        }

        private void BtnRemoveUser_Click(object? sender, EventArgs e)
        {
            if (lstUsers.SelectedIndex >= 0)
            {
                var username = lstUsers.SelectedItem?.ToString();
                _config.SmtpSettings.Credentials.RemoveAll(c => c.Username == username);
                lstUsers.Items.RemoveAt(lstUsers.SelectedIndex);
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

                _config.GraphSettings.TenantId = txtTenantId.Text.Trim();
                _config.GraphSettings.ClientId = txtClientId.Text.Trim();
                _config.GraphSettings.ClientSecret = txtClientSecret.Text;
                _config.GraphSettings.SenderEmail = txtSenderEmail.Text.Trim();

                _config.QueueSettings.MaxRetryAttempts = (int)numMaxRetry.Value;
                _config.QueueSettings.RetryDelayMinutes = (int)numRetryDelay.Value;

                _config.ApplicationSettings.RunMode = cmbRunMode.SelectedIndex;

                // Save configuration
                _configManager.SaveConfiguration(_config);

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
                if (string.IsNullOrWhiteSpace(txtTenantId.Text))
                {
                    MessageBox.Show("Tenant ID is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(txtClientId.Text))
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
                    TenantId = txtTenantId.Text.Trim(),
                    ClientId = txtClientId.Text.Trim(),
                    ClientSecret = txtClientSecret.Text.Trim(),
                    SenderEmail = txtSenderEmail.Text.Trim()
                };

                Serilog.Log.Information("Testing MS Graph connection...");
                Serilog.Log.Information($"Tenant ID: {settings.TenantId}");
                Serilog.Log.Information($"Client ID: {settings.ClientId}");
                Serilog.Log.Information($"Sender Email: {settings.SenderEmail}");

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
                var graphService = new Services.GraphEmailService(
                    logger,
                    new GraphSettings
                    {
                        TenantId = txtTenantId.Text.Trim(),
                        ClientId = txtClientId.Text.Trim(),
                        ClientSecret = txtClientSecret.Text.Trim(),
                        SenderEmail = txtSenderEmail.Text.Trim()
                    }
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
