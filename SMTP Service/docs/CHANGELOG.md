# SMTP to MS Graph Relay Service - Changelog

## VERSION 2.0.0 - October 2, 2025

**🚀 MAJOR UPDATE: Automated Update System**

Introduced a complete automated update system with GitHub integration. The application can now check for updates, download releases, and install them automatically with backup and rollback capabilities.

**Key Changes:**
- **GitHub Integration**: Check for updates directly from GitHub releases with authentication
- **One-Click Updates**: Download and install updates from the system tray menu
- **Automatic Installation**: PowerShell-based installer handles extraction, comparison, and file replacement
- **Smart Version Detection**: Auto-detects latest version from multiple zip files using semantic versioning
- **Service State Preservation**: Automatically stops service before update and restarts only if it was running
- **Backup & Safety**: Creates timestamped backups before replacing any files
- **Self-Update Capability**: Update script can update itself via two-stage replacement on next app start
- **Protected Files**: Never replaces smtp-config.json or user data (logs, stats, config)
- **Progress Tracking**: Visual progress bar during download with percentage display
- **Installation Logs**: Detailed timestamped logs for every update (update_YYYY-MM-DD.txt)
- **Confirmation Prompts**: Requires user confirmation before proceeding with installation
- **Administrator Support**: Automatically elevates to admin when needed
- **Error Handling**: Comprehensive error handling with colored console output (green/yellow/red)

**Update Process:**
1. Check for Updates from system tray
2. View version comparison and release notes
3. Download update with progress tracking
4. Click "Install Update" button
5. PowerShell installer runs with admin privileges
6. Automatic backup, installation, and service restart
7. Update script self-replaces on next application start if needed

**Technical Details:**
- Install-Update.ps1: Main installer script with auto-detection, backup, and rollback
- Process-Update.ps1: Analysis tool for comparing update files
- Updates stored in `updates` folder with version-numbered zips
- Backups created as `backup_VERSION_TIMESTAMP` folders
- Supports version formats: 1.4.3, 2.3, 5.0.2, etc.

**Impact:** Zero-downtime updates with automatic service management. Update process completes in under 30 seconds for typical releases.

---

## VERSION 1.4.2 - October 2, 2025

**Code Safety & Exit Behavior**

Fixed all null reference warnings to improve code safety and resolved system tray exit behavior to properly terminate the application.

**Key Changes:**
- Fixed system tray "Exit" to properly terminate application using Environment.Exit()
- Resolved all CS8602/CS8604 null reference warnings in Worker.cs, SmtpProtocolHandler.cs, and ConfigurationForm.cs
- Added null-coalescing operators and null checks throughout codebase
- Improved font initialization safety with fallback to GenericSansSerif
- Enhanced authentication data handling with null safety

**Impact:** Zero compiler warnings, more robust error handling, application now fully closes when exiting from system tray.

---

## VERSION 1.4.1 - October 2, 2025

**Statistics Tracking & User Management**

Added comprehensive email statistics tracking and reorganized user management. Track total successful/failed emails globally and per-user, with support for both authenticated users and IP addresses. Authentication is now optional by default, allowing both authenticated and unauthenticated connections.

**Key Changes:**
- Added Statistics tab showing global and per-user email metrics
- Track successful/failed email counts with timestamps
- Display current queue count in statistics
- Per-user statistics show authentication status (Yes/No)
- Moved user management to dedicated Users tab
- Authentication now optional: when disabled, accepts both authenticated and unauthenticated connections
- IP address tracking for unauthenticated connections
- Statistics persist in stats/statistics.json

**Authentication Behavior:**
- Require Authentication ENABLED: Only authenticated users can send
- Require Authentication DISABLED: Both authenticated and unauthenticated allowed (tracks by username or IP)

---

## VERSION 1.3.0 - October 1, 2025

**🔥 CRITICAL FIX: Email Encoding & MIME Support**

Fixed garbled HTML emails from Veeam and other applications by implementing complete MIME parsing with Base64/Quoted-Printable decoding, charset conversion (UTF-16/ISO-8859/Windows-1252 to UTF-8), and RFC 2047 header support. Emails now preserve formatting, tables, and special characters correctly.

**Key Changes:**
- Added full MIME header parsing and preservation
- Implemented Base64 and Quoted-Printable content decoding
- Added automatic charset detection and conversion to UTF-8
- Enhanced HTML email handling with automatic charset meta tag injection
- Updated configuration window title to show version number

**Impact:** Backward compatible, no configuration changes required. Performance impact <5ms per email.

---

## VERSION 1.2.0 - October 1, 2025

**Service Management & Security**

Added self-service installation from system tray menu and enhanced credential masking. Users can now install/remove the Windows Service without batch files, and sensitive Azure AD credentials are automatically masked in the UI.

**Key Changes:**
- Install/Remove Windows Service directly from tray menu
- Credential masking for Tenant ID and Client ID (reveals on focus)
- Dynamic tray menu based on service installation status
- Improved error handling and user feedback

---

## VERSION 1.1.4 - October 1, 2025

**Logging & UI Refinements**

Enhanced application start logging with visual separators and confirmed all UI features are working correctly (smart Save button, intelligent Cancel/Close button, change tracking).

---

## VERSION 1.1.3 - October 1, 2025

**Enhanced Configuration UI**

Improved configuration form with smart save button (disabled until changes made), intelligent Cancel/Close button, dedicated Exit Application button, comprehensive change tracking, and relocated "Show File Locations" to Application Settings tab.

---

## VERSION 1.1.2 - October 1, 2025

**🔴 CRITICAL FIX: Configuration Protection**

Fixed configuration file being overwritten during builds by implementing build exclusions, automatic backups, and overwrite protection. Configuration now persists safely across builds.

---

## VERSION 1.1.1 - October 1, 2025

**Run Mode Improvements**

Enhanced default run mode behavior with Console + Tray as default, improved logging, and better run mode source indication.

---

## VERSION 1.1.0 - October 1, 2025

**Configurable Run Modes & SMTP Protocol Fixes**

Added configurable default run modes in Application Settings tab. Fixed critical UTF-8 BOM issue causing disconnections with strict SMTP clients (Veeam). Enhanced SMTP protocol support with STARTTLS advertisement and 8BITMIME extension.

---

## VERSION 1.0.0 - September 30, 2025

**Initial Release**

Custom SMTP server implementation with Microsoft Graph API integration. Receives emails via SMTP (port 25) and relays them through MS Graph using OAuth. Features include:

- Custom SMTP server (no external libraries)
- Microsoft Graph API integration with OAuth
- Email queue with automatic retry
- DPAPI-encrypted configuration
- Windows Service support
- System tray GUI with configuration interface
- Comprehensive logging with Serilog
- Support for SMTP authentication (AUTH LOGIN/PLAIN)

Compatible with Veeam, network printers, IoT devices, and legacy applications.

---

## Known Limitations

- No STARTTLS/TLS encryption
- No attachment support
- No SMTP over SSL (port 465)
- Queue is in-memory only (lost on restart)
- No rate limiting

## Upcoming Features

- Full TLS encryption support
- Email attachment support
- Database-backed queue
- Web-based configuration
- Rate limiting
- Multiple sender accounts

---

For detailed technical information, see CHANGELOG_DETAILED.md
