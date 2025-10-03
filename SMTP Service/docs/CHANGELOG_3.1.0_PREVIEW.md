# CHANGELOG PREVIEW - VERSION 3.1.0

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

**End of Version 3.1.0 Preview**
