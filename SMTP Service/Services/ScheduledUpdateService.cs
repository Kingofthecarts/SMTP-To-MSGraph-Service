using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SMTP_Service.Models;
using Serilog;

namespace SMTP_Service.Services
{
    public class ScheduledUpdateService : BackgroundService
    {
        private readonly UpdateSettings _updateSettings;
        private readonly UpdateService _updateService;
        private readonly Managers.ConfigurationManager _configManager;
        private System.Threading.Timer? _updateTimer;

        public ScheduledUpdateService(
            AppConfig config,
            Managers.ConfigurationManager configManager)
        {
            _updateSettings = config.UpdateSettings;
            _updateService = new UpdateService();
            _configManager = configManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Scheduled Update Service starting");

            if (!_updateSettings.AutoUpdateEnabled)
            {
                Log.Information("Auto-updates are disabled in configuration");
                return;
            }

            // Check on startup if enabled
            if (_updateSettings.CheckOnStartup)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait for service to fully start
                await CheckForUpdatesAsync();
            }

            // Schedule recurring checks
            ScheduleNextCheck();

            // Keep service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void ScheduleNextCheck()
        {
            if (!_updateSettings.AutoUpdateEnabled)
                return;

            var nextCheckTime = CalculateNextCheckTime();
            var delay = nextCheckTime - DateTime.Now;

            if (delay.TotalMilliseconds < 0)
                delay = TimeSpan.FromMinutes(1); // Check in 1 minute if time already passed

            Log.Information($"Next update check scheduled for: {nextCheckTime:yyyy-MM-dd HH:mm:ss} ({nextCheckTime.DayOfWeek})");

            _updateTimer?.Dispose();
            _updateTimer = new System.Threading.Timer(
                async _ => await OnTimerElapsed(),
                null,
                delay,
                Timeout.InfiniteTimeSpan);
        }

        private DateTime CalculateNextCheckTime()
        {
            var now = DateTime.Now;
            DateTime nextCheck;

            switch (_updateSettings.CheckFrequency)
            {
                case UpdateCheckFrequency.Daily:
                    // Check at specific time each day
                    nextCheck = now.Date.Add(_updateSettings.CheckTime);
                    if (nextCheck < now)
                        nextCheck = nextCheck.AddDays(1);
                    break;

                case UpdateCheckFrequency.Weekly:
                    // Check at specific time on the selected day of week
                    nextCheck = now.Date.Add(_updateSettings.CheckTime);
                    
                    // Calculate days until the target day
                    int daysUntilTargetDay = ((int)_updateSettings.WeeklyCheckDay - (int)now.DayOfWeek + 7) % 7;
                    
                    if (daysUntilTargetDay == 0 && nextCheck < now)
                    {
                        // Today is the target day but time has passed, go to next week
                        daysUntilTargetDay = 7;
                    }
                    
                    nextCheck = nextCheck.AddDays(daysUntilTargetDay);
                    break;

                default:
                    nextCheck = now.AddHours(24);
                    break;
            }

            return nextCheck;
        }

        private async Task OnTimerElapsed()
        {
            await CheckForUpdatesAsync();
            ScheduleNextCheck();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                Log.Information("Performing scheduled update check");

                var result = await _updateService.CheckForUpdateAsync();

                // Update last check time and save
                _updateSettings.LastCheckDate = DateTime.Now;
                SaveConfiguration();

                if (!string.IsNullOrEmpty(result.Error))
                {
                    Log.Warning($"Update check failed: {result.Error}");
                    return;
                }

                if (result.Available)
                {
                    Log.Information($"Update available: {result.LatestVersion} (Current: {result.CurrentVersion})");

                    // Auto-download if enabled
                    if (_updateSettings.AutoDownload)
                    {
                        await DownloadUpdateAsync(result);
                    }
                    else
                    {
                        Log.Information("Auto-download is disabled. Update will not be downloaded automatically.");
                    }
                }
                else
                {
                    Log.Information($"No updates available (Current version: {result.CurrentVersion})");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during scheduled update check");
            }
        }

        private async Task DownloadUpdateAsync(UpdateCheckResult result)
        {
            try
            {
                Log.Information($"Auto-downloading update: {result.FileName}");

                var progress = new Progress<int>(percent =>
                {
                    if (percent % 10 == 0) // Log every 10%
                        Log.Information($"Download progress: {percent}%");
                });

                var success = await _updateService.DownloadUpdateAsync(
                    result.DownloadUrl!,
                    result.FileName!,
                    progress);

                if (success)
                {
                    Log.Information($"Update downloaded successfully: {result.FileName}");
                    Log.Information("Update will be available for installation on next application start");
                    
                    // Update tracking
                    _updateSettings.LastUpdateDate = DateTime.Now;
                    SaveConfiguration();
                }
                else
                {
                    Log.Error("Failed to download update");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading update");
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                _configManager.SaveConfiguration(config);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save configuration after update check");
            }
        }

        public override void Dispose()
        {
            _updateTimer?.Dispose();
            base.Dispose();
        }
    }
}
