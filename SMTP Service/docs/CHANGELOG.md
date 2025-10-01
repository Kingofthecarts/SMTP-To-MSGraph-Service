# SMTP to MS Graph Relay Service - Changelog

## VERSION 1.1.0 - October 1, 2025

### NEW FEATURES

- **Added configurable default run mode**
  - Users can now set their preferred startup mode in Application Settings tab
  - Choose between Console, Console+Tray, or Tray Only modes
  
- **Application Settings tab added to configuration GUI**
  - Combined run mode and queue settings in one organized location
  
- **RunMode setting persists in configuration file**
  - Automatic mode selection on startup
  
- **Command line arguments override configured setting**
  - --console and --tray still work for one-time overrides

### BUG FIXES

- **Fixed critical UTF-8 BOM issue in SMTP greeting**
  - Was causing immediate disconnection with strict SMTP clients (Veeam, etc.)
  - Changed encoding from UTF-8 with BOM to UTF-8 without BOM
  - SMTP protocol requires ASCII-compatible encoding without byte order marks
  
- **Added proper CRLF line endings for SMTP protocol**
  - Ensures full RFC 5321 compliance

### IMPROVEMENTS

- **Enhanced SMTP logging with detailed connection tracking**
  - Millisecond timestamps for all SMTP operations
  - Connection duration tracking
  - Command count per connection
  - 30-second timeout detection
  - Data availability checking before reads
  
- **Improved error handling**
  - Specific handling for IOException and SocketException
  - Better diagnostics for connection issues
  
- **Enhanced SMTP protocol support**
  - Added STARTTLS advertisement in EHLO response
  - Added 8BITMIME extension support
  - Added STARTTLS command handler (returns "454 TLS not available")
  - Changed greeting to RFC 5321 compliant format: "ESMTP Service ready"

### DOCUMENTATION

- **Added .NET 9 Desktop Runtime requirement**
  - Added to all documentation files
  - Download links and verification instructions included
  - Made it Step 0 in setup process
  
- **Created comprehensive documentation files**
  - DOTNET_RUNTIME_REQUIREMENT.txt
  - RUN_MODE_FEATURE.txt with troubleshooting

---

## VERSION 1.0.0 - September 30, 2025 - INITIAL RELEASE

### CORE FEATURES

- **Custom SMTP server implementation**
  - Built from scratch without external SMTP libraries
  - Full protocol support: HELO, EHLO, AUTH LOGIN, AUTH PLAIN, MAIL FROM, RCPT TO, DATA, QUIT, RSET, NOOP
  - TCP listener on configurable port (default: 25)
  
- **Microsoft Graph API integration**
  - Send emails through Microsoft 365 using Graph API
  - OAuth authentication with Client Credentials flow
  - Secure app-only authentication
  
- **Email queue system**
  - Automatic retry logic with configurable attempts
  - Thread-safe in-memory queue (ConcurrentQueue)
  - Configurable retry attempts and delays

### SECURITY FEATURES

- **SMTP authentication support**
  - AUTH LOGIN and AUTH PLAIN methods
  - Base64 authentication encoding
  
- **Encrypted configuration storage**
  - Uses Windows DPAPI (Data Protection API)
  - Sensitive fields automatically encrypted (passwords, secrets, tenant info)
  - Per-machine encryption (configuration not portable)

### CONFIGURATION SYSTEM

- **JSON-based configuration** (smtp-config.json)
  - SMTP settings: Port, authentication, credentials, message size limits
  - MS Graph settings: Tenant ID, Client ID, Client Secret, Sender Email
  - Queue settings: Max retry attempts, retry delay, max queue size
  - Log settings: Log level, file path, rolling interval
  
- **Configuration GUI with tabbed interface**
  - Easy-to-use Windows Forms interface
  - Test MS Graph connection capability
  - Send test emails directly via Graph API

### LOGGING SYSTEM

- **Serilog structured logging framework**
  - Rolling daily log files
  - Console output with timestamps
  - Configurable log levels (Debug, Information, Warning, Error)
  
- **Comprehensive logging**
  - All SMTP transactions logged
  - All MS Graph API calls logged
  - Authentication attempts tracked
  - Detailed error logging with stack traces

### SYSTEM TRAY GUI

- **Windows Forms-based interface**
  - Tabbed layout: SMTP Settings, MS Graph Settings, Application Settings, Test Email, Changelog
  - Context menu with quick actions
  - Service control: Start, Stop, Restart
  - Service status checking
  - View logs functionality
  - Show file locations feature

### WINDOWS SERVICE SUPPORT

- **Runs as background Windows Service**
  - Auto-start on system boot
  - Graceful shutdown handling
  - Service lifecycle management
  - Compatible with Windows 10/11 and Windows Server

### OPERATING MODES

- **Service Mode** (default)
  - Console with logs visible
  
- **Tray Mode** (--tray)
  - System tray only
  
- **Console + Tray Mode** (--console)
  - Both console and tray

### EMAIL PROCESSING

- **Multi-recipient support**
  - Handle multiple RCPT TO commands
  
- **Email header parsing**
  - Extract Subject, From, To, Cc
  - HTML email detection (Content-Type)
  - Raw message preservation
  
- **Proper email handling**
  - Email body extraction
  - Header/body separation (handles both CRLF and LF)

### SMTP PROTOCOL COMPLIANCE

- **RFC 5321 compliant implementation**
  - Supported commands: HELO, EHLO, AUTH, MAIL FROM, RCPT TO, DATA, RSET, NOOP, QUIT
  - Proper response codes: 220, 221, 235, 250, 334, 354, 454, 500, 501, 503, 530, 535
  - Multi-line EHLO responses with capability advertisement
  - SIZE extension support
  - Proper error handling and response codes

### ERROR HANDLING

- **Enhanced Azure AD error detection**
  - Client secret expiration detection (AADSTS7000215)
  - Invalid tenant ID detection (AADSTS90002)
  - Invalid client ID detection (AADSTS700016)
  - Unauthorized client detection (unauthorized_client)
  - Detailed error messages with troubleshooting guidance
  - Automatic retry on transient failures

### COMPATIBILITY

- **Works with any SMTP client**
  - Tested with: Telnet, PuTTY, PowerShell, Veeam
  - Compatible with enterprise applications
  - Network printers (scan-to-email)
  - IoT devices
  - Legacy applications

### DOCUMENTATION

- **Complete documentation suite**
  - README.md with installation instructions
  - README.txt for quick start
  - SETUP_CHECKLIST.txt with detailed steps
  - PROJECT_NOTES.txt with technical details
  - PROJECT_COMPLETE.txt with summary
  - MESSAGE_FLOW.txt with architecture diagram
  - Installation batch script (install-service.bat)

---

## KNOWN LIMITATIONS

- ❌ No STARTTLS/TLS encryption (SMTP is unencrypted)
- ❌ No email attachments support (Graph API supports it, not implemented)
- ❌ No SMTP over SSL (port 465)
- ❌ No user mapping (all emails sent as configured sender)
- ❌ No SPF/DKIM validation
- ❌ No rate limiting
- ❌ Queue is in-memory only (lost on restart)
- ❌ No web-based configuration interface

---

## UPCOMING FEATURES (Planned)

- 🔜 Full STARTTLS/TLS encryption support
- 🔜 Email attachment support
- 🔜 Database-backed queue for persistence
- 🔜 Web-based configuration interface
- 🔜 Rate limiting per user/IP
- 🔜 Domain whitelisting/blacklisting
- 🔜 Multiple sender account support
- 🔜 Real-time statistics dashboard
- 🔜 Advanced monitoring and alerting

---

## Additional Documentation

For detailed technical information, see:
- **PROJECT_NOTES.txt** - Implementation details
- **MESSAGE_FLOW.txt** - Architecture diagram
- **README.md** - User documentation
- **SETUP_CHECKLIST.txt** - Installation guide
