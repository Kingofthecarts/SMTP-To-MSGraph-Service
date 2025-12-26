using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SMTPServiceUpdater.Models;

namespace SMTPServiceUpdater.Services
{
    /// <summary>
    /// Handles self-update scenarios where the updater executable itself needs to be replaced.
    /// Creates bridge scripts to allow the updater to replace itself.
    /// </summary>
    public class SelfUpdateHandler
    {
        private readonly UpdateLogger _logger;
        private readonly string _rootPath;

        public SelfUpdateHandler(UpdateLogger logger, string rootPath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
            }
            
            _rootPath = rootPath;
        }

        /// <summary>
        /// Checks if self-update is needed by looking for updater executable/dll files in operations.
        /// </summary>
        /// <param name="operations">List of file operations to check</param>
        /// <param name="version">Version being installed</param>
        /// <returns>True if self-update is needed</returns>
        public bool CheckForSelfUpdate(System.Collections.Generic.List<FileOperation> operations, string version)
        {
            if (operations == null)
            {
                return false;
            }

            foreach (var operation in operations)
            {
                if (operation.Operation == OperationType.Replace || operation.Operation == OperationType.Add)
                {
                    string fileName = Path.GetFileName(operation.Path);
                    if (fileName != null &&
                        (fileName.Equals("SMTPServiceUpdater.exe", StringComparison.OrdinalIgnoreCase) ||
                         fileName.Equals("SMTPServiceUpdater.dll", StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.WriteLog($"Self-update detected: {fileName} needs to be replaced", LogLevel.Warning);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the version is a major version update (x.0.0 format).
        /// Major version updates require automatic self-update confirmation.
        /// </summary>
        /// <param name="version">Version string to check</param>
        /// <returns>True if major version (x.0.0)</returns>
        public bool IsMajorVersionUpdate(string version)
        {
            if (!VersionInfo.TryParse(version, out VersionInfo? versionInfo) || versionInfo == null)
            {
            return false;
            }

            return versionInfo.Minor == 0 && versionInfo.Patch == 0;
        }

        /// <summary>
        /// Handles the self-update process by creating a bridge script and launching it.
        /// The bridge script will wait for this process to exit, replace the updater, and relaunch it.
        /// </summary>
        /// <param name="operations">All file operations for this update</param>
        /// <param name="version">Version being installed</param>
        /// <param name="noRestart">No-restart flag to pass to relaunched updater</param>
        /// <param name="isAutomatic">Whether this is an automatic update (no user prompts)</param>
        /// <param name="logFilePath">Current log file path to continue logging to</param>
        /// <returns>True if bridge script was created and launched (process will exit)</returns>
        public bool HandleSelfUpdate(System.Collections.Generic.List<FileOperation> operations, string version, bool noRestart, bool isAutomatic, string? logFilePath = null)
        {
            if (operations == null || operations.Count == 0)
            {
                _logger.WriteLog("No operations to process", LogLevel.Error);
                return false;
            }

            try
            {
                // Find all self-update files
                var updaterExeOp = operations.FirstOrDefault(o =>
                    (o.Operation == OperationType.Replace || o.Operation == OperationType.Add) &&
                    Path.GetFileName(o.Path).Equals("SMTPServiceUpdater.exe", StringComparison.OrdinalIgnoreCase));

                var updaterDllOp = operations.FirstOrDefault(o =>
                    (o.Operation == OperationType.Replace || o.Operation == OperationType.Add) &&
                    Path.GetFileName(o.Path).Equals("SMTPServiceUpdater.dll", StringComparison.OrdinalIgnoreCase));

                bool hasUpdaterFiles = updaterExeOp != null || updaterDllOp != null;

                if (!hasUpdaterFiles)
                {
                    _logger.WriteLog("No self-update files found", LogLevel.Error);
                    return false;
                }

                // Check if confirmation is needed
                bool isMajorVersion = IsMajorVersionUpdate(version);
                bool needsConfirmation = !isMajorVersion && !isAutomatic;

                if (needsConfirmation)
                {
                    _logger.WriteLog("Self-update requires confirmation (non-major version, interactive mode)", LogLevel.Warning);
                    _logger.WriteLog("Note: In GUI mode, user confirmation would be requested here", LogLevel.Info);
                }

                if (isMajorVersion)
                {
                    _logger.WriteLog($"Major version update ({version}) - auto-confirming self-update", LogLevel.Info);
                }

                // Save new updater files with -NEW suffix
                if (updaterExeOp != null)
                {
                    string newExePath = Path.Combine(_rootPath, "SMTPServiceUpdater-NEW.exe");
                    File.Copy(updaterExeOp.SourcePath, newExePath, overwrite: true);
                    _logger.WriteLog($"Saved new updater exe as: {newExePath}", LogLevel.Info);
                }

                if (updaterDllOp != null)
                {
                    string newDllPath = Path.Combine(_rootPath, "SMTPServiceUpdater-NEW.dll");
                    File.Copy(updaterDllOp.SourcePath, newDllPath, overwrite: true);
                    _logger.WriteLog($"Saved new updater dll as: {newDllPath}", LogLevel.Info);
                }

                // Create bridge script
                string? bridgeScriptPath = CreateBridgeScript(version, noRestart, isAutomatic, logFilePath);
                if (string.IsNullOrEmpty(bridgeScriptPath))
                {
                    _logger.WriteLog("Failed to create bridge script", LogLevel.Error);
                    return false;
                }

                _logger.WriteLog("Launching bridge script to complete self-update", LogLevel.Info);
                _logger.WriteLog("This process will exit - the updated version will relaunch automatically", LogLevel.Warning);

                // Launch bridge script
                LaunchBridgeScript(bridgeScriptPath);

                return true; // Signal that we should exit the process
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Self-update handler failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Creates a PowerShell bridge script that will replace the updater and relaunch it.
        /// </summary>
        /// <param name="version">Version being installed</param>
        /// <param name="noRestart">No-restart flag to pass to relaunched updater</param>
        /// <param name="isAutomatic">Whether this is automatic mode</param>
        /// <param name="logFilePath">Current log file path to continue logging to</param>
        /// <returns>Path to the created bridge script, or null on failure</returns>
        private string? CreateBridgeScript(string version, bool noRestart, bool isAutomatic, string? logFilePath)
        {
            string bridgeScriptPath = Path.Combine(_rootPath, "Update-Bridge.ps1");

            try
            {
                var scriptContent = new StringBuilder();
                scriptContent.AppendLine("# Bridge script for self-update");
                scriptContent.AppendLine("# This script replaces the updater and relaunches it");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Bridge script starting - waiting for updater to exit...' -ForegroundColor Yellow");
                scriptContent.AppendLine("Start-Sleep -Seconds 5");
                scriptContent.AppendLine();
                scriptContent.AppendLine($"$rootPath = '{_rootPath}'");
                scriptContent.AppendLine("$bridgeScript = Join-Path $rootPath 'Update-Bridge.ps1'");
                scriptContent.AppendLine();
                scriptContent.AppendLine("# Update updater executable and DLL");
                scriptContent.AppendLine("$oldExe = Join-Path $rootPath 'SMTPServiceUpdater.exe'");
                scriptContent.AppendLine("$newExe = Join-Path $rootPath 'SMTPServiceUpdater-NEW.exe'");
                scriptContent.AppendLine("$oldDll = Join-Path $rootPath 'SMTPServiceUpdater.dll'");
                scriptContent.AppendLine("$newDll = Join-Path $rootPath 'SMTPServiceUpdater-NEW.dll'");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Replacing updater files...' -ForegroundColor Yellow");
                scriptContent.AppendLine();
                scriptContent.AppendLine("if (Test-Path $newExe) {");
                scriptContent.AppendLine("    if (Test-Path $oldExe) {");
                scriptContent.AppendLine("        Remove-Item $oldExe -Force");
                scriptContent.AppendLine("        Write-Host 'Removed old updater exe' -ForegroundColor Green");
                scriptContent.AppendLine("    }");
                scriptContent.AppendLine("    Rename-Item $newExe $oldExe -Force");
                scriptContent.AppendLine("    Write-Host 'Installed new updater exe' -ForegroundColor Green");
                scriptContent.AppendLine("}");
                scriptContent.AppendLine();
                scriptContent.AppendLine("if (Test-Path $newDll) {");
                scriptContent.AppendLine("    if (Test-Path $oldDll) {");
                scriptContent.AppendLine("        Remove-Item $oldDll -Force");
                scriptContent.AppendLine("        Write-Host 'Removed old updater dll' -ForegroundColor Green");
                scriptContent.AppendLine("    }");
                scriptContent.AppendLine("    Rename-Item $newDll $oldDll -Force");
                scriptContent.AppendLine("    Write-Host 'Installed new updater dll' -ForegroundColor Green");
                scriptContent.AppendLine("}");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Relaunching updater...' -ForegroundColor Yellow");
                scriptContent.AppendLine("$updaterExe = Join-Path $rootPath 'SMTPServiceUpdater.exe'");

                // Build arguments based on mode
                if (isAutomatic)
                {
                    scriptContent.AppendLine("$arguments = '--resume --mode auto'");
                }
                else
                {
                    scriptContent.AppendLine("$arguments = '--resume --mode gui'");
                }

                if (noRestart)
                {
                    scriptContent.AppendLine("$arguments += ' --no-restart'");
                }

                // Add log file path if provided
                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    string relativeLogPath = Path.GetRelativePath(_rootPath, logFilePath).Replace("\\", "/");
                    scriptContent.AppendLine($"$arguments += ' --log-file \"{relativeLogPath}\"'");
                }

                scriptContent.AppendLine("Start-Process $updaterExe -ArgumentList $arguments -WorkingDirectory $rootPath");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Bridge script complete - cleaning up...' -ForegroundColor Green");
                scriptContent.AppendLine("Start-Sleep -Seconds 1");
                scriptContent.AppendLine();
                scriptContent.AppendLine("# Self-delete");
                scriptContent.AppendLine("if (Test-Path $bridgeScript) {");
                scriptContent.AppendLine("    Remove-Item $bridgeScript -Force");
                scriptContent.AppendLine("}");

                File.WriteAllText(bridgeScriptPath, scriptContent.ToString());
                _logger.WriteLog($"Created bridge script: {bridgeScriptPath}", LogLevel.Success);

                return bridgeScriptPath;
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Failed to create bridge script: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Launches the bridge script in a new PowerShell process.
        /// </summary>
        /// <param name="bridgeScriptPath">Path to the bridge script</param>
        private void LaunchBridgeScript(string bridgeScriptPath)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{bridgeScriptPath}\"",
                    WorkingDirectory = _rootPath,
                    UseShellExecute = false,
                    CreateNoWindow = false // Show console for bridge script
                };

                Process.Start(processStartInfo);
                _logger.WriteLog("Bridge script launched successfully", LogLevel.Success);
            }
            catch (Exception ex)
            {
                _logger.WriteLog($"Failed to launch bridge script: {ex.Message}", LogLevel.Error);
                throw;
            }
        }
    }
}
