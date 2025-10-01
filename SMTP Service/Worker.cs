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

            try
            {
                await _smtpServer.StartAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fatal error: {ex.Message}");
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
