using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services
{
    /// <summary>
    /// Main orchestrator for the update process.
    /// Coordinates all update steps from extraction to installation and service restart.
    /// </summary>
    public class UpdateInstaller
    {
        private readonly UpdateLogger _logger;
        private readonly string _rootPath;
        private readonly FileManager _fileManager;
        private readonly SmtpServiceController _serviceController;
        private readonly ConfigMigrator _configMigrator;
        private readonly SelfUpdateHandler _selfUpdateHandler;
        private readonly IProgress<LogMessage>? _progress;

        public UpdateInstaller(string rootPath, IProgress<LogMessage>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
            }

            _rootPath = rootPath;
            _progress = progress;

            // Initialize logger
            _logger = new UpdateLogger(rootPath);
            
            // Subscribe to log events to forward to IProgress if provided
            if (_progress != null)
            {
                _logger.LogMessageReceived += (sender, logMessage) => _progress.Report(logMessage);
            }

            // Initialize all services
            _fileManager = new FileManager(_logger);
            _serviceController = new SmtpServiceController(_logger);
            _configMigrator = new ConfigMigrator(_logger);
            _selfUpdateHandler = new SelfUpdateHandler(_logger, rootPath);
        }

        /// <summary>
        /// Main entry point for the update process.
        /// Executes all update steps in order with comprehensive error handling.
        /// </summary>
        /// <param name="version">Version to install (null for auto-detect)</param>
        /// <param name="noRestart">If true, don't restart service after update</param>
        /// <returns>UpdateResult with success status and statistics</returns>
        public Task<UpdateResult> RunAsync(string version, bool noRestart = false)
        {
            return Task.Run(() =>
            {
                var result = new UpdateResult();
                string? extractedFolder = null;
                bool serviceStopped = false;

                try
                {
                // STEP 1: Initialize logger
                _logger.WriteHeader(AppVersion.Header);
                _logger.WriteLog($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogLevel.Info);
                _logger.WriteLog($"Root path: {_rootPath}", LogLevel.Info);
                _logger.WriteSeparator();

                // STEP 2: Determine if automatic mode
                UpdateSettings settings = UpdateSettingsReader.ReadSettings(Path.Combine(_rootPath, "Config"));
                bool isAutomatic = settings.IsFullyAutomatic;
                
                _logger.WriteLog($"Mode: {(isAutomatic ? "AUTOMATIC" : "INTERACTIVE")}", LogLevel.Info);
                if (isAutomatic)
                {
                    _logger.WriteLog("AutoDownload and AutoInstall are enabled", LogLevel.Info);
                }
                _logger.WriteSeparator();

                // STEP 3: Auto-detect version if not specified
                if (string.IsNullOrWhiteSpace(version))
                {
                    _logger.WriteLog("Version not specified - auto-detecting latest version", LogLevel.Info);
                    version = VersionManager.GetLatestVersion(Path.Combine(_rootPath, "updates")) ?? string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        result.AddError("No update packages found in updates folder");
                        _logger.WriteLog(result.ErrorMessages[0], LogLevel.Error);
                        return result;
                    }
                    
                    _logger.WriteLog($"Latest version found: {version}", LogLevel.Success);
                }
                else
                {
                    _logger.WriteLog($"Target version: {version}", LogLevel.Info);
                }

                string currentVersion = VersionManager.GetCurrentVersion(_rootPath);
                _logger.WriteLog($"Current version: {currentVersion}", LogLevel.Info);
                _logger.WriteSeparator();

                // STEP 4: Verify ZIP file exists
                string zipPath = Path.Combine(_rootPath, "updates", $"{version}.zip");
                if (!File.Exists(zipPath))
                {
                    result.AddError($"Update package not found: {zipPath}");
                    _logger.WriteLog(result.ErrorMessages[0], LogLevel.Error);
                    return result;
                }
                
                _logger.WriteLog($"Update package: {zipPath}", LogLevel.Success);
                _logger.WriteLog($"Package size: {new FileInfo(zipPath).Length / 1024:N0} KB", LogLevel.Info);
                _logger.WriteSeparator();

                // STEP 5: Extract ZIP file
                _logger.WriteLog("Extracting update package...", LogLevel.Info);
                extractedFolder = Path.Combine(_rootPath, "updates", $"extracted_{version}");
                
                try
                {
                    _fileManager.ExtractZip(zipPath, extractedFolder);
                    _logger.WriteLog($"Extracted to: {extractedFolder}", LogLevel.Success);
                }
                catch (Exception ex)
                {
                    result.AddError($"Failed to extract update package: {ex.Message}");
                    _logger.WriteLog(result.ErrorMessages[^1], LogLevel.Error);
                    return result;
                }
                _logger.WriteSeparator();

                // STEP 6: Analyze files
                _logger.WriteLog("Analyzing files...", LogLevel.Info);
                List<FileOperation> operations = _fileManager.AnalyzeFiles(extractedFolder, _rootPath);
                
                int addCount = operations.Count(o => o.Operation == OperationType.Add);
                int replaceCount = operations.Count(o => o.Operation == OperationType.Replace);
                int identicalCount = operations.Count(o => o.Operation == OperationType.Identical);
                int skipCount = operations.Count(o => o.Operation == OperationType.Skip);
                
                _logger.WriteLog($"Files to add: {addCount}", addCount > 0 ? LogLevel.Success : LogLevel.Info);
                _logger.WriteLog($"Files to replace: {replaceCount}", replaceCount > 0 ? LogLevel.Warning : LogLevel.Info);
                _logger.WriteLog($"Files identical: {identicalCount}", LogLevel.Info);
                _logger.WriteLog($"Files to ignore: {skipCount}", LogLevel.Info);
                
                // Show files being added
                if (addCount > 0)
                {
                    _logger.WriteLog("Files to add:", LogLevel.Info);
                    foreach (var op in operations.Where(o => o.Operation == OperationType.Add))
                    {
                        _logger.WriteLog($"  + {op.Path}", LogLevel.Success);
                    }
                }
                
                // Show files being replaced
                if (replaceCount > 0)
                {
                    _logger.WriteLog("Files to replace:", LogLevel.Info);
                    foreach (var op in operations.Where(o => o.Operation == OperationType.Replace))
                    {
                        _logger.WriteLog($"  ~ {op.Path}", LogLevel.Warning);
                    }
                }
                
                // Show files being ignored
                if (skipCount > 0)
                {
                    _logger.WriteLog("Files to ignore (protected/excluded):", LogLevel.Info);
                    foreach (var op in operations.Where(o => o.Operation == OperationType.Skip))
                    {
                        _logger.WriteLog($"  ! {op.Path}", LogLevel.Info);
                    }
                }
                
                _logger.WriteSeparator();

                // STEP 7: Check for self-update BEFORE any other operations
                if (CheckForSelfUpdate(operations, version))
                {
                    _logger.WriteLog("Self-update detected - handling updater replacement", LogLevel.Warning);
                    bool shouldExit = _selfUpdateHandler.HandleSelfUpdate(operations, version, noRestart, isAutomatic, _logger.LogFilePath);
                    
                    if (shouldExit)
                    {
                        _logger.WriteLog("Exiting for self-update - bridge script will relaunch", LogLevel.Info);
                        _logger.WriteLog("Application will terminate in 2 seconds...", LogLevel.Warning);
                        
                        // Give bridge script time to start
                        System.Threading.Thread.Sleep(2000);
                        
                        // Force application exit
                        Environment.Exit(0);
                    }
                    
                    // Remove self-update files from operations list since bridge will handle them
                    operations = operations.Where(o => 
                    {
                        string fileName = Path.GetFileName(o.Path);
                        return !fileName.Equals("SMTPServiceUpdater.exe", StringComparison.OrdinalIgnoreCase) &&
                               !fileName.Equals("SMTPServiceUpdater.dll", StringComparison.OrdinalIgnoreCase) &&
                               true;  // No additional files to exclude
                    }).ToList();
                    
                    _logger.WriteLog("Removed updater files from operations list - will be handled by bridge", LogLevel.Info);
                }

                // STEP 8: Detect orphaned files
                _logger.WriteLog("Detecting orphaned files...", LogLevel.Info);
                List<FileOperation> orphanedFiles = _fileManager.DetectOrphanedFiles(extractedFolder, _rootPath);
                
                if (orphanedFiles.Count > 0)
                {
                    _logger.WriteLog($"Orphaned files found: {orphanedFiles.Count}", LogLevel.Warning);
                    _logger.WriteLog("Files to remove:", LogLevel.Info);
                    foreach (var orphan in orphanedFiles)
                    {
                        _logger.WriteLog($"  - {orphan.Path}", LogLevel.Warning);
                    }
                }
                else
                {
                    _logger.WriteLog("No orphaned files detected", LogLevel.Info);
                }
                _logger.WriteSeparator();

                // STEP 9: Display summary
                _logger.WriteHeader("UPDATE SUMMARY");
                _logger.WriteLog($"Current Version: {currentVersion}", LogLevel.Info);
                _logger.WriteLog($"Target Version: {version}", LogLevel.Info);
                _logger.WriteLog($"Files to Add: {addCount}", LogLevel.Info);
                _logger.WriteLog($"Files to Replace: {replaceCount}", LogLevel.Info);
                _logger.WriteLog($"Files to Remove: {orphanedFiles.Count}", LogLevel.Info);
                _logger.WriteSeparator();

                // STEP 10: Confirmation (if not automatic)
                if (!isAutomatic)
                {
                    _logger.WriteLog("Manual confirmation required in interactive mode", LogLevel.Warning);
                    _logger.WriteLog("Proceeding with update...", LogLevel.Info);
                    _logger.WriteSeparator();
                }

                // STEP 11: Stop SMTP Service
                _logger.WriteLog("Checking SMTP Service status...", LogLevel.Info);
                if (_serviceController.IsServiceRunning())
                {
                    _logger.WriteLog("SMTP Service is running - stopping...", LogLevel.Warning);
                    if (!_serviceController.StopService())
                    {
                        result.AddError("Failed to stop SMTP Service - cannot proceed");
                        _logger.WriteLog(result.ErrorMessages[^1], LogLevel.Critical);
                        return result;
                    }
                    serviceStopped = true;
                    _logger.WriteLog("SMTP Service stopped successfully", LogLevel.Success);
                }
                else
                {
                    _logger.WriteLog("SMTP Service is not running", LogLevel.Info);
                }
                _logger.WriteSeparator();

                // STEP 12: Configuration migration
                bool migrationNeeded = VersionManager.IsMigrationNeeded(currentVersion, version);
                if (migrationNeeded)
                {
                    _logger.WriteLog("Configuration migration required", LogLevel.Warning);
                    _logger.WriteLog("Migrating configuration to split files...", LogLevel.Info);
                    
                    string configPath = Path.Combine(_rootPath, "Config");
                    if (_configMigrator.MigrateConfiguration(configPath))
                    {
                        _logger.WriteLog("Configuration migration completed", LogLevel.Success);
                    }
                    else
                    {
                        _logger.WriteLog("Configuration migration failed - continuing anyway", LogLevel.Warning);
                    }
                }
                else
                {
                    _logger.WriteLog("No configuration migration needed", LogLevel.Info);
                }
                _logger.WriteSeparator();

                // STEP 13: Create backup
                _logger.WriteLog("Creating backup...", LogLevel.Info);
                BackupInfo? backup;
                try
                {
                    backup = _fileManager.CreateBackup(_rootPath, version);
                    if (backup != null)
                    {
                        result.BackupPath = backup.BackupPath;
                        _logger.WriteLog($"Backup created: {backup.BackupPath}", LogLevel.Success);
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Failed to create backup: {ex.Message}");
                    _logger.WriteLog(result.ErrorMessages[^1], LogLevel.Critical);
                    return result;
                }
                _logger.WriteSeparator();

                // STEP 14: Apply file operations
                _logger.WriteLog("Applying file operations...", LogLevel.Info);
                var operationsToApply = operations.Where(o => 
                    o.Operation == OperationType.Add || 
                    o.Operation == OperationType.Replace).ToList();

                if (!_fileManager.ApplyFileOperations(operationsToApply))
                {
                    result.AddError("Some file operations failed - check log for details");
                    _logger.WriteLog(result.ErrorMessages[^1], LogLevel.Error);
                    // Continue anyway - some files may have succeeded
                }

                result.FilesAdded = addCount;
                result.FilesReplaced = replaceCount;
                _logger.WriteLog($"Applied {operationsToApply.Count} file operations", LogLevel.Success);
                _logger.WriteSeparator();

                // STEP 15: Remove orphaned files
                if (orphanedFiles.Count > 0)
                {
                    _logger.WriteLog("Removing orphaned files...", LogLevel.Info);
                    if (_fileManager.ApplyFileOperations(orphanedFiles))
                    {
                        result.FilesRemoved = orphanedFiles.Count;
                        _logger.WriteLog($"Removed {orphanedFiles.Count} orphaned files", LogLevel.Success);
                    }
                    else
                    {
                        _logger.WriteLog("Some orphaned files could not be removed", LogLevel.Warning);
                    }
                    _logger.WriteSeparator();
                }

                // STEP 16: Cleanup extracted folder
                _logger.WriteLog("Cleaning up temporary files...", LogLevel.Info);
                try
                {
                    if (Directory.Exists(extractedFolder))
                    {
                        _logger.WriteLog($"Removing extracted folder: {extractedFolder}", LogLevel.Info);
                        Directory.Delete(extractedFolder, recursive: true);
                        _logger.WriteLog("Temporary files removed successfully", LogLevel.Success);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog($"Failed to cleanup temporary files: {ex.Message}", LogLevel.Warning);
                }
                _logger.WriteSeparator();

                // STEP 17: Maintain backups
                _logger.WriteLog("Maintaining backup history...", LogLevel.Info);
                string backupFolder = Path.Combine(_rootPath, "backup");
                _fileManager.MaintainBackups(backupFolder, maxBackups: 20);
                _logger.WriteLog("Backup maintenance complete", LogLevel.Info);
                _logger.WriteSeparator();

                // STEP 18: Restart service
                if (serviceStopped && !noRestart)
                {
                    _logger.WriteLog("Restarting SMTP Service...", LogLevel.Info);
                    string exePath = Path.Combine(_rootPath, "SMTP Service.exe");
                    
                    if (_serviceController.StartService(exePath, noRestart))
                    {
                        _logger.WriteLog("SMTP Service started successfully", LogLevel.Success);
                    }
                    else
                    {
                        _logger.WriteLog("Failed to start SMTP Service - may require manual start", LogLevel.Error);
                    }
                }
                else if (serviceStopped && noRestart)
                {
                    _logger.WriteLog("Service not restarted (--no-restart flag set)", LogLevel.Warning);
                }
                else
                {
                    _logger.WriteLog("Service was not running - no restart needed", LogLevel.Info);
                }
                _logger.WriteSeparator();

                // STEP 19: Complete
                result.Success = true;
                _logger.WriteHeader("UPDATE COMPLETED SUCCESSFULLY");
                _logger.WriteLog($"Version: {version}", LogLevel.Success);
                _logger.WriteLog($"Files Added: {result.FilesAdded}", LogLevel.Info);
                _logger.WriteLog($"Files Replaced: {result.FilesReplaced}", LogLevel.Info);
                _logger.WriteLog($"Files Removed: {result.FilesRemoved}", LogLevel.Info);
                _logger.WriteLog($"Backup Location: {result.BackupPath}", LogLevel.Info);
                _logger.WriteLog($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogLevel.Info);
                _logger.WriteSeparator();

                return result;
            }
            catch (Exception ex)
            {
                result.AddError($"Unexpected error: {ex.Message}");
                _logger.WriteLog($"CRITICAL ERROR: {ex.Message}", LogLevel.Critical);
                _logger.WriteLog($"Stack trace: {ex.StackTrace}", LogLevel.Error);
                
                // Attempt cleanup if extraction folder exists
                if (!string.IsNullOrEmpty(extractedFolder) && Directory.Exists(extractedFolder))
                {
                    try
                    {
                        Directory.Delete(extractedFolder, recursive: true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                return result;
                }
            });
        }

        /// <summary>
        /// Gets the log file path for the current update session.
        /// </summary>
        public string? LogFilePath => _logger?.LogFilePath;

        /// <summary>
        /// Checks if any self-update files are in the operations list.
        /// </summary>
        private bool CheckForSelfUpdate(List<FileOperation> operations, string version)
        {
            return _selfUpdateHandler.CheckForSelfUpdate(operations, version);
        }
    }
}
