================================================================================
SMTP TO MS GRAPH RELAY SERVICE - README
================================================================================
Version 1.0.0

WHAT IS THIS?
=============
This application receives emails via standard SMTP protocol (port 25) and 
forwards them through Microsoft Graph API to Microsoft 365. It acts as a 
bridge between devices/applications that can only send via SMTP and your 
modern cloud-based Microsoft 365 email system.

PERFECT FOR:
• Legacy devices that only support SMTP
• Network printers sending scan-to-email
• Applications that require SMTP relay
• IoT devices sending notifications
• Any system that needs to send email through Microsoft 365


QUICK START GUIDE
=================

STEP 1: PREREQUISITES
--------------------
Before running this application, you need:

✓ Windows computer (Windows 10/11 or Windows Server)
✓ Administrator privileges (required for port 25)
✓ .NET 9 Desktop Runtime (REQUIRED - see below)
✓ Microsoft 365 subscription
✓ Azure AD App Registration with Mail.Send permission

**CRITICAL: Install .NET 9 Desktop Runtime**

The application requires .NET 9 Desktop Runtime to run.

Download from: https://dotnet.microsoft.com/download/dotnet/9.0

1. Click "Download .NET Desktop Runtime 9.0.x"
2. Choose Windows x64 or x86 (match your system)
3. Run the installer
4. Restart your computer if prompted

To verify installation, open Command Prompt and run:
  dotnet --list-runtimes

You should see: Microsoft.WindowsDesktop.App 9.0.x

If you don't have an Azure AD app set up yet, see "AZURE AD SETUP" below.


STEP 2: FIRST RUN - CONFIGURE THE SERVICE
-----------------------------------------
1. Right-click "SMTP Service.exe" and select "Run as administrator"
   (or open Command Prompt as admin and run: "SMTP Service.exe" --tray)

2. A system tray icon will appear in the bottom-right of your screen
   (look for the application icon near the clock)

3. Double-click the tray icon to open the configuration window

4. Go to the "MS Graph Settings" tab and enter:
   - Tenant ID (from Azure Portal)
   - Client ID (Application ID from App Registration)
   - Client Secret (from App Registration - Certificates & secrets)
   - Sender Email (a valid email in your Microsoft 365 tenant)

5. Click "Test Connection" to verify your credentials work

6. Go to the "SMTP Settings" tab (optional):
   - Change port if needed (default: 25)
   - Add SMTP users if you want authentication
   - Set "Require Authentication" if needed

7. Go to the "Application Settings" tab:
   - Select your preferred "Default Run Mode":
     * Service/Console Mode (0) - Shows console with logs
     * Console with Tray Icon (1) - Console + tray (best for monitoring)
     * Tray Only (2) - Minimal interface
   - Adjust queue settings if needed

8. Click "Save"


STEP 3: INSTALL AS WINDOWS SERVICE (Recommended)
------------------------------------------------
For the service to run automatically on startup:

Option A - Use the installation script:
   1. Right-click "install-service.bat"
   2. Select "Run as administrator"
   3. Choose option 1 to install
   4. Choose option 9 to add firewall rule
   5. Choose option 2 to start the service

Option B - Manual installation:
   1. Open Command Prompt as Administrator
   2. Navigate to the folder containing SMTP Service.exe
   3. Run: sc create "SMTP to Graph Relay" binPath= "C:\Full\Path\To\SMTP Service.exe" start= auto
   4. Run: sc start "SMTP to Graph Relay"
   5. Run: netsh advfirewall firewall add rule name="SMTP Relay" dir=in action=allow protocol=TCP localport=25


STEP 4: TEST IT
--------------
Option A - Send a test email from the GUI:
   1. Double-click the tray icon
   2. Go to "Test Email" tab
   3. Enter your email address
   4. Click "Send Test Email"
   5. Check your inbox!

Option B - Test via Telnet:
   1. Open Command Prompt
   2. Run: telnet localhost 25
   3. You should see: 220 [hostname] SMTP Ready
   4. Type: QUIT

Option C - Send a real email via SMTP:
   Configure your device/application to use:
   - SMTP Server: [your-computer-name] or [your-ip-address]
   - Port: 25
   - Authentication: (if you enabled it)


CONFIGURING DEFAULT RUN MODE
============================

★ NEW FEATURE: Set How the Application Starts ★

You can now configure how the application runs by default, without needing
to remember command line arguments every time!

THREE RUN MODES AVAILABLE:

1. SERVICE/CONSOLE MODE (Default - RunMode 0)
   "SMTP Service.exe"
   - Shows console window with real-time logs
   - Best for troubleshooting and debugging
   - See everything that's happening

2. CONSOLE + TRAY MODE (RunMode 1)
   "SMTP Service.exe" --console
   - Console window with logs PLUS system tray icon
   - Best of both worlds!
   - Great for development and monitoring
   - Quick access to configuration via tray

3. TRAY ONLY MODE (RunMode 2)
   "SMTP Service.exe" --tray
   - System tray icon only, no console window
   - Minimal interface
   - Best for production use
   - Clean and unobtrusive

HOW TO SET YOUR PREFERRED MODE:

1. Open configuration:
   - Run "SMTP Service.exe" --tray
   - Double-click the system tray icon

2. Go to "Application Settings" tab

3. Select your preferred "Default Run Mode" from the dropdown

4. Click "Save"

5. From now on, just run "SMTP Service.exe" and it will automatically
   start in your chosen mode!

OVERRIDING THE DEFAULT:

Command line arguments still work and override the configured setting:
- "SMTP Service.exe" --tray     = Force tray-only mode (override config)
- "SMTP Service.exe" --console  = Force console+tray mode (override config)
- "SMTP Service.exe"            = Use configured RunMode setting


THREE WAYS TO RUN
=================

1. SERVICE MODE (No arguments)
   "SMTP Service.exe"
   - Shows console window with all logs
   - Best for troubleshooting

2. TRAY MODE (Recommended for production)
   "SMTP Service.exe" --tray
   - System tray icon only
   - No console window
   - Minimal interface

3. CONSOLE + TRAY MODE (Best for monitoring)
   "SMTP Service.exe" --console
   - Console window with real-time logs
   - System tray icon for configuration
   - See everything that's happening


AZURE AD APP REGISTRATION SETUP
================================

You need to create an App Registration in Azure AD to use this service.

STEP 1: Register the Application
--------------------------------
1. Go to https://portal.azure.com
2. Navigate to "Azure Active Directory" or "Microsoft Entra ID"
3. Click "App registrations" in the left menu
4. Click "New registration"
5. Enter a name: "SMTP Graph Relay"
6. Select "Accounts in this organizational directory only"
7. Click "Register"

STEP 2: Get Your IDs
-------------------
On the Overview page, copy these values:
- Application (client) ID → This is your "Client ID"
- Directory (tenant) ID → This is your "Tenant ID"

STEP 3: Create a Client Secret
------------------------------
1. Click "Certificates & secrets" in the left menu
2. Click "New client secret"
3. Enter a description: "SMTP Relay Secret"
4. Select expiration (24 months recommended)
5. Click "Add"
6. IMPORTANT: Copy the "Value" immediately → This is your "Client Secret"
   (You won't be able to see it again!)

STEP 4: Add API Permissions
---------------------------
1. Click "API permissions" in the left menu
2. Click "Add a permission"
3. Select "Microsoft Graph"
4. Select "Application permissions" (NOT Delegated!)
5. Search for "Mail.Send"
6. Check the box next to "Mail.Send"
7. Click "Add permissions"

STEP 5: Grant Admin Consent
---------------------------
1. Click "Grant admin consent for [Your Organization]"
2. Click "Yes" to confirm
3. Wait for the status to show "Granted for [Your Organization]"

Done! Now you have:
- Tenant ID
- Client ID
- Client Secret
- Mail.Send permission with admin consent

Enter these in the application's "MS Graph Settings" tab.


SYSTEM TRAY MENU
================

Right-click the system tray icon to access:

• Configuration - Open settings window
• Service Status - Check if service is running/installed
• View Logs - Open the logs folder
• Show File Locations - See where all files are stored
• Start/Stop/Restart Service - Control the Windows Service
• Exit - Close the tray application


CONFIGURATION TABS
==================

SMTP Settings:
- Port (default: 25)
- Require Authentication (checkbox)
- Add/Remove authorized users
- Message size limit

MS Graph Settings:
- Tenant ID (from Azure AD)
- Client ID (Application ID)
- Client Secret
- Sender Email
- Test Connection button

Application Settings:
- Default Run Mode (dropdown):
  * Service/Console Mode (0) - Console with logs
  * Console with Tray Icon (1) - Console + tray (best for monitoring)
  * Tray Only (2) - Tray icon only
- Max retry attempts (default: 3)
- Retry delay in minutes (default: 5)

Test Email:
- Send a test email directly via MS Graph
- Verify your configuration works
- No SMTP required


TROUBLESHOOTING
===============

ISSUE: "Port 25 requires administrator privileges"
SOLUTION: Run as administrator or install as a service

ISSUE: "Cannot connect to localhost:25"
CHECK:
  ✓ Service is running (check tray icon or service status)
  ✓ Windows Firewall allows port 25
  ✓ No other application is using port 25
  
To check what's using port 25:
  netstat -ano | findstr :25

ISSUE: "Authentication Failed" when testing MS Graph
SOLUTIONS:
  ✓ Verify Tenant ID is correct (Azure Portal → Azure AD → Overview)
  ✓ Verify Client ID is correct (App Registrations → Your App → Overview)
  ✓ Check if Client Secret expired (common issue!)
  ✓ Create a new client secret if needed
  ✓ Ensure Mail.Send permission is granted with admin consent

ISSUE: "Email queued but not sent"
CHECK LOGS: Right-click tray icon → View Logs
COMMON CAUSES:
  ✓ Client secret expired
  ✓ Sender email doesn't exist in your tenant
  ✓ No internet connection
  ✓ API permissions not granted

ISSUE: "MS Graph connection failed"
CHECK:
  ✓ All credentials are entered correctly
  ✓ No extra spaces in Tenant ID, Client ID, or Secret
  ✓ App has Mail.Send application permission (not delegated)
  ✓ Admin consent was granted
  ✓ Client secret hasn't expired

ISSUE: "Service won't start"
CHECK:
  ✓ Run Command Prompt as Administrator
  ✓ Check Windows Event Viewer for errors
  ✓ View logs in the application folder
  ✓ Ensure port 25 is not already in use

ISSUE: Client secret expired
SOLUTION:
  1. Go to Azure Portal → App Registrations → Your App
  2. Click "Certificates & secrets"
  3. Delete old expired secret
  4. Create new client secret
  5. Copy the new secret Value
  6. Update in the application (MS Graph Settings tab)
  7. Click Save


VIEWING LOGS
============

Logs are stored in the "logs" folder next to the executable.

To view logs:
1. Right-click the tray icon
2. Select "View Logs"
3. Open the latest .log file

Logs contain:
- All SMTP connections and commands
- Authentication attempts
- Emails queued and sent
- MS Graph API calls
- Errors and warnings with details

Log files rotate daily to prevent unlimited growth.


FILE LOCATIONS
==============

All files are stored in the same folder as SMTP Service.exe:

SMTP Service.exe       - The main application
smtp-config.json       - Your settings (encrypted)
logs\                  - Log files folder
  smtp-relay-YYYYMMDD.log - Daily log files

To see exact paths:
Right-click tray icon → "Show File Locations"


UNINSTALLING
============

If installed as a service:
1. Run install-service.bat as Administrator
2. Choose option 5 to uninstall
3. OR manually run: sc delete "SMTP to Graph Relay"

Remove firewall rule:
  netsh advfirewall firewall delete rule name="SMTP Relay"

Delete files:
  Simply delete the folder containing SMTP Service.exe


SECURITY NOTES
==============

✓ Configuration file (smtp-config.json) is encrypted using Windows DPAPI
✓ Encrypted values only work on the computer where they were created
✓ Client secrets expire - monitor and renew them in Azure Portal
✓ SMTP authentication is optional but recommended
✓ Only grant Mail.Send permission (not full mailbox access)
✓ Review logs regularly for unauthorized access attempts
✓ Keep the application updated


SMTP CLIENT CONFIGURATION
==========================

Configure your devices/applications with these settings:

SMTP Server: [your-computer-name] or [your-ip-address]
Port: 25
Security: None (or STARTTLS if you enabled it)
Authentication: Optional (depends on your configuration)
  Username: [from your SMTP Settings]
  Password: [from your SMTP Settings]


PERFORMANCE & LIMITS
====================

• Concurrent connections: Unlimited (limited by system resources)
• Queue size: 1000 emails (configurable)
• Max message size: 10 MB (configurable)
• Retry attempts: 3 (configurable)
• Retry delay: 5 minutes (configurable)
• Log retention: Daily rotation, manual cleanup


SUPPORTED SMTP COMMANDS
========================

✓ HELO/EHLO - Handshake
✓ AUTH LOGIN - Authentication
✓ AUTH PLAIN - Plain authentication
✓ MAIL FROM - Sender address
✓ RCPT TO - Recipient address (multiple supported)
✓ DATA - Email content
✓ RSET - Reset transaction
✓ NOOP - No operation
✓ QUIT - Close connection


WHAT'S NOT SUPPORTED
====================

✗ STARTTLS (can be added if needed)
✗ Email attachments (Graph API supports it, not implemented)
✗ SMTP over SSL (port 465)
✗ Advanced SMTP features (VRFY, EXPN, etc.)


COMMAND LINE OPTIONS
====================

SMTP Service.exe              Run in service mode (console with logs)
SMTP Service.exe --tray       Run with system tray only
SMTP Service.exe --console    Run with console logs AND system tray


SUPPORT & DOCUMENTATION
=======================

For more detailed information, see:
- PROJECT_NOTES.txt - Complete technical documentation
- MESSAGE_FLOW.txt - Visual flow diagram and architecture
- SETUP_CHECKLIST.txt - Detailed setup steps

Check logs for detailed error information:
Right-click tray icon → View Logs


COMMON USE CASES
================

✓ Network printers sending scan-to-email
✓ NAS devices sending notifications
✓ Legacy business applications requiring SMTP
✓ IoT devices sending alerts
✓ Monitoring systems sending reports
✓ Backup software sending job status
✓ Security cameras sending motion alerts
✓ Any device/app that needs email via SMTP


TECHNICAL DETAILS
=================

Built with: .NET 9.0
UI Framework: Windows Forms
Authentication: OAuth 2.0 Client Credentials Flow
API: Microsoft Graph v1.0
Logging: Serilog
Encryption: Windows DPAPI (Data Protection API)


VERSION HISTORY
===============

v1.0.0 - Initial Release
  • Custom SMTP server implementation
  • Microsoft Graph integration
  • System tray GUI with tabbed interface
  • Three operation modes
  • Test email functionality
  • Enhanced error detection
  • Comprehensive logging


LICENSE
=======

[Your License Here]


CREDITS
=======

Developed with assistance from Claude (Anthropic AI)
Microsoft Graph API: https://graph.microsoft.com
SMTP Protocol: RFC 5321


CONTACT & UPDATES
=================

For updates, issues, or questions:
[Your contact information or website]


================================================================================
Thank you for using SMTP to MS Graph Relay Service!
================================================================================

Quick Start Checklist:
[ ] Azure AD App Registration created
[ ] Tenant ID, Client ID, and Client Secret obtained
[ ] Mail.Send permission granted with admin consent
[ ] Application configured (MS Graph Settings tab)
[ ] Test Connection successful
[ ] Service installed (install-service.bat)
[ ] Firewall rule added
[ ] Test email sent successfully
[ ] Devices configured to use this SMTP server

Need help? Check the logs: Right-click tray icon → View Logs

================================================================================
