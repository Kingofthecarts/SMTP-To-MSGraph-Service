using System.IO.Pipes;
using System.Text;

namespace SMTP_Service.Helpers
{
    /// <summary>
    /// Helper class for sending commands to the running SMTP service via Named Pipes.
    /// Used by UI-only instances to control the service remotely.
    /// </summary>
    public static class ServiceCommandClient
    {
        private const string PipeName = "SMTPRelayCommand";
        private const int TimeoutMs = 5000; // 5 second timeout

        /// <summary>
        /// Check if the service is reachable via Named Pipe
        /// </summary>
        public static bool IsServiceReachable()
        {
            try
            {
                var result = SendCommand("PING", out _);
                return result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Send HALT command to stop accepting SMTP connections
        /// </summary>
        public static bool TryHaltSmtp(out string error)
        {
            if (SendCommand("HALT", out error))
            {
                error = string.Empty;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Send RESUME command to start accepting SMTP connections
        /// </summary>
        public static bool TryResumeSmtp(out string error)
        {
            if (SendCommand("RESUME", out error))
            {
                error = string.Empty;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get current SMTP flow status (FLOWING or HALTED)
        /// </summary>
        public static bool TryGetStatus(out bool isFlowing, out string error)
        {
            if (SendCommand("STATUS", out error))
            {
                // Parse response: "OK|FLOWING" or "OK|HALTED"
                var parts = error.Split('|');
                if (parts.Length == 2)
                {
                    isFlowing = parts[1] == "FLOWING";
                    error = string.Empty;
                    return true;
                }
            }

            isFlowing = false;
            return false;
        }

        /// <summary>
        /// Send a command to the service via Named Pipe
        /// </summary>
        private static bool SendCommand(string command, out string response)
        {
            response = string.Empty;

            try
            {
                using var pipeClient = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.InOut,
                    PipeOptions.None);

                // Try to connect with timeout
                pipeClient.Connect(TimeoutMs);

                // Send command
                var commandBytes = Encoding.UTF8.GetBytes(command);
                pipeClient.Write(commandBytes, 0, commandBytes.Length);
                pipeClient.Flush();

                // Read response
                var buffer = new byte[1024];
                var bytesRead = pipeClient.Read(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                // Check if response indicates success
                if (response.StartsWith("OK|"))
                {
                    return true;
                }
                else if (response.StartsWith("ERROR|"))
                {
                    response = response.Substring(6); // Remove "ERROR|" prefix
                    return false;
                }

                return true;
            }
            catch (TimeoutException)
            {
                response = "Service is not responding. It may not be running.";
                return false;
            }
            catch (IOException ex)
            {
                response = $"Communication error: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                response = $"Unexpected error: {ex.Message}";
                return false;
            }
        }
    }
}
