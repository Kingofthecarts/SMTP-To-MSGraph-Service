using System;
using System.Drawing;
using System.Windows.Forms;

namespace SMTP_Service.UI
{
    public class UpdateProgressDialog : Form
    {
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblPercentage = null!;
        private Button btnCancel = null!;
        private Button btnInstall = null!;
        
        public bool DownloadComplete { get; private set; }
        public bool InstallRequested { get; private set; }
        
        public UpdateProgressDialog()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            Text = "Downloading Update";
            Size = new Size(400, 180);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            
            // Status label
            lblStatus = new Label
            {
                Text = "Downloading update...",
                Location = new Point(20, 20),
                Size = new Size(350, 20)
            };
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 50),
                Size = new Size(350, 25),
                Style = ProgressBarStyle.Continuous
            };
            
            // Percentage label
            lblPercentage = new Label
            {
                Text = "0%",
                Location = new Point(20, 80),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Cancel button
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(150, 110),
                Size = new Size(100, 25),
                DialogResult = DialogResult.Cancel
            };
            
            // Install button (initially hidden)
            btnInstall = new Button
            {
                Text = "Install Update",
                Location = new Point(100, 110),
                Size = new Size(100, 25),
                Visible = false,
                BackColor = Color.LightGreen
            };
            btnInstall.Click += (s, e) =>
            {
                InstallRequested = true;
                DialogResult = DialogResult.OK;
            };
            
            // Close button (shown after download)
            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(210, 110),
                Size = new Size(90, 25),
                Visible = false
            };
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;
            
            Controls.Add(lblStatus);
            Controls.Add(progressBar);
            Controls.Add(lblPercentage);
            Controls.Add(btnCancel);
            Controls.Add(btnInstall);
            Controls.Add(btnClose);
            
            // Handle cancel
            FormClosing += (s, e) =>
            {
                if (!DownloadComplete && e.CloseReason == CloseReason.UserClosing)
                {
                    if (MessageBox.Show("Cancel the download?", "Cancel Download", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            };
        }
        
        public void UpdateProgress(int percentage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(UpdateProgress), percentage);
                return;
            }
            
            progressBar.Value = Math.Min(100, Math.Max(0, percentage));
            lblPercentage.Text = $"{percentage}%";
            
            if (percentage >= 100)
            {
                CompleteDownload();
            }
        }
        
        public void CompleteDownload()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(CompleteDownload));
                return;
            }
            
            DownloadComplete = true;
            lblStatus.Text = "Download complete! Ready to install.";
            lblStatus.ForeColor = Color.Green;
            progressBar.Value = 100;
            lblPercentage.Text = "100% - Complete";
            
            // Show install button, hide cancel
            btnCancel.Visible = false;
            btnInstall.Visible = true;
            
            // Also show close button if they don't want to install now
            var btnClose = new Button
            {
                Text = "Install Later",
                Location = new Point(210, 110),
                Size = new Size(90, 25)
            };
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnClose);
        }
        
        public void ShowError(string error)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowError), error);
                return;
            }
            
            lblStatus.Text = $"Error: {error}";
            lblStatus.ForeColor = Color.Red;
            btnCancel.Text = "Close";
        }
    }
}
