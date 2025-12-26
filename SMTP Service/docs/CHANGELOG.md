# SMTP to MS Graph Relay Service - Changelog

## VERSION 5.0.2 - December 26, 2025

### 🐛 Bug Fixes

- Fixed `--resume` argument parsing - was incorrectly consuming `--mode` as version parameter
- Added default repository fallback (`Kingofthecarts/SMTP-To-MSGraph-Service`) when git.json is missing or invalid
- Auto-detect encrypted/base64 config values and use defaults instead
- Removed unused `GitConfigDecryptor.cs` helper from SMTPServiceUpdater

---

## VERSION 5.0.1 - December 26, 2025

### 🧹 Code Cleanup

Cleanup from 5.0.0 update cycle - removed unused authentication code.

---

## VERSION 5.0.0 - December 26, 2025

### 🎯 Public GitHub Updates

Switched update system to public GitHub releases.

- Updates from: `Kingofthecarts/SMTP-To-MSGraph-Service`
- Removed PowerShell scripts (`Install-Update.ps1`, `Setup-GitHubToken.ps1`, `Test-GitHubConfig.ps1`)
- `SMTPServiceUpdater.exe` handles all updates
- Simple `git.json` with just repo owner/name

---

## VERSION 4.2.7 - October 5, 2025

### 🔧 Improvements

**SMTP Service Updater - Unified Update Logging**
- Unified update logging across self-updates - complete process now in single log file
- GUI resume mode after self-update preserves installation state and log continuity
- Bridge script now logs operations to same file with clear "RESUMED AFTER SELF-UPDATE" separator
- Added --mode and --log-file command-line parameters for advanced resume control

### Impact
Users can now see the entire update process including self-updates in one continuous log file. GUI automatically resumes where it left off after self-update with Install button pre-enabled.

---

## VERSION 4.2.6 - October 5, 2025

### 🔧 Improvements

**SMTP Service Updater - UI Enhancements**
- Added "Clean Logs" button to remove update log files separately from update packages
- Configuration tab now displays scheduled update settings from config file

### Impact
Better control over log file management. Users can now see their scheduled update configuration at a glance in the Configuration tab.

---

## VERSION 4.2.5 - October 5, 2025

### 🔧 Improvements

**SMTP Service Updater - Self-Update Process**
- Fixed application not exiting during self-update, causing file lock conflicts
- Increased bridge script wait time to 5 seconds for reliable file lock release
- Bridge script now preserves GUI/auto mode when relaunching after self-update
- Simplified --resume flag to auto-detect version (no longer requires version parameter)

**SMTP Service Updater - Service Management**
- Added Windows Service detection and proper shutdown before process termination
- Enhanced SMTP Service stopping: checks Windows Service first, then kills lingering processes
- Service controller now uses ServiceController API for graceful Windows Service shutdown

### Impact
Self-updates now complete reliably without file locking issues. Windows Service installations are handled properly with graceful shutdown before updates.

---

## VERSION 4.2.4 - October 5, 2025

### 🔧 Improvements

**SMTP Service Updater**
- Fixed self-update detection to occur before file operations, preventing "file in use" errors
- Self-update files now excluded from normal operations when bridge script is used
- Updated file operation logging to show full destination paths instead of relative paths
- Improved error messages with complete file paths for better troubleshooting
- Added centralized version control via AppVersion class for easier version management
- Updated GUI to display both updater version and SMTP Service version in title bar and progress tab
- Fixed application not exiting during self-update, causing file lock conflicts
- Increased bridge script wait time to 5 seconds for reliable file lock release
- Bridge script now preserves GUI/auto mode when relaunching after self-update
- Simplified --resume flag to auto-detect version (no longer requires version parameter)
- Added Windows Service detection and proper shutdown before process termination
- Enhanced SMTP Service stopping: checks Windows Service first, then kills lingering processes

### Impact
Self-updates now work reliably without file locking issues. Better troubleshooting with full paths in logs and version information clearly displayed in the UI.

---

## VERSION 4.2.3 - October 5, 2025

### 🔧 Improvements

**SMTP Service Updater UI**
- Fixed download progress logging to show only milestone percentages (25%, 50%, 75%, 100%)
- Added "Clean Updates" button to purge downloaded update files from updates folder
- Fixed duplicate "Download complete" log messages
- Ensured 100% progress appears after 75% milestone for correct ordering

### Impact
Cleaner update logs with less noise. Users can now easily clean up old update files to free disk space.

---

## VERSION 4.2.2 - October 5, 2025

### 🔧 Improvements

**Update Checker Logging**
- Update checker now logs when running the latest version and still checking for updates
- Log message: "Already running latest version - checked for update anyway"
- Provides clarity that the update check ran even when no update is available

### Impact
Better visibility into update check behavior. Users can confirm that update checks are happening even when already on the latest version.

---

## VERSION 4.2.1 - October 5, 2025

### 🐛 Bug Fixes

**Configuration UI**
- Fixed compilation error in ConfigurationForm.cs related to UpdateCheckFrequency enum
- Resolved CS0246 error preventing application from building

### Impact
Application now compiles successfully. No functional changes to runtime behavior.

---

## VERSION 4.2.0 - October 5, 2025

### ✨ New Features

**New C# GUI Updater (In Development)**
- Complete rewrite of update system from PowerShell to C#
- GUI with Download and Install buttons for user-friendly updates
- GitHub integration for automatic update downloads
- Real-time progress display and color-coded logging
- Command-line modes for automation (-a auto, -c console)
- **Note:** PowerShell updater (Install-Update.ps1) still included and functional
- GUI updater currently in testing phase

### 🔧 Improvements

**Build System**
- Fixed SMTPServiceUpdater integration with publish process
- Corrected framework path references (net9.0-windows)
- Release builds now properly include updater executable

### 📦 Technical

- New SMTPServiceUpdater.exe alongside existing Install-Update.ps1
- Both updaters fully functional during transition period
- Future versions will phase out PowerShell script

---

## VERSION 4.0.1 - October 4, 2025

### 🐛 Bug Fixes

**Build System Cleanup**
- Fixed compilation errors caused by duplicate class definitions
- Resolved temp file conflicts during build process
- Improved project organization for cleaner builds

**Impact:** Project now builds successfully without errors. No functional changes to the application.

---

## VERSION 4.0.0 - October 3, 2025

### 🎯 Major Release: SMTP Flow Control & Network Configuration

This major version introduces real-time SMTP flow control, configurable network binding, and improved runtime state management integrated with the existing configuration system.

### ✨ New Features

**SMTP Flow Control System**
- **Real-time Halt/Resume**: Stop and start SMTP server without restarting the service
  - Instant rejection of connections when halted (421 Service Temporarily Unavailable)
  - Console output: "HALT SMTP" (red) and "FLOW SMTP" (green) with color-coded status
  - Available in Configuration UI, System Tray menu, and through IPC commands
  - Runtime state persists during session, saved preference for startup
  - Separate "Enable on Startup" configuration from runtime toggle

**Network Binding Configuration**
- **Configurable Interface Selection**: Control which network interface accepts SMTP connections
  - **0.0.0.0** - All Interfaces (default) - accepts remote connections
  - **127.0.0.1** - Localhost Only - restricts to local connections for security
  - Changes require service restart to take effect
  - Configuration saved in existing `smtp-config.json` file

**Send Delay Configuration**
- **Adjustable Processing Delay**: Control the pace of email processing
  - Configurable delay between sending emails (100-10000ms)
  - Helps prevent overwhelming downstream servers
  - Applied during queue processing

**Inter-Process Communication (IPC)**
- **Named Pipes Infrastructure**: Enables real-time communication between UI and service
  - Control flow state from any UI instance
  - Query service status without direct service access
  - Works in both same-process and separate-process modes
  - Foundation for future remote management capabilities

### 📦 Configuration Management

**Integrated Configuration**
- **Flow control settings integrated into existing SmtpSettings**: No separate configuration file needed
  ```json
  {
    "SmtpSettings": {
      "BindAddress": "0.0.0.0",
      "SendDelayMs": 1000,
      "SmtpFlowEnabled": true
    }
  }
  ```
- **Runtime vs Saved State**: Flow toggle affects current session only until explicitly saved
- **Automatic Backup**: Creates `smtp-config.json.backup` when saving configuration
- **Startup Preference**: "Enable on Startup" checkbox controls whether flow starts enabled

### 🔧 Technical Improvements

- **SmtpFlowControl Singleton**: Centralized flow control with event system in `Flow` namespace
- **Integrated with existing ConfigurationManager**: Uses project's existing configuration system
- **CommandListenerService**: Named pipe server for IPC commands
- **ServiceCommandClient**: Helper for sending commands to running service
- **Improved UI State Management**: Runtime state separated from saved configuration
- **Color-coded Console Output**: Visual feedback for flow state changes (using Serilog)

### 🎨 User Interface Updates

- **Flow Control Section**: New UI section in SMTP Settings tab
  - Live status indicator (RUNNING/HALTED with color coding)
  - Toggle button for immediate control
  - "Enable on Startup" checkbox for saved preference
- **Tray Menu Integration**: Quick access to flow control from system tray
  - "Halt SMTP Flow" / "Resume SMTP Flow" with confirmation dialogs
  - Status updates reflected in tooltip

### 🛡️ Safety & Reliability

- **Graceful Degradation**: Service continues if flow control initialization fails
- **Uses existing configuration system**: No additional dependencies or configuration files
- **Backup on Save**: Automatic backup of configuration before changes
- **Default Values**: Sensible defaults in existing SmtpSettings class

---

## VERSION 3.5.0 - October 3, 2025

### ✨ New Features

**Auto-Update Configuration UI**
- **Complete Update Settings Section**: Added comprehensive auto-update configuration in the Application Settings tab
  - Enable/disable automatic updates with a master toggle
  - Configure check frequency: Daily or Weekly schedules
  - Set specific check time (default: 2:00 AM)
  - Select day of week for weekly checks (Sunday through Saturday)
  - Auto-download: Automatically download updates when available
  - Auto-install: Automatically install downloaded updates (requires auto-download enabled)
  - Check on startup: Option to check for updates when the service starts
- **Update Status Tracking**: Real-time display of update activity
  - Last Check: Timestamp of most recent update check
  - Last Download: Timestamp of most recent update download
  - Last Installed Version: Version number of the most recently installed update
- **Smart UI Behavior**: 
  - All update controls automatically disable when auto-updates are turned off
  - Weekly day selector only appears when Weekly frequency is selected
  - Auto-install checkbox only enables when auto-download is checked
  - Dependency-aware control states prevent invalid configurations

### 🔧 Improvements

**Scrollable Configuration Tabs**
- Added `AutoScroll = true` to all configuration tabs
- Prevents content from being cut off at the bottom of tabs
- Automatic scrollbars appear when content exceeds visible area
- All settings now accessible regardless of screen resolution

### 📦 Technical

- Update settings fully integrated with `UpdateSettings` model in `AppConfig`
- Settings persist across application restarts in smtp-config.json
- `ScheduledUpdateService` reads configuration to manage automatic update checks
- Change tracking ensures Save button enables when any update setting is modified

### Impact

Users can now fully configure the automatic update system through the GUI without editing configuration files. The scheduled update service respects all configured settings for checking, downloading, and installing updates automatically.

---

## VERSION 3.4.0 - October 3, 2025

### ✨ New Features

**Enhanced Statistics Monitoring**
- **Real-time System Metrics**: The Statistics tab now displays live CPU usage, active memory consumption, and application uptime
  - CPU usage updates every second for responsive monitoring
  - Memory usage and uptime refresh every 5 seconds
  - Email statistics automatically refresh every 10 seconds to catch new events
- **Min/Max Tracking**: Hover your mouse over CPU or Memory values to see minimum and maximum values since the app started
  - Provides quick insight into performance trends without cluttering the interface
  - Values reset when you close the Configuration window
- **Automatic Performance Logging**: System statistics are automatically written to the log file every 10 minutes
  - Includes current, minimum, and maximum CPU and memory usage
  - Records uptime for troubleshooting and performance analysis
  - Example log entry: `System Stats - CPU: 2.5% (Min: 1.2%, Max: 5.8%) | Memory: 45.32 MB (Min: 42.15 MB, Max: 48.67 MB) | Uptime: 2h 35m`

### 🔧 Improvements
- **Application-Specific Metrics**: CPU usage now shows the SMTP Service's CPU consumption, not system-wide usage
- **Larger Configuration Window**: Increased window size to 620x780 pixels to better display all statistics without content being cut off
- **Color-Coded Performance Indicators**: 
  - CPU and memory values change color based on usage (green = good, orange = moderate, red = high)
  - Helps quickly identify potential performance issues

### 📦 Technical
- Added System.Diagnostics.PerformanceCounter package for accurate performance monitoring
- Implemented separate timers for optimal refresh intervals based on metric volatility

---

## VERSION 3.3.0 - October 3, 2025

**Update Script Detection Improvements**

Enhanced update script self-detection and improved file summary display.

### Changes

- Update script self-update now detected immediately after file analysis
- Restarts before showing summaries if script needs updating
- Removed file display limits - all files now shown in summary
- No more truncation with "... and X more" messages

**Impact:** Faster script updates and complete visibility of all file changes.

---

## VERSION 3.2.3 - October 3, 2025

**Visual Update Improvements**

Added color-coded file analysis for clearer update preview.

### Changes

- Color-coded file listing: Green (new), Yellow (modified), Red (deleted)
- Enhanced summary display with color grouping
- Clearer visual feedback before update confirmation

**Impact:** Easier to understand what changes will be made before confirming update.

---

## VERSION 3.2.2 - October 3, 2025

**Update Script Immediate Self-Update with Major Version Enforcement**

Update script now immediately updates itself when detected, with mandatory updates for major version changes.

### Changes

- Update script detects when it needs updating during file replacement phase
- **Major version updates (x.0.0) force script update** - no option to decline
- Minor/patch updates (x.y.z) still prompt for user confirmation
- Creates temporary bridge script to handle the swap
- Bridge script deletes old version, renames new version, and relaunches
- Update process restarts from the beginning with new script
- All parameters preserved during restart
- Bridge script self-deletes after launching new update

**Impact:** Immediate self-updating with version-aware enforcement. Major updates require script update to ensure compatibility.

---

## VERSION 3.2.1 - October 3, 2025

**Update System Improvements**

Enhanced the update installer with better backup organization, automatic cleanup, and self-update capability.

### Changes

**Backup Management:**
- Backups now stored in centralized `backup` folder instead of root directory
- Automatic migration of existing backup folders to new location
- Complete backup of all files except logs, updates, and backup folders
- Automatic retention: keeps only 20 most recent backups
- Added protection for `.backup` files and `update_*.txt` files from deletion

**Self-Update Capability:**
- Update script can now update itself through a two-stage process
- When updated, creates Install-Update-NEW.ps1 for next run
- Automatically swaps and relaunches with preserved parameters
- Cleans up temporary files after successful swap

**Impact:** Cleaner root directory, automatic disk space management, more comprehensive backups, and fully self-maintaining update system.

---

## VERSION 3.2.0 - October 3, 2025

**Multi-Instance Support & Service Mode Improvements**

Enhanced run modes with multi-instance configuration support and pure background service mode.

### New Features

**Multi-Instance Configuration**
- Launch app while service is running to show UI-only configuration mode
- Multiple instances can configure the same running service
- Uses named mutex for service detection

**Pure Service Mode (Mode 0)**
- Mode 0 is now true background service (no console, no tray)
- Minimal memory footprint (~50 MB vs ~70 MB)
- Configure by launching another instance while service is running

**Close Console (Mode 1)**
- "Close Console" button actually frees console memory using FreeConsole()
- Saves ~10-20 MB of memory
- Console cannot be reopened without restarting app

### Changes

**Run Modes:**
- Mode 0: Service Mode (pure background, no UI)
- Mode 1: Console with Tray (can close console from tray)
- Mode 2: Tray Only (no console, tray icon only)

**UI Updates:**
- Tray tooltip shows "Configuration UI" when in UI-only mode
- Service Status indicates if running in UI-only mode
- Updated configuration descriptions

---

## VERSION 3.1.0 - October 3, 2025

**Console Toggle & Run Mode Improvements**

Enhanced user experience with console show/hide functionality and improved run mode behavior.

### New Features

**Console Visibility Control**
- Added "Show Console" / "Hide Console" toggle to system tray menu
- Console can be shown or hidden at any time via tray icon
- Menu text dynamically updates based on console state
- Available in all run modes with console support

**Improved Run Modes**
- Mode 0 renamed to "Service Mode with Tray" (was "Service/Console Mode")
- Mode 0 now shows system tray icon with console hidden by default
- Console automatically hides after 2-second delay in Mode 0 (startup messages remain visible)
- Mode 1: Console with Tray (console visible on startup)
- Mode 2: Tray Only (no console window)
- All modes with console support now include toggle functionality

### Bug Fixes

- Fixed system tray context menu not appearing on right-click
- Resolved threading issue where TrayApplicationContext was created on wrong thread
- Context menu now properly initializes on STA thread

### User Experience

- Clean startup in Service Mode - no console clutter
- Debug-friendly - console available when needed via tray menu
- Perfect for background service operation with optional visibility
- Consistent menu behavior across all run modes

---

## VERSION 3.0.0 - October 3, 2025

**Test Email Attachments & Bug Fixes**

Major update adding file attachment support to the Test Email feature and resolving compilation issues.

### New Features

**Test Email Attachments**
- Added ability to attach files to test emails
- Support for any file type up to 100 MB
- Browse button to select files with real-time file info display
- Clear button to remove selected attachment
- Automatic content type detection for common file formats (PDF, DOCX, images, archives, etc.)
- File size validation with user-friendly error messages
- Success confirmation shows attachment details

### Bug Fixes

- Fixed attachment list type incompatibility with Microsoft Graph API
- Fixed attachment size type casting error
- Removed invalid reference file causing compilation errors
- Suppressed null reference warnings in Statistics tab UI controls
- All compilation errors and warnings resolved

### Impact

Users can now fully test email delivery including attachments before deploying. The test email feature provides complete end-to-end verification through the Graph API without needing external SMTP clients.

---

## VERSION 2.2.0 - October 3, 2025

**Attachment Support Added**

Implemented full MIME multipart parsing with comprehensive attachment support for emails.

**Key Changes:**
- Added complete attachment support for SMTP emails sent via Microsoft Graph
- Implemented MIME multipart message parsing with boundary detection
- Support for Base64, Quoted-Printable, and other transfer encodings
- Handles both regular attachments and inline attachments (with Content-ID)
- Supports nested multipart messages (e.g., multipart/alternative inside multipart/mixed)
- Automatically detects and extracts filenames from Content-Type and Content-Disposition headers
- Logs attachment details (filename, size, inline status) for debugging
- No size limit on individual attachments (overall message size limit still applies)

**Impact:** Devices and applications can now send emails with attachments through the SMTP relay. Fully compatible with scanners, backup software, and email clients.

---

## VERSION 2.1.0 - October 2, 2025

**Configuration UI Improvements**

Enhanced configuration interface with better usability and increased default message size limit.

**Key Changes:**
- Added clickable link to Azure Entra admin center (https://entra.microsoft.com/) in MS Graph Settings
- Increased default max message size from 10MB to 50MB (51200 KB)
- Changed message size UI from KB to MB for easier configuration (range: 1-100 MB)
- Improved save button responsiveness - now enables immediately when typing or editing any field
- Added Leave and KeyUp event handlers to all text boxes and numeric controls

**Impact:** More intuitive configuration experience with larger default message size suitable for modern email needs.

---

## VERSION 2.0.1 - October 2, 2025

**Update System Improvements**

Enhanced the update notification interface and added orphaned file cleanup to the installer.

**Key Changes:**
- Simplified update notification dialog by removing release notes section
- Changed "Install Later" button to "Cancel" for clarity
- Added orphaned file detection: automatically removes files that exist in current installation but not in the update package
- Orphaned files are backed up before removal for safety
- Update installer now shows count of files to be removed during confirmation

**Impact:** Cleaner update experience and prevents accumulation of obsolete files from previous versions.

---

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
- Fixed system tray "Exit" button to properly close the application
- Resolved all null reference warnings for improved code safety

---

## VERSION 1.4.1 - October 2, 2025
- Added Statistics tab with email tracking (successful/failed counts per user and globally)
- Authentication is now optional - allows both authenticated and unauthenticated connections
- IP address tracking for unauthenticated senders

---

## VERSION 1.3.0 - October 1, 2025
- **CRITICAL FIX**: Fixed garbled HTML emails from Veeam and other applications
- Added complete MIME parsing with charset conversion (UTF-16/ISO-8859/Windows-1252 to UTF-8)
- HTML emails now display correctly with proper formatting and special characters

---

## VERSION 1.2.0 - October 1, 2025
- Added Install/Remove Windows Service directly from system tray menu
- Automatic credential masking for Tenant ID and Client ID in configuration UI

---

## VERSION 1.1.0 - 1.1.4 - October 1, 2025
- Added configurable run modes (Service Mode, Console with Tray, Tray Only)
- Enhanced configuration UI with smart save button and change tracking
- Fixed UTF-8 BOM issue causing disconnections with strict SMTP clients (Veeam)
- Configuration protection with automatic backups
- Added STARTTLS advertisement and 8BITMIME extension support

---

## VERSION 1.0.0 - September 30, 2025

**Initial Release** - Custom SMTP relay service with Microsoft Graph API integration

**Core Features:**
- Custom SMTP server (port 25) with Microsoft Graph API relay
- Email queue with automatic retry
- DPAPI-encrypted configuration
- Windows Service support
- System tray GUI with configuration interface
- SMTP authentication (AUTH LOGIN/PLAIN)
- Compatible with Veeam, network printers, IoT devices, and legacy applications

---

## Known Limitations

- No STARTTLS/TLS encryption
- No SMTP over SSL (port 465)
- Queue is in-memory only (lost on restart)
- No rate limiting

## Upcoming Features

- Full TLS encryption support
- Database-backed queue
- Web-based configuration
- Rate limiting
- Multiple sender accounts

---

For detailed technical information, see CHANGELOG_DETAILED.md
