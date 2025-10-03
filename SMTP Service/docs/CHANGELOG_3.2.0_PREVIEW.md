# CHANGELOG PREVIEW - VERSION 3.2.0

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

**End of Version 3.2.0 Preview**
