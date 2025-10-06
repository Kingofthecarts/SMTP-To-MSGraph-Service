using SMTPServiceUpdater.Models;
using System.Diagnostics;
using System.ServiceProcess;

namespace SMTPServiceUpdater.Services;

/// <summary>
/// Manages the SMTP Service process (start/stop) during updates
/// </summary>
public class SmtpServiceController
{
    private readonly UpdateLogger _logger;
    private bool _wasRunning;

    /// <summary>
    /// Initializes a new instance of SmtpServiceController
    /// </summary>
    /// <param name="logger">Logger instance for operation tracking</param>
    public SmtpServiceController(UpdateLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wasRunning = false;
    }

    /// <summary>
    /// Checks if the SMTP Service process is currently running
    /// </summary>
    /// <returns>True if service is running, false otherwise</returns>
    public bool IsServiceRunning()
    {
        try
        {
            // Check for "SMTP Service" process name (exe without extension)
            var processes = Process.GetProcessesByName("SMTP Service");
            var isRunning = processes.Length > 0;

            // Clean up process objects
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return isRunning;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Error checking service status: {ex.Message}", LogLevel.Warning);
            return false;
        }
    }

    /// <summary>
    /// Gets all running SMTP Service processes (checks both process name and executable path)
    /// </summary>
    /// <returns>List of running SMTP Service processes</returns>
    private List<Process> GetSmtpServiceProcesses()
    {
        var smtpProcesses = new List<Process>();

        try
        {
            // First, try getting by process name "SMTP Service"
            var processesByName = Process.GetProcessesByName("SMTP Service");
            smtpProcesses.AddRange(processesByName);

            // Also check all processes for those with "SMTP Service.exe" in the path
            // This catches cases where the process might be running under a different name
            var allProcesses = Process.GetProcesses();
            
            foreach (var process in allProcesses)
            {
                try
                {
                    // Try to get the main module (exe path)
                    var exePath = process.MainModule?.FileName;
                    
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var fileName = Path.GetFileName(exePath);
                        
                        // Check if this is SMTP Service.exe
                        if (fileName.Equals("SMTP Service.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if we already have this process (avoid duplicates)
                            if (!smtpProcesses.Any(p => p.Id == process.Id))
                            {
                                smtpProcesses.Add(process);
                            }
                            else
                            {
                                // Already in list, dispose this duplicate reference
                                process.Dispose();
                            }
                        }
                        else
                        {
                            // Not SMTP Service, dispose
                            process.Dispose();
                        }
                    }
                    else
                    {
                        // Can't get path, dispose
                        process.Dispose();
                    }
                }
                catch
                {
                    // Can't access process (likely permission issue or process exited)
                    // Dispose and continue
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Error enumerating SMTP Service processes: {ex.Message}", LogLevel.Warning);
        }

        return smtpProcesses;
    }

    /// <summary>
    /// Stops the SMTP Service - first tries Windows Service, then kills processes
    /// </summary>
    /// <returns>True if service was stopped successfully or was not running, false on failure</returns>
    public bool StopService()
    {
        _logger.WriteLog("Checking if SMTP Service is running...", LogLevel.Info);

        bool wasRunningAsService = false;

        // STEP 1: Check if running as Windows Service
        try
        {
            var service = ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName.Equals("SMTP to MS Graph Relay", StringComparison.OrdinalIgnoreCase));

            if (service != null)
            {
                _logger.WriteLog($"Found Windows Service: {service.ServiceName}", LogLevel.Info);
                _logger.WriteLog($"Service Status: {service.Status}", LogLevel.Info);

                if (service.Status == ServiceControllerStatus.Running)
                {
                    wasRunningAsService = true;
                    _logger.WriteLog("Stopping Windows Service...", LogLevel.Warning);
                    
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    
                    _logger.WriteLog("Windows Service stopped successfully", LogLevel.Success);
                }
                else if (service.Status != ServiceControllerStatus.Stopped)
                {
                    _logger.WriteLog($"Service is in {service.Status} state - waiting for it to stop...", LogLevel.Warning);
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }

                service.Dispose();
            }
            else
            {
                _logger.WriteLog("Windows Service not found (may be running as standalone process)", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Error checking Windows Service: {ex.Message}", LogLevel.Warning);
            _logger.WriteLog("Will attempt to stop processes directly", LogLevel.Info);
        }

        // STEP 2: Check for running processes (even if service was stopped, processes might still be running)
        var processes = GetSmtpServiceProcesses();

        if (processes.Count == 0)
        {
            if (wasRunningAsService)
            {
                _logger.WriteLog("SMTP Service stopped successfully (was running as Windows Service)", LogLevel.Success);
                _wasRunning = true;
            }
            else
            {
                _logger.WriteLog("SMTP Service is not running", LogLevel.Info);
                _wasRunning = false;
            }
            return true;
        }

        // Found processes - kill them
        _logger.WriteLog($"Found {processes.Count} SMTP Service process(es) still running - terminating...", LogLevel.Warning);
        _wasRunning = true;

        try
        {
            // Kill all SMTP Service processes
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        _logger.WriteLog($"Terminating process ID: {process.Id} ({process.ProcessName})", LogLevel.Info);
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog($"Failed to kill process {process.Id}: {ex.Message}", LogLevel.Error);
                }
                finally
                {
                    process.Dispose();
                }
            }

            // Wait up to 5 seconds for all processes to terminate
            _logger.WriteLog("Waiting for processes to terminate...", LogLevel.Info);
            
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(5);

            while (stopwatch.Elapsed < timeout)
            {
                var remainingProcesses = GetSmtpServiceProcesses();
                
                if (remainingProcesses.Count == 0)
                {
                    // All processes terminated
                    _logger.WriteLog("All SMTP Service processes terminated successfully", LogLevel.Success);
                    return true;
                }

                // Clean up process objects
                foreach (var p in remainingProcesses)
                {
                    p.Dispose();
                }

                // Wait a bit before checking again
                Thread.Sleep(500);
            }

            // Timeout - check if any processes still running
            var stillRunning = GetSmtpServiceProcesses();
            
            if (stillRunning.Count > 0)
            {
                _logger.WriteLog($"Warning: {stillRunning.Count} process(es) still running after timeout", LogLevel.Warning);
                
                // Clean up
                foreach (var p in stillRunning)
                {
                    p.Dispose();
                }
                
                // Return true anyway - we tried our best
                return true;
            }

            _logger.WriteLog("SMTP Service stopped successfully", LogLevel.Success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Failed to stop SMTP Service: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Starts the SMTP Service process if it was previously running and restart is not disabled
    /// </summary>
    /// <param name="exePath">Full path to SMTP Service.exe</param>
    /// <param name="noRestart">If true, service will not be restarted even if it was running</param>
    /// <returns>True if service started successfully or restart was skipped, false on failure</returns>
    public bool StartService(string exePath, bool noRestart)
    {
        // Don't start if service wasn't running before
        if (!_wasRunning)
        {
            _logger.WriteLog("Service was not running before update, not starting", LogLevel.Info);
            return true;
        }

        // Don't start if noRestart flag is set
        if (noRestart)
        {
            _logger.WriteLog("Restart disabled by noRestart flag, not starting service", LogLevel.Info);
            return true;
        }

        if (string.IsNullOrWhiteSpace(exePath))
        {
            _logger.WriteLog("Service executable path is null or empty", LogLevel.Error);
            return false;
        }

        if (!File.Exists(exePath))
        {
            _logger.WriteLog($"Service executable not found: {exePath}", LogLevel.Error);
            return false;
        }

        _logger.WriteLog($"Starting SMTP Service: {exePath}", LogLevel.Info);

        try
        {
            // Start the process
            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath)
            };

            var process = Process.Start(processStartInfo);

            if (process == null)
            {
                _logger.WriteLog("Failed to start service process (Process.Start returned null)", LogLevel.Error);
                return false;
            }

            _logger.WriteLog($"Service process started with ID: {process.Id}", LogLevel.Info);

            // Wait 2 seconds and verify process is still running
            _logger.WriteLog("Waiting 2 seconds to verify service is running...", LogLevel.Info);
            Thread.Sleep(2000);

            // Check if process is still running
            process.Refresh();
            
            if (process.HasExited)
            {
                _logger.WriteLog($"Service process exited immediately with code: {process.ExitCode}", LogLevel.Error);
                process.Dispose();
                return false;
            }

            // Process is still running - success
            _logger.WriteLog("SMTP Service started successfully", LogLevel.Success);
            process.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            _logger.WriteLog($"Failed to start SMTP Service: {ex.Message}", LogLevel.Error);
            return false;
        }
    }
}
