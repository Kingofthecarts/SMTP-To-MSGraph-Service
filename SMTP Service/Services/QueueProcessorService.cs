using SMTP_Service.Managers;
using SMTP_Service.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.Services
{
    public class QueueProcessorService : BackgroundService
    {
        private readonly ILogger<QueueProcessorService> _logger;
        private readonly QueueManager _queueManager;
        private readonly GraphEmailService _graphService;
        private readonly StatisticsManager _statsManager;
        private readonly SmtpConfiguration _smtpConfig;

        public QueueProcessorService(
            ILogger<QueueProcessorService> logger,
            QueueManager queueManager,
            GraphEmailService graphService,
            StatisticsManager statsManager,
            SmtpConfiguration smtpConfig)
        {
            _logger = logger;
            _queueManager = queueManager;
            _graphService = graphService;
            _statsManager = statsManager;
            _smtpConfig = smtpConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue processor service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = _queueManager.Dequeue();

                    if (item != null)
                    {
                        _logger.LogInformation($"Processing email from queue: {item.Id}");
                        _logger.LogInformation($"Email AuthenticatedUser: '{item.Message.AuthenticatedUser ?? "(null)"}'" );

                        var success = await _graphService.SendEmailAsync(item.Message);

                        if (success)
                        {
                            _queueManager.MarkAsSent(item.Id);
                            _logger.LogInformation($"Email sent successfully, recording stats for user: '{item.Message.AuthenticatedUser}'" );
                            _statsManager.RecordSuccess(item.Message.AuthenticatedUser);
                            
                            // Apply send delay if configured (flow control)
                            if (_smtpConfig.SendDelayMs > 0)
                            {
                                _logger.LogDebug($"Applying send delay of {_smtpConfig.SendDelayMs}ms before processing next message");
                                await Task.Delay(_smtpConfig.SendDelayMs, stoppingToken);
                            }
                        }
                        else
                        {
                            _queueManager.MarkAsFailed(item.Id, "Failed to send via Graph API");
                            _logger.LogWarning($"Email send failed, recording stats for user: '{item.Message.AuthenticatedUser}'" );
                            _statsManager.RecordFailure(item.Message.AuthenticatedUser);
                        }
                    }
                    else
                    {
                        // No items in queue, wait a bit
                        await Task.Delay(1000, stoppingToken);
                    }

                    // Cleanup old items periodically
                    if (DateTime.Now.Minute == 0 && DateTime.Now.Second < 10)
                    {
                        _queueManager.CleanupOldItems();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in queue processor: {ex.Message}");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("Queue processor service stopped");
        }
    }
}
