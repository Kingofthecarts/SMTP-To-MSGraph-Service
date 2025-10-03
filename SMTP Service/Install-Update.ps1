param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    [switch]$NoRestart
)

# Get the script's directory (root of the running app)
$RootPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$UpdatesFolder = Join-Path $RootPath "updates"

# Files and folders to exclude from replacement
$ExcludedFiles = @("smtp-config.json")
$ExcludedFolders = @("logs", "stats", "updates", "backup")

# Track if service was running
$script:WasServiceRunning = $false

# Function to get log file path with incremental numbering
function Get-LogFilePath {
    # Create logs folder if it doesn't exist
    $logsFolder = Join-Path $RootPath "logs"
    if (-not (Test-Path $logsFolder)) {
        New-Item -Path $logsFolder -ItemType Directory -Force | Out-Null
    }
    
    $date = Get-Date -Format "yyyy-MM-dd"
    $logBaseName = "update_$date"
    $counter = 0
    $logFileName = "$logBaseName.txt"
    
    while (Test-Path (Join-Path $logsFolder $logFileName)) {
        $counter++
        $logFileName = "${logBaseName}_$(($counter).ToString('00')).txt"
    }
    
    return Join-Path $logsFolder $logFileName
}

# Function to write to log
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Write-Host $logMessage
    Add-Content -Path $script:LogFile -Value $logMessage
}

# Function to parse version string into comparable object
function Parse-Version {
    param([string]$VersionString)
    
    $parts = $VersionString -split '\.'
    $major = if ($parts.Length -gt 0) { [int]$parts[0] } else { 0 }
    $minor = if ($parts.Length -gt 1) { [int]$parts[1] } else { 0 }
    $patch = if ($parts.Length -gt 2) { [int]$parts[2] } else { 0 }
    
    return [PSCustomObject]@{
        Original = $VersionString
        Major = $major
        Minor = $minor
        Patch = $patch
    }
}

# Function to compare versions
function Compare-Versions {
    param(
        [PSCustomObject]$Version1,
        [PSCustomObject]$Version2
    )
    
    if ($Version1.Major -ne $Version2.Major) {
        return $Version1.Major - $Version2.Major
    }
    if ($Version1.Minor -ne $Version2.Minor) {
        return $Version1.Minor - $Version2.Minor
    }
    return $Version1.Patch - $Version2.Patch
}

# Function to detect latest version from updates folder
function Get-LatestVersion {
    if (-not (Test-Path $UpdatesFolder)) {
        return $null
    }
    
    $zipFiles = Get-ChildItem -Path $UpdatesFolder -Filter "*.zip" -File
    if ($zipFiles.Count -eq 0) {
        return $null
    }
    
    $versions = @()
    foreach ($zip in $zipFiles) {
        $versionString = [System.IO.Path]::GetFileNameWithoutExtension($zip.Name)
        $versions += Parse-Version -VersionString $versionString
    }
    
    # Sort by version and get highest
    $sorted = $versions | Sort-Object -Property @{Expression={$_.Major}; Descending=$true}, @{Expression={$_.Minor}; Descending=$true}, @{Expression={$_.Patch}; Descending=$true}
    return $sorted[0].Original
}

# Function to check if path should be excluded
function Should-Exclude {
    param([string]$RelativePath)
    
    $fileName = Split-Path -Leaf $RelativePath
    if ($ExcludedFiles -contains $fileName) {
        return $true
    }
    
    foreach ($folder in $ExcludedFolders) {
        if ($RelativePath -like "$folder\*" -or $RelativePath -eq $folder) {
            return $true
        }
    }
    
    return $false
}

# Function to check if service is running
function Test-SmtpServiceRunning {
    $processes = Get-Process -Name "SMTP Service" -ErrorAction SilentlyContinue
    return ($null -ne $processes -and $processes.Count -gt 0)
}

# Function to stop SMTP Service
function Stop-SmtpService {
    Write-Log "Checking SMTP Service status..."
    
    # Check if service is running
    $processes = Get-Process -Name "SMTP Service" -ErrorAction SilentlyContinue
    
    if ($processes) {
        $script:WasServiceRunning = $true
        Write-Log "  Service is running - stopping now..."
        
        foreach ($proc in $processes) {
            Write-Log "  Found process: PID $($proc.Id)"
            try {
                $proc.Kill()
                $proc.WaitForExit(5000)  # Wait up to 5 seconds
                Write-Log "  [OK] Process stopped (PID $($proc.Id))"
            }
            catch {
                Write-Log "  WARNING: Could not stop process: $($_.Exception.Message)"
                return $false
            }
        }
        
        # Give it a moment to fully release files
        Start-Sleep -Seconds 2
        
        # Verify it's actually stopped
        if (Test-SmtpServiceRunning) {
            Write-Log "  ERROR: Service is still running after stop attempt"
            return $false
        }
        
        Write-Log "  [OK] Service stopped successfully"
        return $true
    }
    else {
        $script:WasServiceRunning = $false
        Write-Log "  Service is not running - no need to stop"
        return $true
    }
}

# Initialize log file
$script:LogFile = Get-LogFilePath

# =============================================================================
# SELF-UPDATE CHECK
# If a previous update included a new version of this script, it was saved as
# Install-Update-NEW.ps1 because we couldn't replace ourselves while running.
# Now we detect it, swap it in, and RESTART THE ENTIRE UPDATE PROCESS from
# the beginning with the new script version.
# =============================================================================

# Check if there's a new update script to swap in
$NewUpdateScript = Join-Path $RootPath "Install-Update-NEW.ps1"
if (Test-Path $NewUpdateScript) {
    Write-Log "=========================================="
    Write-Log "    UPDATE SCRIPT SELF-UPDATE DETECTED"
    Write-Log "=========================================="
    Write-Log ""
    Write-Log "Found new update script: Install-Update-NEW.ps1"
    Write-Log "The update script itself needs to be updated."
    Write-Log "Will swap in the new version and restart the update from the beginning."
    
    # Check if the new script is different from the current one
    $currentScript = Join-Path $RootPath "Install-Update.ps1"
    $currentHash = (Get-FileHash -Path $currentScript -Algorithm SHA256).Hash
    $newHash = (Get-FileHash -Path $NewUpdateScript -Algorithm SHA256).Hash
    
    if ($currentHash -eq $newHash) {
        Write-Log "[WARNING] New script is identical to current script"
        Write-Log "Removing duplicate Install-Update-NEW.ps1 and continuing..."
        Remove-Item $NewUpdateScript -Force
        Write-Log ""
    }
    else {
        Write-Log "Swapping in new version and restarting entire update process..."
        
        try {
            # Backup current script
            $BackupScript = Join-Path $RootPath "Install-Update-OLD.ps1"
            if (Test-Path $BackupScript) {
                Remove-Item $BackupScript -Force
            }
            Copy-Item (Join-Path $RootPath "Install-Update.ps1") -Destination $BackupScript -Force
            Write-Log "[OK] Current script backed up to Install-Update-OLD.ps1"
            
            # Replace with new version
            Copy-Item $NewUpdateScript -Destination (Join-Path $RootPath "Install-Update.ps1") -Force
            Write-Log "[OK] New update script installed"
            
            # Remove the NEW file
            Remove-Item $NewUpdateScript -Force
            Write-Log "[OK] Temporary NEW script removed"
            
            # Prepare arguments for relaunch
            $arguments = @()
            if (-not [string]::IsNullOrEmpty($Version)) {
                $arguments += "-Version"
                $arguments += $Version
            }
            if ($NoRestart) {
                $arguments += "-NoRestart"
            }
            
            Write-Log ""
            Write-Log "RESTARTING UPDATE PROCESS FROM THE BEGINNING WITH NEW SCRIPT..."
            Write-Log "Arguments: $($arguments -join ' ')"
            Write-Log "=========================================="
            Write-Log ""
            
            Write-Host "" -ForegroundColor Yellow
            Write-Host "===== AUTOMATICALLY RESTARTING UPDATE WITH NEW SCRIPT =====" -ForegroundColor Yellow
            Write-Host "Please wait, the update will continue automatically..." -ForegroundColor Yellow  
            Write-Host "" -ForegroundColor Yellow
            
            # Give a moment for file operations to complete and user to see message
            Start-Sleep -Seconds 3
            
            # Relaunch the script from the beginning in a new window
            $scriptPath = Join-Path $RootPath "Install-Update.ps1"
            if ($arguments.Count -gt 0) {
                Start-Process powershell.exe -ArgumentList "-ExecutionPolicy", "Bypass", "-File", "`"$scriptPath`"", @arguments
            } else {
                Start-Process powershell.exe -ArgumentList "-ExecutionPolicy", "Bypass", "-File", "`"$scriptPath`""
            }
            
            Write-Host "New update script launched. This window will now close..." -ForegroundColor Green
            Start-Sleep -Seconds 2
            
            # Exit this instance immediately
            exit 0
        }
        catch {
            Write-Log "[ERROR] Failed to swap update script: $($_.Exception.Message)"
            Write-Log "Continuing with current version..."
            Write-Log ""
        }
    }
}

# Auto-detect version if not specified
if ([string]::IsNullOrEmpty($Version)) {
    Write-Log "=========================================="
    Write-Log "    SMTP Service Update Installer"
    Write-Log "=========================================="
    Write-Log ""
    Write-Log "Auto-detecting latest version..."
    
    $Version = Get-LatestVersion
    if ([string]::IsNullOrEmpty($Version)) {
        Write-Log "ERROR: No update packages found in updates folder"
        Write-Log "  Location: $UpdatesFolder"
        exit 1
    }
    
    Write-Log "[OK] Latest version detected: $Version"
    Write-Log ""
}
else {
    Write-Log "=========================================="
    Write-Log "    SMTP Service Update Installer"
    Write-Log "=========================================="
    Write-Log ""
    Write-Log "Using specified version: $Version"
    Write-Log ""
}

$ZipFile = Join-Path $UpdatesFolder "$Version.zip"
$ExtractPath = Join-Path $UpdatesFolder $Version

Write-Log "Root Path: $RootPath"
Write-Log "Zip File: $ZipFile"
Write-Log "Extract Path: $ExtractPath"
Write-Log "Log File: $script:LogFile"
Write-Log ""

# Step 1: Verify zip file exists
Write-Log "Step 1: Verifying update package..."
if (-not (Test-Path $ZipFile)) {
    Write-Log "ERROR: Update zip file not found at: $ZipFile"
    Write-Log "Please ensure the update has been downloaded."
    exit 1
}
Write-Log "[OK] Update package found: $ZipFile"
$zipInfo = Get-Item $ZipFile
Write-Log "  Size: $($zipInfo.Length) bytes"
Write-Log "  Date: $($zipInfo.LastWriteTime)"
Write-Log ""

# Step 2: Extract the zip file
Write-Log "Step 2: Extracting update package..."
if (Test-Path $ExtractPath) {
    Write-Log "WARNING: Extract folder already exists. Removing old extraction..."
    Remove-Item -Path $ExtractPath -Recurse -Force
    Write-Log "[OK] Old extraction removed"
}

try {
    Expand-Archive -Path $ZipFile -DestinationPath $ExtractPath -Force
    Write-Log "[OK] Update package extracted successfully"
    Write-Log "  Location: $ExtractPath"
    Write-Log ""
}
catch {
    Write-Log "ERROR: Failed to extract update package"
    Write-Log "  Error: $($_.Exception.Message)"
    exit 1
}

# Step 3: Compare files
Write-Log "Step 3: Analyzing files..."
Write-Log "=========================================="
Write-Log ""

$FilesToReplace = @()
$NewFiles = @()
$SkippedFiles = @()
$IdenticalFiles = @()

$UpdateFiles = Get-ChildItem -Path $ExtractPath -Recurse -File

foreach ($UpdateFile in $UpdateFiles) {
    $RelativePath = $UpdateFile.FullName.Substring($ExtractPath.Length + 1)
    
    if (Should-Exclude -RelativePath $RelativePath) {
        Write-Log "SKIPPED: $RelativePath (excluded)"
        $SkippedFiles += $RelativePath
        continue
    }
    
    $RootFile = Join-Path $RootPath $RelativePath
    
    if (Test-Path $RootFile) {
        $RootFileInfo = Get-Item $RootFile
        $UpdateFileInfo = $UpdateFile
        
        $sizeMatch = $RootFileInfo.Length -eq $UpdateFileInfo.Length
        $dateMatch = $RootFileInfo.LastWriteTime -eq $UpdateFileInfo.LastWriteTime
        
        if (-not $sizeMatch -or -not $dateMatch) {
            Write-Log "REPLACE: $RelativePath"
            $FilesToReplace += [PSCustomObject]@{
                Path = $RelativePath
                SourcePath = $UpdateFile.FullName
                DestPath = $RootFile
            }
        } else {
            $IdenticalFiles += $RelativePath
        }
    } else {
        Write-Log "NEW: $RelativePath"
        $NewFiles += [PSCustomObject]@{
            Path = $RelativePath
            SourcePath = $UpdateFile.FullName
            DestPath = $RootFile
        }
    }
}

# Detect orphaned files (files in current installation but NOT in update)
Write-Log "Detecting orphaned files (removed in new version)..."
$OrphanedFiles = @()

# Get all files currently in root
$CurrentFiles = Get-ChildItem -Path $RootPath -Recurse -File
foreach ($currentFile in $CurrentFiles) {
    $relativePath = $currentFile.FullName.Substring($RootPath.Length + 1)
    
    # Skip if excluded
    if (Should-Exclude -RelativePath $relativePath) {
        continue
    }
    
    # Skip if it's a backup folder (both old style and new central backup)
    if ($relativePath -like "backup_*\*" -or $relativePath -like "backup_*" -or $relativePath -like "backup\*" -or $relativePath -eq "backup") {
        continue
    }
    
    # Skip .backup files
    if ($currentFile.Extension -eq ".backup") {
        continue
    }
    
    # Skip update_*.txt files
    if ($currentFile.Name -like "update_*.txt") {
        continue
    }
    
    # Skip the log file we're currently writing to
    if ($currentFile.FullName -eq $script:LogFile) {
        continue
    }
    
    # Check if this file exists in the update
    $updateFilePath = Join-Path $ExtractPath $relativePath
    if (-not (Test-Path $updateFilePath)) {
        Write-Log "ORPHANED: $relativePath (not in update - will be removed)"
        $OrphanedFiles += [PSCustomObject]@{
            Path = $relativePath
            FullPath = $currentFile.FullName
        }
    }
}

Write-Log ""
Write-Log "Files to replace: $($FilesToReplace.Count)"
Write-Log "New files to add: $($NewFiles.Count)"
Write-Log "Identical files:  $($IdenticalFiles.Count)"
Write-Log "Skipped files:    $($SkippedFiles.Count)"
Write-Log "Orphaned files:   $($OrphanedFiles.Count)"
Write-Log ""
Write-Log "=========================================="
Write-Log ""

# Confirmation prompt
Write-Host ""
Write-Host "=========================================="  -ForegroundColor Yellow
Write-Host "         UPDATE CONFIRMATION"  -ForegroundColor Yellow
Write-Host "=========================================="  -ForegroundColor Yellow
Write-Host ""
Write-Host "Version to install: $Version" -ForegroundColor Cyan
Write-Host "Files to replace:   $($FilesToReplace.Count)" -ForegroundColor Cyan
Write-Host "New files to add:   $($NewFiles.Count)" -ForegroundColor Cyan
if ($OrphanedFiles.Count -gt 0) {
    Write-Host "Files to remove:    $($OrphanedFiles.Count)" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "A backup will be created before updating." -ForegroundColor Gray
Write-Host ""

$confirmation = Read-Host "Do you want to proceed with the update? (Y/N)"

if ($confirmation -notmatch '^[Yy]') {
    Write-Log "Update cancelled by user"
    Write-Host ""
    Write-Host "Update cancelled. No changes were made." -ForegroundColor Yellow
    Write-Host ""
    
    # Cleanup extracted folder since we're cancelling
    if (Test-Path $ExtractPath) {
        Remove-Item -Path $ExtractPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    exit 0
}

Write-Log "User confirmed update - proceeding..."
Write-Log ""

# Step 4: Stop the service if running
Write-Log "Step 4: Checking SMTP Service..."
Write-Log "=========================================="
Write-Log ""

if (-not (Stop-SmtpService)) {
    Write-Log ""
    Write-Log "ERROR: Could not stop SMTP Service"
    Write-Log "Please stop the service manually and run the update again."
    exit 1
}
Write-Log ""

# Step 5: Create backups and replace files
Write-Log "Step 5: Installing update..."
Write-Log "=========================================="
Write-Log ""

# Create central backup folder if it doesn't exist
$CentralBackupFolder = Join-Path $RootPath "backup"
if (-not (Test-Path $CentralBackupFolder)) {
    New-Item -Path $CentralBackupFolder -ItemType Directory -Force | Out-Null
    Write-Log "Created central backup folder: $CentralBackupFolder"
}

# Move any existing backup folders from root to central backup folder
$existingBackups = Get-ChildItem -Path $RootPath -Directory -Filter "backup_*"
if ($existingBackups.Count -gt 0) {
    Write-Log "Found $($existingBackups.Count) existing backup(s) in root - moving to central backup folder..."
    foreach ($oldBackup in $existingBackups) {
        try {
            $newPath = Join-Path $CentralBackupFolder $oldBackup.Name
            Move-Item -Path $oldBackup.FullName -Destination $newPath -Force
            Write-Log "  [OK] Moved: $($oldBackup.Name)"
        }
        catch {
            Write-Log "  [WARNING] Could not move: $($oldBackup.Name) - $($_.Exception.Message)"
        }
    }
    Write-Log ""
}

# Create new backup subfolder with timestamp
$BackupName = "backup_$Version_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
$BackupFolder = Join-Path $CentralBackupFolder $BackupName
New-Item -Path $BackupFolder -ItemType Directory -Force | Out-Null
Write-Log "Creating backup: $BackupName"
Write-Log "  Location: $BackupFolder"

# Backup all files except logs, updates, and backup folders
$foldersToExclude = @("logs", "updates", "backup")
$allItems = Get-ChildItem -Path $RootPath -Recurse
foreach ($item in $allItems) {
    $relativePath = $item.FullName.Substring($RootPath.Length + 1)
    
    # Skip excluded folders
    $skip = $false
    foreach ($excludeFolder in $foldersToExclude) {
        if ($relativePath -like "$excludeFolder\*" -or $relativePath -eq $excludeFolder) {
            $skip = $true
            break
        }
    }
    
    if (-not $skip) {
        $backupPath = Join-Path $BackupFolder $relativePath
        
        if ($item.PSIsContainer) {
            # Create directory
            if (-not (Test-Path $backupPath)) {
                New-Item -Path $backupPath -ItemType Directory -Force | Out-Null
            }
        }
        else {
            # Copy file
            $backupDir = Split-Path -Parent $backupPath
            if (-not (Test-Path $backupDir)) {
                New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
            }
            try {
                Copy-Item -Path $item.FullName -Destination $backupPath -Force
            }
            catch {
                Write-Log "  [WARNING] Could not backup: $relativePath"
            }
        }
    }
}
Write-Log "[OK] Backup created successfully"
Write-Log ""

$updateSuccess = $true

# Replace existing files
foreach ($file in $FilesToReplace) {
    try {
        # Special handling for Install-Update.ps1 (can't replace itself while running)
        # This creates Install-Update-NEW.ps1 which will be swapped in on the NEXT run
        if ($file.Path -eq "Install-Update.ps1") {
            Write-Log "SPECIAL: Install-Update.ps1 needs updating"
            $newScriptPath = Join-Path $RootPath "Install-Update-NEW.ps1"
            Copy-Item -Path $file.SourcePath -Destination $newScriptPath -Force
            Write-Log "[OK] New update script saved as Install-Update-NEW.ps1"
            Write-Log "     It will be automatically swapped in on next update run"
            continue
        }
        
        # Note: File already backed up in the full backup step above
        
        # Copy new file
        Copy-Item -Path $file.SourcePath -Destination $file.DestPath -Force
        Write-Log "[OK] Replaced: $($file.Path)"
    }
    catch {
        Write-Log "[ERROR] Failed to replace: $($file.Path)"
        Write-Log "  Error: $($_.Exception.Message)"
        $updateSuccess = $false
    }
}

# Add new files
foreach ($file in $NewFiles) {
    try {
        # Create directory if needed
        $destDir = Split-Path -Parent $file.DestPath
        if (-not (Test-Path $destDir)) {
            New-Item -Path $destDir -ItemType Directory -Force | Out-Null
        }
        
        # Copy new file
        Copy-Item -Path $file.SourcePath -Destination $file.DestPath -Force
        Write-Log "[OK] Added: $($file.Path)"
    }
    catch {
        Write-Log "[ERROR] Failed to add: $($file.Path)"
        Write-Log "  Error: $($_.Exception.Message)"
        $updateSuccess = $false
    }
}

# Remove orphaned files (already backed up in the full backup step)
if ($OrphanedFiles.Count -gt 0) {
    Write-Log ""
    Write-Log "Removing orphaned files..."
    foreach ($file in $OrphanedFiles) {
        try {
            # Note: File already backed up in the full backup step above
            
            # Delete the file
            Remove-Item -Path $file.FullPath -Force
            Write-Log "[OK] Removed: $($file.Path)"
        }
        catch {
            Write-Log "[ERROR] Failed to remove: $($file.Path)"
            Write-Log "  Error: $($_.Exception.Message)"
            $updateSuccess = $false
        }
    }
}

Write-Log ""

# Step 6: Post-update actions (if any)
Write-Log "Step 6: Post-update actions..."
Write-Log "=========================================="
Write-Log ""

# TODO: Add any post-update actions here
# Examples:
# - Modify configuration files
# - Run database migrations
# - Update registry settings
# - Set file permissions
# - etc.

Write-Log "No post-update actions configured"
Write-Log ""

# Step 7: Cleanup
Write-Log "Step 7: Cleanup..."
Write-Log "=========================================="
Write-Log ""

# Remove the extracted update folder (keep the zip file)
if (Test-Path $ExtractPath) {
    try {
        Remove-Item -Path $ExtractPath -Recurse -Force
        Write-Log "[OK] Removed extracted update folder"
        Write-Log "  Location: $ExtractPath"
    }
    catch {
        Write-Log "WARNING: Could not remove extracted folder"
        Write-Log "  Location: $ExtractPath"
        Write-Log "  Error: $($_.Exception.Message)"
        Write-Log "  You can manually delete this folder"
    }
}

Write-Log "[OK] Keeping update package: $ZipFile"
Write-Log ""

# Final summary
Write-Log ""
if ($updateSuccess) {
    Write-Log "=========================================="
    Write-Log "Update installed successfully!"
    Write-Log "=========================================="
    Write-Log ""
    Write-Log "Version: $Version"
    Write-Log "Files replaced: $($FilesToReplace.Count)"
    Write-Log "Files added: $($NewFiles.Count)"
    if ($OrphanedFiles.Count -gt 0) {
        Write-Log "Files removed: $($OrphanedFiles.Count)"
    }
    Write-Log "Backup location: $BackupFolder"
    
    # Check if this was a restart after self-update
    $OldScript = Join-Path $RootPath "Install-Update-OLD.ps1"
    if (Test-Path $OldScript) {
        Write-Log ""
        Write-Log "Note: Update script was also updated during this process"
    }
    
    Write-Log ""
    
    # Only restart if it was running before AND NoRestart not specified
    if ($script:WasServiceRunning -and -not $NoRestart) {
        Write-Log "Starting SMTP Service (was running before update)..."
        $exePath = Join-Path $RootPath "SMTP Service.exe"
        
        if (Test-Path $exePath) {
            try {
                $process = Start-Process -FilePath $exePath -PassThru
                Write-Log "[OK] SMTP Service process started (PID: $($process.Id))"
                
                # Wait a moment and verify it's still running
                Start-Sleep -Seconds 2
                
                if (-not $process.HasExited) {
                    Write-Log "[OK] SMTP Service verified running"
                    Write-Host ""
                    Write-Host "SMTP Service restarted successfully!" -ForegroundColor Green
                    Write-Host "Process ID: $($process.Id)" -ForegroundColor Green
                }
                else {
                    Write-Log "WARNING: SMTP Service started but exited immediately"
                    Write-Log "  Exit Code: $($process.ExitCode)"
                    Write-Host ""
                    Write-Host "WARNING: Service started but stopped immediately. Check logs." -ForegroundColor Yellow
                }
            }
            catch {
                Write-Log "ERROR: Failed to start SMTP Service"
                Write-Log "  Error: $($_.Exception.Message)"
                Write-Host ""
                Write-Host "ERROR: Could not start SMTP Service. Please start manually." -ForegroundColor Red
            }
        }
        else {
            Write-Log "ERROR: Could not find SMTP Service.exe to restart"
            Write-Log "  Expected path: $exePath"
            Write-Host ""
            Write-Host "ERROR: SMTP Service.exe not found. Please start manually." -ForegroundColor Red
        }
    }
    elseif (-not $script:WasServiceRunning) {
        Write-Log "Service not restarted (was not running before update)"
        Write-Host ""
        Write-Host "Service was not running before update - not restarted." -ForegroundColor Gray
    }
    elseif ($NoRestart) {
        Write-Log "Service not restarted (NoRestart flag specified)"
        Write-Host ""
        Write-Host "Service not restarted (NoRestart flag specified)." -ForegroundColor Gray
    }
}
else {
    Write-Log "=========================================="
    Write-Log "Update completed with errors"
    Write-Log "=========================================="
    Write-Log ""
    Write-Log "Some files could not be updated. Check log for details."
    Write-Log "Backup location: $BackupFolder"
}

# Step 8: Cleanup old backups (keep only 20 most recent)
Write-Log ""
Write-Log "Step 8: Backup maintenance..."
Write-Log "=========================================="
Write-Log ""

if (Test-Path $CentralBackupFolder) {
    $allBackups = Get-ChildItem -Path $CentralBackupFolder -Directory -Filter "backup_*" | Sort-Object -Property LastWriteTime -Descending
    
    if ($allBackups.Count -gt 20) {
        $backupsToRemove = $allBackups | Select-Object -Skip 20
        Write-Log "Found $($allBackups.Count) backups - removing oldest $($backupsToRemove.Count) to keep only 20..."
        
        foreach ($oldBackup in $backupsToRemove) {
            try {
                Remove-Item -Path $oldBackup.FullName -Recurse -Force
                Write-Log "  [OK] Removed old backup: $($oldBackup.Name)"
            }
            catch {
                Write-Log "  [WARNING] Could not remove old backup: $($oldBackup.Name)"
                Write-Log "    Error: $($_.Exception.Message)"
            }
        }
    }
    else {
        Write-Log "Current backup count: $($allBackups.Count) (within 20 backup limit)"
    }
}

# Clean up old update script backup if it exists
$OldUpdateScript = Join-Path $RootPath "Install-Update-OLD.ps1"
if (Test-Path $OldUpdateScript) {
    try {
        Remove-Item $OldUpdateScript -Force
        Write-Log "[OK] Removed old update script backup (Install-Update-OLD.ps1)"
    }
    catch {
        Write-Log "[WARNING] Could not remove Install-Update-OLD.ps1: $($_.Exception.Message)"
    }
}

Write-Log ""
Write-Log "Log file: $script:LogFile"
Write-Log ""
