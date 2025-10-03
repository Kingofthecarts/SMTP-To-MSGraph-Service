# SMTP to MS Graph Relay Service - Changelog

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
