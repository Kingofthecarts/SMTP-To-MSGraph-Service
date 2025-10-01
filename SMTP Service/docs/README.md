# SMTP to MS Graph Relay Service

A Windows service that receives emails via SMTP protocol and forwards them through Microsoft Graph API to Microsoft 365.

## Features

- ✅ Custom SMTP server implementation (port 25)
- ✅ SMTP authentication support
- ✅ Microsoft Graph API integration
- ✅ Email queue with retry logic
- ✅ Encrypted configuration storage
- ✅ Comprehensive logging (Serilog)
- ✅ System tray GUI for configuration
- ✅ Configurable run modes (Console, Console+Tray, Tray Only)
- ✅ Windows Service support

## Prerequisites

### .NET 9 Desktop Runtime

**IMPORTANT**: You must have the .NET 9 Desktop Runtime installed to run this application.

**Download Link**: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

- Select **"Desktop Runtime 9.0.x"** (includes ASP.NET Core Runtime)
- Choose your platform (Windows x64/x86)
- Install before running the SMTP service

To check if .NET 9 is installed, run:
```cmd
dotnet --list-runtimes
```

You should see: `Microsoft.WindowsDesktop.App 9.0.x`

### NuGet Packages Required

Install these packages via NuGet Package Manager or Package Manager Console:

```powershell
Install-Package Microsoft.Graph
Install-Package Azure.Identity
Install-Package Serilog.Extensions.Hosting
Install-Package Serilog.Sinks.File
Install-Package Serilog.Sinks.Console
Install-Package Microsoft.Extensions.Hosting.WindowsServices
Install-Package System.Security.Cryptography.ProtectedData
```

### Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Name: "SMTP Graph Relay"
5. Click **Register**
6. Note the **Application (client) ID** and **Directory (tenant) ID**
7. Go to **Certificates & secrets** > **New client secret**
8. Create a secret and copy the **Value** (you won't see it again!)
9. Go to **API permissions**
10. Click **Add a permission** > **Microsoft Graph** > **Application permissions**
11. Add **Mail.Send** permission
12. Click **Grant admin consent**

## Installation

### Step 1: Build the Project

1. Open the solution in Visual Studio
2. Restore NuGet packages
3. Build in Release mode
4. Navigate to `bin\Release\net9.0\`

### Step 2: Configure the Service

Run the application with `--tray` argument to open the configuration GUI:

```cmd
"SMTP Service.exe" --tray
```

Or double-click the executable to open the system tray application.

Configure:
- **SMTP Settings**: Port (default 25), authentication, users
- **MS Graph Settings**: Tenant ID, Client ID, Client Secret, Sender Email
- **Queue Settings**: Retry attempts and delays

Click **Save** to store the configuration.

### Step 3: Install as Windows Service

Open Command Prompt or PowerShell **as Administrator**:

```powershell
# Create the service
sc create "SMTP to Graph Relay" binPath= "C:\Path\To\SMTP Service.exe" start= auto

# Start the service
sc start "SMTP to Graph Relay"

# Check service status
sc query "SMTP to Graph Relay"
```

Alternative using PowerShell:

```powershell
New-Service -Name "SMTP to Graph Relay" -BinaryPathName "C:\Path\To\SMTP Service.exe" -StartupType Automatic
Start-Service "SMTP to Graph Relay"
```

### Step 4: Configure Firewall

Allow inbound connections on port 25:

```powershell
New-NetFirewallRule -DisplayName "SMTP Relay" -Direction Inbound -Protocol TCP -LocalPort 25 -Action Allow
```

## Run Modes

The application supports three different run modes:

### Available Modes

0. **Service/Console Mode** - Shows console window with logs (no tray icon)
1. **Console + Tray Mode (DEFAULT)** ⭐ - Console logs + system tray icon (best for monitoring)
2. **Tray Only Mode** - System tray icon only (minimal interface)

**Default Behavior**: When the configuration file is missing or on first run, the application automatically defaults to **Console + Tray mode (RunMode 1)** for the best user experience.

### Setting Your Preferred Run Mode

You can configure your preferred run mode in three ways:

#### Method 1: Configuration File (Persistent)

1. Open configuration: Run `"SMTP Service.exe" --tray` or double-click the tray icon
2. Go to the **Application Settings** tab
3. Select your preferred **Default Run Mode**:
   - **0** = Service/Console mode
   - **1** = Console + Tray mode (recommended)
   - **2** = Tray Only mode
4. Click **Save**

The application will start in your chosen mode automatically.

#### Method 2: Command Line Arguments (One-Time Override)

Command line arguments override the configuration file setting:

```cmd
# Force tray-only mode (ignores config setting)
"SMTP Service.exe" --tray

# Force console + tray mode (ignores config setting)
"SMTP Service.exe" --console
```

#### Method 3: Direct JSON Edit

Edit `config\smtp-config.json`:

```json
{
  "ApplicationSettings": {
    "RunMode": 1
  },
  ...
}
```

### Run Mode Information Display

When the application starts, it displays the active run mode:

**Console Output:**
```
========== RUN MODE: Console + Tray ==========
RunMode 1: Console with system tray icon (DEFAULT)
Source: Configuration file (RunMode=1)
==========================================
```

**Log File Output:**
```
[INF] ========================================
[INF] SMTP to Graph Relay - Service Started
[INF] Run Mode: Console + Tray (RunMode 1: Console with system tray icon (DEFAULT))
[INF] Source: Configuration file (RunMode=1)
[INF] ========================================
```

This helps with troubleshooting and verifying the application is running in your intended mode.

## Usage

### As a Service

Once installed, the service runs automatically on system startup. It:
1. Listens for SMTP connections on port 25
2. Authenticates clients using configured credentials
3. Receives emails via SMTP protocol
4. Queues emails for processing
5. Sends emails through Microsoft Graph API
6. Retries failed sends automatically
7. Logs all transactions

### System Tray Application

Run with `--tray` argument to access:
- **Configuration**: Modify settings
- **Service Status**: Check if service is running
- **View Logs**: Open log directory
- **Start/Stop/Restart Service**: Control the service
- **Exit**: Close tray application

### Testing SMTP Connection

Use any email client or command-line tool:

#### Using Telnet:

```cmd
telnet localhost 25
EHLO test.local
AUTH LOGIN
<base64 encoded username>
<base64 encoded password>
MAIL FROM: <sender@domain.com>
RCPT TO: <recipient@domain.com>
DATA
Subject: Test Email

This is a test message.
.
QUIT
```

#### Using PowerShell:

```powershell
Send-MailMessage -SmtpServer localhost -Port 25 -From "sender@domain.com" -To "recipient@domain.com" -Subject "Test" -Body "Test message" -Credential (Get-Credential)
```

## Configuration Files

### Configuration Protection 🛡️

**Your configuration file is protected and will NEVER be overwritten by builds.**

The application uses a template-based system:
- `smtp-config.template.json` - Reference template (in source code, never copied)
- `smtp-config.json` - Your actual config (created on first run, always protected)
- `smtp-config.json.backup` - Automatic backup (created before each save)

See `config/README.md` for complete details on the protection system.

### smtp-config.json

Located in the `config` subdirectory of the application directory. Contains encrypted configuration:

```json
{
  "ApplicationSettings": {
    "RunMode": 0
  },
  "SmtpSettings": {
    "Port": 25,
    "RequireAuthentication": true,
    "Credentials": [
      {
        "Username": "user1",
        "Password": "ENC:..."
      }
    ],
    "MaxMessageSizeKb": 10240,
    "EnableTls": false
  },
  "GraphSettings": {
    "TenantId": "ENC:...",
    "ClientId": "ENC:...",
    "ClientSecret": "ENC:...",
    "SenderEmail": "noreply@yourdomain.com"
  },
  "QueueSettings": {
    "MaxRetryAttempts": 3,
    "RetryDelayMinutes": 5,
    "MaxQueueSize": 1000
  },
  "LogSettings": {
    "LogLevel": "Information",
    "LogFilePath": "logs/smtp-relay.log",
    "RollingInterval": "Day"
  }
}
```

**Note**: Sensitive fields are automatically encrypted using Windows DPAPI.

### Logs

Logs are stored in the `logs/` directory:
- Rolling daily log files
- Structured logging with timestamps
- All SMTP transactions logged
- All MS Graph API calls logged
- Errors and warnings tracked

## Troubleshooting

### Service won't start

- Check logs in `logs/` directory
- Ensure port 25 is not in use: `netstat -an | find "25"`
- Verify configuration is valid
- Check Windows Event Viewer for service errors

### SMTP clients can't connect

- Verify firewall allows port 25
- Check service is running: `sc query "SMTP to Graph Relay"`
- Test locally first before remote connections
- Ensure authentication credentials are correct

### Emails not sending

- Check MS Graph configuration (Test Connection in GUI)
- Verify API permissions are granted and admin consented
- Check logs for specific error messages
- Verify sender email address exists in your tenant
- Ensure the service account has Mail.Send permission

### Permission Denied Errors

- Port 25 requires administrator privileges
- Run service as SYSTEM or administrator account
- Grant necessary permissions in Azure AD

## Uninstalling

```powershell
# Stop the service
sc stop "SMTP to Graph Relay"

# Delete the service
sc delete "SMTP to Graph Relay"

# Remove firewall rule (optional)
Remove-NetFirewallRule -DisplayName "SMTP Relay"
```

## Security Considerations

- Configuration file contains encrypted credentials (DPAPI)
- Only accessible by the user/machine that encrypted them
- SMTP authentication required by default
- Use strong passwords for SMTP users
- Keep Azure AD client secret secure
- Regularly rotate client secrets in Azure AD
- Monitor logs for unauthorized access attempts
- Consider using TLS/SSL for SMTP connections (STARTTLS)

## Architecture

See `MESSAGE_FLOW.txt` for detailed message flow diagram.

## Project Structure

```
SMTP Service/
├── Models/           # Data models
├── Services/         # Core services (SMTP, Graph, Queue)
├── Managers/         # Configuration, Queue, Protocol handlers
├── UI/              # System tray and configuration GUI
├── config/          # Configuration files
│   └── smtp-config.json # Configuration file
└── logs/            # Application logs
```

## Support

For issues or questions, check:
- Log files in `logs/` directory
- `PROJECT_NOTES.txt` for detailed documentation
- Windows Event Viewer for service-related errors

## License

[Your License Here]

## Version

1.1.0 - Run Mode Configuration Update
- Added configurable default run mode
- Application Settings tab in GUI
- Automatic mode selection on startup
