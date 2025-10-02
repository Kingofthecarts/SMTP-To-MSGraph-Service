# SMTP to MS Graph Relay Service - Changelog

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
