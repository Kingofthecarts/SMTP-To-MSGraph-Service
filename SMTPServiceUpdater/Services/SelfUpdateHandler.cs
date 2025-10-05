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
        /// Checks if self-update is needed by looking for Install-Update.ps1 in operations.
        /// This script file represents the updater executable that needs replacement.
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
                    if (fileName != null && fileName.Equals("Install-Update.ps1", StringComparison.OrdinalIgnoreCase))
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
            if (!VersionInfo.TryParse(version, out VersionInfo versionInfo))
            {
                return false;
            }

            return versionInfo.Minor == 0 && versionInfo.Patch == 0;
        }

        /// <summary>
        /// Handles the self-update process by creating a bridge script and launching it.
        /// The bridge script will wait for this process to exit, replace the updater, and relaunch it.
        /// </summary>
        /// <param name="scriptOp">File operation for the script update</param>
        /// <param name="version">Version being installed</param>
        /// <param name="noRestart">No-restart flag to pass to relaunched updater</param>
        /// <param name="isAutomatic">Whether this is an automatic update (no user prompts)</param>
        /// <returns>True if bridge script was created and launched (process will exit)</returns>
        public bool HandleScriptUpdate(FileOperation scriptOp, string version, bool noRestart, bool isAutomatic)
        {
            if (scriptOp == null)
            {
                _logger.WriteLog("Script operation is null", LogLevel.Error);
                return false;
            }

            try
            {
                // Save new script with -NEW suffix
                string newScriptPath = Path.Combine(_rootPath, "Install-Update-NEW.ps1");
                File.Copy(scriptOp.SourcePath, newScriptPath, overwrite: true);
                _logger.WriteLog($"Saved new updater script as: {newScriptPath}", LogLevel.Info);

                // Check if confirmation is needed
                bool isMajorVersion = IsMajorVersionUpdate(version);
                bool needsConfirmation = !isMajorVersion && !isAutomatic;

                if (needsConfirmation)
                {
                    _logger.WriteLog("Self-update requires confirmation (non-major version, interactive mode)", LogLevel.Warning);
                    _logger.WriteLog("Note: In GUI mode, user confirmation would be requested here", LogLevel.Info);
                    
                    // In console mode, we would prompt here
                    // In GUI mode, the calling code should handle the prompt
                    // For now, proceed with the update
                }

                if (isMajorVersion)
                {
                    _logger.WriteLog($"Major version update ({version}) - auto-confirming self-update", LogLevel.Info);
                }

                // Create bridge script
                string? bridgeScriptPath = CreateBridgeScript(version, noRestart);
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
        /// <returns>Path to the created bridge script, or null on failure</returns>
        private string? CreateBridgeScript(string version, bool noRestart)
        {
            string bridgeScriptPath = Path.Combine(_rootPath, "Install-Update-Bridge.ps1");

            try
            {
                var scriptContent = new StringBuilder();
                scriptContent.AppendLine("# Bridge script for self-update");
                scriptContent.AppendLine("# This script replaces the updater and relaunches it");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Bridge script starting - waiting for updater to exit...' -ForegroundColor Yellow");
                scriptContent.AppendLine("Start-Sleep -Seconds 3");
                scriptContent.AppendLine();
                scriptContent.AppendLine($"$rootPath = '{_rootPath}'");
                scriptContent.AppendLine("$oldScript = Join-Path $rootPath 'Install-Update.ps1'");
                scriptContent.AppendLine("$newScript = Join-Path $rootPath 'Install-Update-NEW.ps1'");
                scriptContent.AppendLine("$bridgeScript = Join-Path $rootPath 'Install-Update-Bridge.ps1'");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Replacing updater script...' -ForegroundColor Yellow");
                scriptContent.AppendLine("if (Test-Path $oldScript) {");
                scriptContent.AppendLine("    Remove-Item $oldScript -Force");
                scriptContent.AppendLine("    Write-Host 'Removed old updater script' -ForegroundColor Green");
                scriptContent.AppendLine("}");
                scriptContent.AppendLine();
                scriptContent.AppendLine("if (Test-Path $newScript) {");
                scriptContent.AppendLine("    Rename-Item $newScript $oldScript -Force");
                scriptContent.AppendLine("    Write-Host 'Installed new updater script' -ForegroundColor Green");
                scriptContent.AppendLine("} else {");
                scriptContent.AppendLine("    Write-Host 'ERROR: New updater script not found!' -ForegroundColor Red");
                scriptContent.AppendLine("    exit 1");
                scriptContent.AppendLine("}");
                scriptContent.AppendLine();
                scriptContent.AppendLine("Write-Host 'Relaunching updater with updated version...' -ForegroundColor Yellow");
                scriptContent.AppendLine($"$arguments = '-v {version}'");
                
                if (noRestart)
                {
                    scriptContent.AppendLine("$arguments += ' -n'");
                }
                
                scriptContent.AppendLine();
                scriptContent.AppendLine("Start-Process powershell -ArgumentList \"-ExecutionPolicy Bypass -File `\"$oldScript`\" $arguments\" -WorkingDirectory $rootPath");
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
