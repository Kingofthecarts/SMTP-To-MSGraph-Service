using SMTP_Service.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.Services
{
    public class QueueProcessorService : BackgroundService
    {
        private readonly ILogger<QueueProcessorService> _logger;
        private readonly QueueManager _queueManager;
        private readonly GraphEmailService _graphService;

        public QueueProcessorService(
            ILogger<QueueProcessorService> logger,
            QueueManager queueManager,
            GraphEmailService graphService)
        {
            _logger = logger;
            _queueManager = queueManager;
            _graphService = graphService;
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

                        var success = await _graphService.SendEmailAsync(item.Message);

                        if (success)
                        {
                            _queueManager.MarkAsSent(item.Id);
                        }
                        else
                        {
                            _queueManager.MarkAsFailed(item.Id, "Failed to send via Graph API");
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
