using System.Collections.Concurrent;
using SMTP_Service.Models;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.Managers
{
    public class QueueManager
    {
        private readonly ConcurrentQueue<EmailQueueItem> _queue = new();
        private readonly ConcurrentDictionary<Guid, EmailQueueItem> _allItems = new();
        private readonly ILogger<QueueManager> _logger;
        private readonly QueueSettings _settings;

        public QueueManager(ILogger<QueueManager> logger, QueueSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool Enqueue(EmailMessage message)
        {
            if (_allItems.Count >= _settings.MaxQueueSize)
            {
                _logger.LogWarning("Queue is full. Cannot enqueue new message.");
                return false;
            }

            var item = new EmailQueueItem
            {
                Message = message,
                Status = QueueItemStatus.Pending,
                QueuedAt = DateTime.Now
            };

            _allItems.TryAdd(item.Id, item);
            _queue.Enqueue(item);
            
            _logger.LogInformation($"Email queued: {item.Id} from {message.From} to {string.Join(", ", message.To)}");
            return true;
        }

        public EmailQueueItem? Dequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                item.Status = QueueItemStatus.Processing;
                item.LastAttempt = DateTime.Now;
                return item;
            }
            return null;
        }

        public void MarkAsSent(Guid id)
        {
            if (_allItems.TryGetValue(id, out var item))
            {
                item.Status = QueueItemStatus.Sent;
                _logger.LogInformation($"Email sent successfully: {id}");
            }
        }

        public void MarkAsFailed(Guid id, string error)
        {
            if (_allItems.TryGetValue(id, out var item))
            {
                item.RetryCount++;
                item.LastError = error;
                item.LastAttempt = DateTime.Now;

                if (item.RetryCount >= _settings.MaxRetryAttempts)
                {
                    item.Status = QueueItemStatus.Failed;
                    _logger.LogError($"Email failed permanently after {item.RetryCount} attempts: {id}. Error: {error}");
                }
                else
                {
                    item.Status = QueueItemStatus.Retrying;
                    item.NextRetry = DateTime.Now.AddMinutes(_settings.RetryDelayMinutes);
                    _queue.Enqueue(item); // Re-queue for retry
                    _logger.LogWarning($"Email failed (attempt {item.RetryCount}/{_settings.MaxRetryAttempts}): {id}. Will retry. Error: {error}");
                }
            }
        }

        public bool ShouldRetry(EmailQueueItem item)
        {
            return item.Status == QueueItemStatus.Retrying && 
                   item.NextRetry.HasValue && 
                   DateTime.Now >= item.NextRetry.Value;
        }

        public int GetQueueCount()
        {
            return _queue.Count;
        }

        public int GetTotalCount()
        {
            return _allItems.Count;
        }

        public Dictionary<QueueItemStatus, int> GetStatistics()
        {
            return _allItems.Values
                .GroupBy(i => i.Status)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public List<EmailQueueItem> GetRecentItems(int count = 100)
        {
            return _allItems.Values
                .OrderByDescending(i => i.QueuedAt)
                .Take(count)
                .ToList();
        }

        public void CleanupOldItems(int daysOld = 7)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var itemsToRemove = _allItems.Values
                .Where(i => (i.Status == QueueItemStatus.Sent || i.Status == QueueItemStatus.Failed) 
                           && i.QueuedAt < cutoffDate)
                .Select(i => i.Id)
                .ToList();

            foreach (var id in itemsToRemove)
            {
                _allItems.TryRemove(id, out _);
            }

            if (itemsToRemove.Any())
            {
                _logger.LogInformation($"Cleaned up {itemsToRemove.Count} old queue items");
            }
        }
    }
}
