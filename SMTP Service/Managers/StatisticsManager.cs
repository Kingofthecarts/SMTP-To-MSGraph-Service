using System.Text.Json;
using SMTP_Service.Models;
using Serilog;

namespace SMTP_Service.Managers
{
    public class StatisticsManager
    {
        private readonly string _statsFilePath;
        private Statistics _stats;
        private readonly object _lockObject = new object();

        public StatisticsManager()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var statsDirectory = Path.Combine(baseDirectory, "stats");
            Directory.CreateDirectory(statsDirectory);
            _statsFilePath = Path.Combine(statsDirectory, "statistics.json");
            
            _stats = LoadStatistics();
        }

        private Statistics LoadStatistics()
        {
            try
            {
                if (File.Exists(_statsFilePath))
                {
                    var json = File.ReadAllText(_statsFilePath);
                    return JsonSerializer.Deserialize<Statistics>(json) ?? new Statistics();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading statistics");
            }

            return new Statistics();
        }

        private void SaveStatistics()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_stats, options);
                File.WriteAllText(_statsFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving statistics");
            }
        }

        public void RecordSuccess(string? username = null)
        {
            lock (_lockObject)
            {
                Log.Information($"Recording success for user: '{username ?? "(null)"}'" );
                
                _stats.Global.TotalSuccess++;
                _stats.Global.LastSuccessDate = DateTime.Now;

                if (!string.IsNullOrEmpty(username))
                {
                    if (!_stats.UserStats.ContainsKey(username))
                    {
                        _stats.UserStats[username] = new UserStats { Username = username };
                        Log.Information($"Created new user stats entry for: {username}");
                    }

                    _stats.UserStats[username].TotalSuccess++;
                    _stats.UserStats[username].LastSuccessDate = DateTime.Now;
                    Log.Information($"User '{username}' now has {_stats.UserStats[username].TotalSuccess} successful sends");
                }
                else
                {
                    Log.Warning("Username was null or empty, not recording per-user stats");
                }

                SaveStatistics();
            }
        }

        public void RecordFailure(string? username = null)
        {
            lock (_lockObject)
            {
                Log.Information($"Recording failure for user: '{username ?? "(null)"}'" );
                
                _stats.Global.TotalFailed++;
                _stats.Global.LastFailureDate = DateTime.Now;

                if (!string.IsNullOrEmpty(username))
                {
                    if (!_stats.UserStats.ContainsKey(username))
                    {
                        _stats.UserStats[username] = new UserStats { Username = username };
                        Log.Information($"Created new user stats entry for: {username}");
                    }

                    _stats.UserStats[username].TotalFailed++;
                    _stats.UserStats[username].LastFailureDate = DateTime.Now;
                    Log.Information($"User '{username}' now has {_stats.UserStats[username].TotalFailed} failed sends");
                }
                else
                {
                    Log.Warning("Username was null or empty, not recording per-user stats");
                }

                SaveStatistics();
            }
        }

        public Statistics GetStatistics()
        {
            lock (_lockObject)
            {
                return _stats;
            }
        }

        public void ResetStatistics()
        {
            lock (_lockObject)
            {
                _stats = new Statistics();
                SaveStatistics();
            }
        }

        public void ResetUserStatistics(string username)
        {
            lock (_lockObject)
            {
                if (_stats.UserStats.ContainsKey(username))
                {
                    _stats.UserStats.Remove(username);
                    SaveStatistics();
                }
            }
        }
    }
}
