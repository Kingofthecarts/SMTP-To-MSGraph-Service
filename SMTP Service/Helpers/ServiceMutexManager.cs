using System;
using System.Threading;

namespace SMTP_Service.Helpers
{
    /// <summary>
    /// Manages service instance detection using a named mutex
    /// </summary>
    public class ServiceMutexManager : IDisposable
    {
        private const string MutexName = "Global\\SMTPGraphRelayService";
        private Mutex? _mutex;
        private bool _isServiceOwner;

        /// <summary>
        /// Attempts to acquire the service mutex (becomes the service instance)
        /// </summary>
        /// <returns>True if this instance owns the mutex (is the service), false otherwise</returns>
        public bool TryAcquireService()
        {
            try
            {
                _mutex = new Mutex(false, MutexName, out _isServiceOwner);
                return _isServiceOwner;
            }
            catch (Exception)
            {
                _isServiceOwner = false;
                return false;
            }
        }

        /// <summary>
        /// Checks if the service is currently running (mutex exists)
        /// </summary>
        /// <returns>True if service is running, false otherwise</returns>
        public static bool IsServiceRunning()
        {
            try
            {
                // Try to open existing mutex
                using var existingMutex = Mutex.OpenExisting(MutexName);
                return true; // Mutex exists = service is running
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false; // Mutex doesn't exist = service not running
            }
            catch (UnauthorizedAccessException)
            {
                // Mutex exists but we don't have access (running as different user)
                return true;
            }
        }

        /// <summary>
        /// Returns true if this instance is the service owner
        /// </summary>
        public bool IsServiceOwner => _isServiceOwner;

        public void Dispose()
        {
            if (_isServiceOwner && _mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Mutex was already released
                }
            }
            _mutex?.Dispose();
        }
    }
}
