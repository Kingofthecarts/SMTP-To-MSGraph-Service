using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SMTP_Service.Services;
using ConfigManager = SMTP_Service.Managers.ConfigurationManager;

namespace SMTP_Service.Services
{
    /// <summary>
    /// Background service that listens for IPC commands via Named Pipes.
    /// Allows UI-only instances to control the running service.
    /// </summary>
    public class CommandListenerService : BackgroundService
    {
        private const string PipeName = "SMTPRelayCommand";
        private readonly ILogger<CommandListenerService> _logger;
        private readonly SmtpServerService _smtpServerService;
        private readonly ConfigManager _configManager;

        public CommandListenerService(
            ILogger<CommandListenerService> logger,
            SmtpServerService smtpServerService,
            ConfigManager configManager)
        {
            _logger = logger;
            _smtpServerService = smtpServerService;
            _configManager = configManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Command Listener Service starting on pipe: {PipeName}", PipeName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    _logger.LogDebug("Waiting for client connection on pipe...");
                    await pipeServer.WaitForConnectionAsync(stoppingToken);
                    _logger.LogDebug("Client connected to command pipe");

                    try
                    {
                        // Read command
                        var buffer = new byte[1024];
                        var bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                        var command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        _logger.LogInformation("Received command: {Command}", command);

                        // Process command
                        var response = ProcessCommand(command);

                        // Send response
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, stoppingToken);
                        await pipeServer.FlushAsync(stoppingToken);

                        _logger.LogDebug("Sent response: {Response}", response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing command");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in command listener loop");
                    await Task.Delay(1000, stoppingToken); // Brief delay before retry
                }
            }

            _logger.LogInformation("Command Listener Service stopped");
        }

        private string ProcessCommand(string command)
        {
            try
            {
                switch (command.ToUpperInvariant())
                {
                    case "HALT":
                        _smtpServerService.HaltSmtpFlow();
                        _configManager.SetSmtpFlowEnabled(false);
                        return "OK|HALTED";

                    case "RESUME":
                        _smtpServerService.ResumeSmtpFlow();
                        _configManager.SetSmtpFlowEnabled(true);
                        return "OK|FLOWING";

                    case "STATUS":
                        var isFlowing = _smtpServerService.IsSmtpFlowEnabled;
                        return $"OK|{(isFlowing ? "FLOWING" : "HALTED")}";

                    case "PING":
                        return "OK|PONG";

                    default:
                        return $"ERROR|Unknown command: {command}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {Command}", command);
                return $"ERROR|{ex.Message}";
            }
        }
    }
}
