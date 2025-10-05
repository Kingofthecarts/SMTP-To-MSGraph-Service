# Configuration File Locations

## Path Verification for SMTPServiceUpdater

All configuration files are located in the **Config** folder (capital C) within the root path.

### File Paths Referenced in Code

**1. git.json** (GitHub configuration)
- **Location**: `{RootPath}/Config/git.json`
- **Used by**: GitHubDownloader.cs
- **Method**: ReadGitConfig()
- **Line**: `string configPath = Path.Combine(_rootPath, "Config", "git.json");`
- **Missing behavior**: Returns null, logs error, GUI continues

**2. smtp-config.json** (Main SMTP configuration)
- **Location**: `{RootPath}/Config/smtp-config.json`
- **Used by**: UpdateSettingsReader.cs, ConfigMigrator.cs, UpdaterMainForm.cs
- **Methods**: 
  - ReadSettings()
  - ReadFullConfig()
  - MigrateConfiguration()
  - LoadConfiguration()
- **Line**: `string smtpConfigFile = Path.Combine(configPath, "smtp-config.json");`
- **Missing behavior**: Returns default settings, logs warning, GUI continues

**3. smtp.json** (Split SMTP settings - created during migration)
- **Location**: `{RootPath}/Config/smtp.json`
- **Used by**: ConfigMigrator.cs
- **Method**: CreateDefaultSplitConfigs()
- **Line**: `string smtpFile = Path.Combine(configPath, "smtp.json");`

**4. user.json** (Credentials - created during migration)
- **Location**: `{RootPath}/Config/user.json`
- **Used by**: ConfigMigrator.cs
- **Method**: CreateDefaultSplitConfigs()
- **Line**: `string userFile = Path.Combine(configPath, "user.json");`

**5. graph.json** (Microsoft Graph settings - created during migration)
- **Location**: `{RootPath}/Config/graph.json`
- **Used by**: ConfigMigrator.cs
- **Method**: CreateDefaultSplitConfigs()
- **Line**: `string graphFile = Path.Combine(configPath, "graph.json");`

## Path Construction Pattern

All config paths follow this pattern:
```csharp
string rootPath = AppDomain.CurrentDomain.BaseDirectory;
string configPath = Path.Combine(rootPath, "Config");
string configFile = Path.Combine(configPath, "filename.json");
```

## GUI Behavior Summary

**On Startup (OnFormLoad):**
- ✅ No auto-download/install
- ✅ Loads configuration from `Config/smtp-config.json`
- ✅ If file missing: Shows error in Configuration tab, GUI continues
- ✅ Displays initial messages in log

**When Download Button Clicked:**
- ✅ Reads `Config/git.json`
- ✅ If file missing: Shows error "git.json not found or invalid", re-enables Download button
- ✅ If file valid: Connects to GitHub API

**When Install Button Clicked:**
- ✅ Reads version from downloaded ZIP
- ✅ No config files required for installation
- ✅ Config migration may occur if needed

## Error Handling Verified

All config file operations have proper error handling:
- ✅ File not found → null/default returned, logged, continues
- ✅ Invalid JSON → null/default returned, logged, continues
- ✅ IOException → null/default returned, logged, continues
- ✅ Any exception → null/default returned, logged, continues

## Testing Checklist

- [ ] Run SMTPServiceUpdater.exe without Config folder → GUI opens, shows errors
- [ ] Run with empty Config folder → GUI opens, shows errors for missing files
- [ ] Run with Config/git.json only → Download works, config tab shows error
- [ ] Run with Config/smtp-config.json only → Config displays, Download shows error
- [ ] Run with both files → Full functionality

## Notes

- Folder name is **"Config"** with capital C (matches SMTP Service convention)
- All paths use `Path.Combine()` for cross-platform compatibility
- All file operations are wrapped in try-catch blocks
- Missing files result in graceful degradation, not crashes
- GUI remains functional even with missing configuration
