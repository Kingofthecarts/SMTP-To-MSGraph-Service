using System.Net;
using System.Net.Sockets;
using System.Text;
using SMTP_Service.Managers;
using SMTP_Service.Models;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.Services
{
    public class SmtpServerService
    {
        private readonly ILogger<SmtpServerService> _logger;
        private readonly SmtpConfiguration _smtpConfig;
        private readonly UserConfiguration _userConfig;
        private readonly QueueManager _queueManager;
        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning = false;
        private volatile bool _smtpFlowEnabled = true;

        public SmtpServerService(
            ILogger<SmtpServerService> logger, 
            SmtpConfiguration smtpConfig,
            UserConfiguration userConfig,
            QueueManager queueManager)
        {
            _logger = logger;
            _smtpConfig = smtpConfig;
            _userConfig = userConfig;
            _queueManager = queueManager;
            _smtpFlowEnabled = smtpConfig.SmtpFlowEnabled; // Initialize from config
        }

        /// <summary>
        /// Gets whether SMTP flow is currently enabled
        /// </summary>
        public bool IsSmtpFlowEnabled => _smtpFlowEnabled;

        /// <summary>
        /// Halt SMTP flow - stop accepting new connections
        /// </summary>
        public void HaltSmtpFlow()
        {
            _smtpFlowEnabled = false;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("⛔ HALT SMTP");
            Console.WriteLine("Inbound connections blocked");
            Console.WriteLine("Messages will queue until resumed");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            Console.ResetColor();
            _logger.LogWarning("SMTP Flow HALTED - No longer accepting inbound connections");
        }

        /// <summary>
        /// Resume SMTP flow - start accepting connections again
        /// </summary>
        public void ResumeSmtpFlow()
        {
            _smtpFlowEnabled = true;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("✅ FLOW SMTP");
            Console.WriteLine("Accepting inbound connections");
            Console.WriteLine("Queued messages will be processed");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            Console.ResetColor();
            _logger.LogInformation("SMTP Flow RESUMED - Accepting inbound connections");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                _logger.LogWarning("SMTP server is already running");
                return;
            }

            try
            {
                _logger.LogInformation($"=== SMTP Server Configuration ===");
                _logger.LogInformation($"Port: {_smtpConfig.Port}");
                _logger.LogInformation($"Bind Address: {_smtpConfig.BindAddress}");
                _logger.LogInformation($"Require Authentication: {_smtpConfig.RequireAuthentication}");
                _logger.LogInformation($"Configured Users: {_userConfig.Credentials.Count}");
                foreach (var cred in _userConfig.Credentials)
                {
                    _logger.LogInformation($"  - User: {cred.Username}");
                }
                _logger.LogInformation($"=================================");
                
                _logger.LogInformation($"Attempting to start SMTP server on {_smtpConfig.BindAddress}:{_smtpConfig.Port}...");
                
                // Parse and validate bind address
                IPAddress bindAddress;
                if (!IPAddress.TryParse(_smtpConfig.BindAddress, out bindAddress!))
                {
                    _logger.LogWarning($"Invalid bind address '{_smtpConfig.BindAddress}', defaulting to 0.0.0.0");
                    bindAddress = IPAddress.Any;
                }
                
                _listener = new TcpListener(bindAddress, _smtpConfig.Port);
                _listener.Start();
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _logger.LogInformation($"SMTP server successfully started and listening on {_smtpConfig.BindAddress}:{_smtpConfig.Port}");
                _logger.LogInformation($"Server is ready to accept connections");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("[SMTP] Waiting for client connection...");
                        var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                        Console.WriteLine($"[SMTP] Client accepted, starting handler...");
                        
                        // Don't use Task.Run, just await the handler directly in a fire-and-forget manner
                        _ = HandleClientAsync(client, _cancellationTokenSource.Token);
                        Console.WriteLine($"[SMTP] Handler started");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error accepting client: {ex.Message}");
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError($"Failed to bind to port {_smtpConfig.Port}: {ex.Message}");
                _logger.LogError($"Error Code: {ex.SocketErrorCode}");
                if (ex.SocketErrorCode == System.Net.Sockets.SocketError.AccessDenied)
                {
                    _logger.LogError("ACCESS DENIED: Port 25 requires administrator privileges. Please run as administrator.");
                }
                else if (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
                {
                    _logger.LogError("PORT IN USE: Another application is already using port 25. Check with: netstat -ano | findstr :25");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start SMTP server: {ex.Message}");
                _logger.LogError($"Exception Type: {ex.GetType().Name}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public Task StopAsync()
        {
            if (!_isRunning)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("Stopping SMTP server...");
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _isRunning = false;
            _logger.LogInformation("SMTP server stopped");
            
            return Task.CompletedTask;
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            var clientIp = client.Client.RemoteEndPoint is IPEndPoint ipEndpoint ? ipEndpoint.Address.ToString() : "Unknown";
            var connectionStartTime = DateTime.Now;
            _logger.LogInformation($"Client connected: {clientEndpoint}");
            Console.WriteLine($"[SMTP] ============================================");
            Console.WriteLine($"[SMTP] NEW CONNECTION at {connectionStartTime:HH:mm:ss.fff}");
            Console.WriteLine($"[SMTP] Client: {clientEndpoint}");
            Console.WriteLine($"[SMTP] ============================================");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, new UTF8Encoding(false))) // UTF-8 without BOM
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" }) // UTF-8 without BOM
                {
                    // Set socket options for better diagnostics
                    client.ReceiveTimeout = 30000; // 30 seconds
                    client.SendTimeout = 30000;
                    
                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Socket configured: ReceiveTimeout=30s, SendTimeout=30s");
                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Creating protocol handler...");
                    
                    // Check if SMTP flow is enabled
                    if (!_smtpFlowEnabled)
                    {
                        Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] ⛔ FLOW HALTED - Rejecting connection");
                        _logger.LogWarning($"Connection from {clientEndpoint} rejected - SMTP flow is halted");
                        
                        // Send 421 Service not available response and close
                        await writer.WriteLineAsync("421 Service not available, closing transmission channel");
                        await writer.FlushAsync();
                        return;
                    }
                    
                    var handler = new SmtpProtocolHandler(
                        SmtpLoggerFactory.Factory.CreateLogger<SmtpProtocolHandler>(),
                        _smtpConfig,
                        _userConfig,
                        clientIp
                    );

                    // Send greeting - RFC 5321 compliant format
                    var greeting = $"220 {Environment.MachineName} ESMTP Service ready";
                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Sending greeting: {greeting}");
                    await writer.WriteLineAsync(greeting);
                    await writer.FlushAsync();
                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Greeting sent successfully");
                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Waiting for client commands...");

                    bool isAuthPhase = false;
                    int commandCount = 0;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Reading next line from client...");
                        
                        // Check if data is available
                        if (stream.DataAvailable)
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Data is available in stream");
                        }
                        else
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] No data available yet, waiting...");
                        }
                        
                        // Add timeout for reading
                        var readTask = reader.ReadLineAsync(cancellationToken).AsTask();
                        var timeoutTask = Task.Delay(30000, cancellationToken); // 30 second timeout
                        
                        var completedTask = await Task.WhenAny(readTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] TIMEOUT: Client did not send data within 30 seconds");
                            _logger.LogWarning($"Client {clientEndpoint} timed out after {commandCount} commands");
                            break;
                        }
                        
                        var line = await readTask;
                        
                        if (line == null)
                        {
                            var duration = DateTime.Now - connectionStartTime;
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Client closed connection (received null)");
                            Console.WriteLine($"[SMTP] Connection duration: {duration.TotalSeconds:F3} seconds");
                            Console.WriteLine($"[SMTP] Commands received: {commandCount}");
                            Console.WriteLine($"[SMTP] Authentication status: {(handler.IsAuthenticated ? "AUTHENTICATED" : "NOT AUTHENTICATED")}");
                            _logger.LogInformation($"Client {clientEndpoint} disconnected after {duration.TotalSeconds:F3}s, {commandCount} commands, Auth: {handler.IsAuthenticated}");
                            break;
                        }

                        commandCount++;
                        Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Command #{commandCount} Received: {line}");
                        _logger.LogInformation($"Client {clientEndpoint} -> {line}");

                        string response;

                        // Handle AUTH LOGIN sequence
                        if (isAuthPhase && !line.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Processing AUTH data (isAuthPhase=true)...");
                            response = handler.ProcessAuthData(line);
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] AUTH response: {response}");
                            if (response.StartsWith("235") || response.StartsWith("535"))
                            {
                                isAuthPhase = false;
                                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] AUTH phase completed. Success: {response.StartsWith("235")}, IsAuthenticated: {handler.IsAuthenticated}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Processing command (isAuthPhase=false)...");
                            response = handler.ProcessCommand(line);
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Command response: {response.Split('\n')[0]}");
                            
                            // Check if entering AUTH phase
                            if (line.StartsWith("AUTH LOGIN", StringComparison.OrdinalIgnoreCase))
                            {
                                isAuthPhase = true;
                                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Entering AUTH LOGIN phase");
                            }
                        }

                        if (!string.IsNullOrEmpty(response))
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Sending response: {response.Replace("\r\n", "\\r\\n")}");
                            await writer.WriteLineAsync(response);
                            await writer.FlushAsync();
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Response sent");
                            _logger.LogInformation($"Server -> {clientEndpoint}: {response.Split('\n')[0]}"); // Log first line only

                            // Check if client is quitting
                            if (response.StartsWith("221"))
                            {
                                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Client issued QUIT command");
                                break;
                            }

                            // Check if email was received (250 OK after DATA)
                            if (!handler.InDataMode && 
                                response.StartsWith("250 OK: Message accepted"))
                            {
                                // Get the email that was just parsed
                                var email = handler.GetLastEmail();
                                if (email != null)
                                {
                                    Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Queuing email: From={email.From}, To={string.Join(", ", email.To)}");
                                    _queueManager.Enqueue(email);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] No response to send (DATA mode or empty response)");
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                var duration = DateTime.Now - connectionStartTime;
                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] IOException: {ioEx.Message}");
                _logger.LogWarning($"IO Error with client {clientEndpoint} after {duration.TotalSeconds:F3}s: {ioEx.Message}");
            }
            catch (SocketException sockEx)
            {
                var duration = DateTime.Now - connectionStartTime;
                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] SocketException: {sockEx.Message}");
                _logger.LogWarning($"Socket Error with client {clientEndpoint} after {duration.TotalSeconds:F3}s: {sockEx.Message}");
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - connectionStartTime;
                Console.WriteLine($"[SMTP] [{DateTime.Now:HH:mm:ss.fff}] Exception: {ex.GetType().Name} - {ex.Message}");
                _logger.LogError($"Error handling client {clientEndpoint} after {duration.TotalSeconds:F3}s: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                var totalDuration = DateTime.Now - connectionStartTime;
                Console.WriteLine($"[SMTP] ============================================");
                Console.WriteLine($"[SMTP] CONNECTION CLOSED at {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine($"[SMTP] Client: {clientEndpoint}");
                Console.WriteLine($"[SMTP] Duration: {totalDuration.TotalSeconds:F3} seconds");
                Console.WriteLine($"[SMTP] ============================================");
                _logger.LogInformation($"Client disconnected: {clientEndpoint} (Duration: {totalDuration.TotalSeconds:F3}s)");
            }
        }

        public bool IsRunning => _isRunning;
    }

    // Renamed from LoggerExtensions to avoid conflict
    public static class SmtpLoggerFactory
    {
        public static ILoggerFactory Factory { get; set; } = null!;
    }
}
