using System.Text;
using System.Text.RegularExpressions;
using SMTP_Service.Models;
using Microsoft.Extensions.Logging;

namespace SMTP_Service.Managers
{
    public class SmtpProtocolHandler
    {
        private readonly ILogger<SmtpProtocolHandler> _logger;
        private readonly SmtpSettings _settings;

        private string _clientHostname = string.Empty;
        private bool _isAuthenticated = false;
        private string _authenticatedUser = string.Empty;
        private string _mailFrom = string.Empty;
        private List<string> _rcptTo = new();
        private StringBuilder _dataBuffer = new();
        private bool _inDataMode = false;
        private EmailMessage? _lastParsedEmail = null;

        public SmtpProtocolHandler(ILogger<SmtpProtocolHandler> logger, SmtpSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public string ProcessCommand(string command)
        {
            try
            {
                if (_inDataMode)
                {
                    return ProcessDataMode(command);
                }

                var parts = command.Split(' ', 2);
                var cmd = parts[0].ToUpper();
                var args = parts.Length > 1 ? parts[1] : string.Empty;

                return cmd switch
                {
                    "HELO" => HandleHelo(args),
                    "EHLO" => HandleEhlo(args),
                    "STARTTLS" => HandleStartTls(),
                    "AUTH" => HandleAuth(args),
                    "MAIL" => HandleMail(args),
                    "RCPT" => HandleRcpt(args),
                    "DATA" => HandleData(),
                    "RSET" => HandleRset(),
                    "NOOP" => "250 OK",
                    "QUIT" => HandleQuit(),
                    _ => "500 Command not recognized"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing command: {ex.Message}");
                return "500 Internal server error";
            }
        }

        private string HandleHelo(string hostname)
        {
            _clientHostname = hostname;
            _logger.LogInformation($"HELO received from {hostname}");
            return $"250 Hello {hostname}";
        }

        private string HandleEhlo(string hostname)
        {
            _clientHostname = hostname;
            _logger.LogInformation($"EHLO received from {hostname}");
            
            var response = new StringBuilder();
            response.AppendLine($"250-Hello {hostname}");
            
            // Advertise STARTTLS support (even though not fully implemented)
            // This prevents clients that require TLS from immediately disconnecting
            response.AppendLine("250-STARTTLS");
            
            if (_settings.RequireAuthentication)
            {
                response.AppendLine("250-AUTH LOGIN PLAIN");
            }
            response.AppendLine($"250-SIZE {_settings.MaxMessageSizeKb * 1024}");
            response.AppendLine("250-8BITMIME");
            response.Append("250 HELP");
            
            return response.ToString();
        }

        private string HandleStartTls()
        {
            _logger.LogInformation("STARTTLS command received");
            // For now, we'll reject STARTTLS since we don't have TLS implemented
            // But at least we respond properly instead of "command not recognized"
            return "454 TLS not available";
        }

        private string HandleAuth(string args)
        {
            if (!_settings.RequireAuthentication)
            {
                return "503 Authentication not required";
            }

            var parts = args.Split(' ', 2);
            var mechanism = parts[0].ToUpper();

            if (mechanism == "LOGIN")
            {
                return "334 VXNlcm5hbWU6"; // Base64 for "Username:"
            }
            else if (mechanism == "PLAIN" && parts.Length > 1)
            {
                return AuthenticatePlain(parts[1]);
            }
            else
            {
                // This is the username or password in response to 334
                if (!_isAuthenticated)
                {
                    // First response is username, ask for password
                    _authenticatedUser = DecodeBase64(args);
                    return "334 UGFzc3dvcmQ6"; // Base64 for "Password:"
                }
                else
                {
                    // This shouldn't happen, but handle it
                    return "503 Bad sequence of commands";
                }
            }
        }

        private string AuthenticatePlain(string encodedCredentials)
        {
            try
            {
                var decoded = DecodeBase64(encodedCredentials);
                var parts = decoded.Split('\0');
                
                string username = parts.Length > 1 ? parts[1] : parts[0];
                string password = parts.Length > 2 ? parts[2] : (parts.Length > 1 ? parts[1] : "");

                if (ValidateCredentials(username, password))
                {
                    _isAuthenticated = true;
                    _authenticatedUser = username;
                    _logger.LogInformation($"User {username} authenticated successfully");
                    return "235 Authentication successful";
                }
                else
                {
                    _logger.LogWarning($"Authentication failed for user {username}");
                    return "535 Authentication failed";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Auth error: {ex.Message}");
                return "535 Authentication failed";
            }
        }

        public string ProcessAuthData(string data)
        {
            if (string.IsNullOrEmpty(_authenticatedUser))
            {
                // This is the username
                _authenticatedUser = DecodeBase64(data);
                return "334 UGFzc3dvcmQ6"; // Base64 for "Password:"
            }
            else
            {
                // This is the password
                var password = DecodeBase64(data);
                if (ValidateCredentials(_authenticatedUser, password))
                {
                    _isAuthenticated = true;
                    _logger.LogInformation($"User {_authenticatedUser} authenticated successfully");
                    return "235 Authentication successful";
                }
                else
                {
                    _logger.LogWarning($"Authentication failed for user {_authenticatedUser}");
                    _authenticatedUser = string.Empty;
                    return "535 Authentication failed";
                }
            }
        }

        private bool ValidateCredentials(string username, string password)
        {
            return _settings.Credentials.Any(c => 
                c.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
                c.Password == password);
        }

        private string HandleMail(string args)
        {
            if (_settings.RequireAuthentication && !_isAuthenticated)
            {
                return "530 Authentication required";
            }

            if (!args.StartsWith("FROM:", StringComparison.OrdinalIgnoreCase))
            {
                return "501 Syntax error in parameters";
            }

            _mailFrom = ExtractEmail(args.Substring(5));
            _logger.LogInformation($"MAIL FROM: {_mailFrom}");
            return "250 OK";
        }

        private string HandleRcpt(string args)
        {
            if (string.IsNullOrEmpty(_mailFrom))
            {
                return "503 Bad sequence of commands";
            }

            if (!args.StartsWith("TO:", StringComparison.OrdinalIgnoreCase))
            {
                return "501 Syntax error in parameters";
            }

            var recipient = ExtractEmail(args.Substring(3));
            _rcptTo.Add(recipient);
            _logger.LogInformation($"RCPT TO: {recipient}");
            return "250 OK";
        }

        private string HandleData()
        {
            if (_rcptTo.Count == 0)
            {
                return "503 Bad sequence of commands";
            }

            _inDataMode = true;
            _dataBuffer.Clear();
            return "354 Start mail input; end with <CRLF>.<CRLF>";
        }

        private string ProcessDataMode(string line)
        {
            if (line == ".")
            {
                _inDataMode = false;
                _lastParsedEmail = ParseEmailData();
                
                if (_lastParsedEmail != null)
                {
                    _logger.LogInformation($"Email received: From={_lastParsedEmail.From}, To={string.Join(", ", _lastParsedEmail.To)}, Charset={_lastParsedEmail.Charset}, Encoding={_lastParsedEmail.ContentTransferEncoding}");
                    return "250 OK: Message accepted for delivery";
                }
                else
                {
                    return "554 Transaction failed";
                }
            }

            _dataBuffer.AppendLine(line);
            return string.Empty; // No response during data input
        }

        public EmailMessage? GetLastEmail()
        {
            var email = _lastParsedEmail;
            _lastParsedEmail = null;
            return email;
        }

        public EmailMessage? ParseEmailData()
        {
            try
            {
                var email = new EmailMessage
                {
                    From = _mailFrom,
                    To = new List<string>(_rcptTo),
                    RawMessage = _dataBuffer.ToString(),
                    ReceivedAt = DateTime.Now
                };

                // Parse headers and body
                var data = _dataBuffer.ToString();
                var headerEndIndex = data.IndexOf("\r\n\r\n");
                
                if (headerEndIndex == -1)
                {
                    headerEndIndex = data.IndexOf("\n\n");
                }

                if (headerEndIndex > 0)
                {
                    var headers = data.Substring(0, headerEndIndex);
                    var body = data.Substring(headerEndIndex + (data[headerEndIndex] == '\r' ? 4 : 2)); // Don't trim!

                    ParseHeaders(headers, email);
                    
                    // Decode the body based on Content-Transfer-Encoding
                    email.Body = DecodeBody(body, email.ContentTransferEncoding);
                    
                    // Convert body to UTF-8 if needed
                    email.Body = EnsureUtf8(email.Body, email.Charset);
                }
                else
                {
                    email.Body = data;
                }

                ResetTransaction();
                return email;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing email: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ResetTransaction();
                return null;
            }
        }

        private void ParseHeaders(string headers, EmailMessage email)
        {
            var lines = headers.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string currentHeader = string.Empty;
            string currentValue = string.Empty;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                // Check if this is a continuation of the previous header (starts with whitespace)
                if (line.StartsWith(" ") || line.StartsWith("\t"))
                {
                    currentValue += " " + line.Trim();
                    continue;
                }
                
                // Process the previous header if we have one
                if (!string.IsNullOrEmpty(currentHeader))
                {
                    ProcessHeader(currentHeader, currentValue, email);
                }
                
                // Start a new header
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    currentHeader = line.Substring(0, colonIndex).Trim();
                    currentValue = line.Substring(colonIndex + 1).Trim();
                }
            }
            
            // Process the last header
            if (!string.IsNullOrEmpty(currentHeader))
            {
                ProcessHeader(currentHeader, currentValue, email);
            }
        }

        private void ProcessHeader(string headerName, string headerValue, EmailMessage email)
        {
            // Store all headers
            email.Headers[headerName] = headerValue;
            
            var headerNameLower = headerName.ToLower();
            
            switch (headerNameLower)
            {
                case "subject":
                    email.Subject = DecodeHeaderValue(headerValue);
                    break;
                    
                case "content-type":
                    email.ContentType = headerValue;
                    email.IsHtml = headerValue.Contains("text/html", StringComparison.OrdinalIgnoreCase);
                    
                    // Extract charset
                    var charsetMatch = Regex.Match(headerValue, @"charset\s*=\s*[""']?([^""'\s;]+)", RegexOptions.IgnoreCase);
                    if (charsetMatch.Success)
                    {
                        email.Charset = charsetMatch.Groups[1].Value.ToLower();
                    }
                    break;
                    
                case "content-transfer-encoding":
                    email.ContentTransferEncoding = headerValue.Trim().ToLower();
                    break;
                    
                case "cc":
                    var ccAddresses = headerValue.Split(',');
                    email.Cc.AddRange(ccAddresses.Select(a => ExtractEmail(a.Trim())));
                    break;
            }
        }

        private string DecodeBody(string body, string transferEncoding)
        {
            try
            {
                switch (transferEncoding.ToLower())
                {
                    case "base64":
                        _logger.LogInformation("Decoding Base64 body content");
                        // Remove whitespace and decode
                        var cleanBase64 = Regex.Replace(body, @"\s+", "");
                        if (string.IsNullOrEmpty(cleanBase64))
                            return body;
                            
                        var bytes = Convert.FromBase64String(cleanBase64);
                        return Encoding.UTF8.GetString(bytes);
                        
                    case "quoted-printable":
                        _logger.LogInformation("Decoding Quoted-Printable body content");
                        return DecodeQuotedPrintable(body);
                        
                    case "7bit":
                    case "8bit":
                    case "binary":
                    default:
                        return body;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error decoding body with encoding '{transferEncoding}': {ex.Message}");
                return body; // Return original if decoding fails
            }
        }

        private string DecodeQuotedPrintable(string input)
        {
            var regex = new Regex(@"=([0-9A-F]{2})", RegexOptions.IgnoreCase);
            return regex.Replace(input, match => 
            {
                var hex = match.Groups[1].Value;
                var value = Convert.ToInt32(hex, 16);
                return ((char)value).ToString();
            }).Replace("=\r\n", "").Replace("=\n", "");
        }

        private string EnsureUtf8(string content, string charset)
        {
            try
            {
                // If already UTF-8, return as-is
                if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                    return content;
                
                // Try to convert from the specified charset to UTF-8
                _logger.LogInformation($"Converting content from {charset} to UTF-8");
                
                Encoding sourceEncoding;
                try
                {
                    sourceEncoding = Encoding.GetEncoding(charset);
                }
                catch
                {
                    _logger.LogWarning($"Unknown charset '{charset}', assuming UTF-8");
                    return content;
                }
                
                // Convert string to bytes in source encoding, then back to UTF-8 string
                var sourceBytes = sourceEncoding.GetBytes(content);
                return Encoding.UTF8.GetString(sourceBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error converting charset from {charset} to UTF-8: {ex.Message}");
                return content;
            }
        }

        private string DecodeHeaderValue(string headerValue)
        {
            // Decode RFC 2047 encoded words (=?charset?encoding?text?=)
            var regex = new Regex(@"=\?([^?]+)\?([BQbq])\?([^?]+)\?=");
            return regex.Replace(headerValue, match =>
            {
                var charset = match.Groups[1].Value;
                var encoding = match.Groups[2].Value.ToUpper();
                var encodedText = match.Groups[3].Value;

                try
                {
                    byte[] bytes;
                    if (encoding == "B")
                    {
                        bytes = Convert.FromBase64String(encodedText);
                    }
                    else // Q encoding (Quoted-Printable)
                    {
                        encodedText = encodedText.Replace('_', ' ');
                        return DecodeQuotedPrintable(encodedText);
                    }

                    var enc = Encoding.GetEncoding(charset);
                    return enc.GetString(bytes);
                }
                catch
                {
                    return match.Value; // Return original if decoding fails
                }
            });
        }

        private string HandleRset()
        {
            ResetTransaction();
            return "250 OK";
        }

        private string HandleQuit()
        {
            return "221 Bye";
        }

        private void ResetTransaction()
        {
            _mailFrom = string.Empty;
            _rcptTo.Clear();
            _dataBuffer.Clear();
            _inDataMode = false;
        }

        private string ExtractEmail(string input)
        {
            input = input.Trim();
            
            // Handle <email@domain.com> format
            var startIndex = input.IndexOf('<');
            if (startIndex >= 0)
            {
                var endIndex = input.IndexOf('>', startIndex);
                if (endIndex > startIndex)
                {
                    return input.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                }
            }
            
            return input;
        }

        private string DecodeBase64(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64;
            }
        }

        public bool IsAuthenticated => _isAuthenticated;
        public bool InDataMode => _inDataMode;
    }
}
