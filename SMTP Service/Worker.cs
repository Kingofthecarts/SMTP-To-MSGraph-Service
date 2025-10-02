using SMTP_Service.Services;
using Microsoft.Extensions.Logging;

namespace SMTP_Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SmtpServerService _smtpServer;

        public Worker(ILogger<Worker> logger, SmtpServerService smtpServer)
        {
            _logger = logger;
            _smtpServer = smtpServer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMTP to Graph Relay Service starting...");
            _logger.LogInformation($"SMTP Server service object created: {_smtpServer != null}");

            try
            {
                _logger.LogInformation("About to call StartAsync on SMTP server...");
                await _smtpServer.StartAsync(stoppingToken);
                _logger.LogInformation("StartAsync completed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fatal error: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SMTP to Graph Relay Service stopping...");
            await _smtpServer.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
