using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Users.Item.SendMail;
using SMTP_Service.Models;
using Microsoft.Extensions.Logging;
using GraphMessage = Microsoft.Graph.Models.Message;
using GraphRecipient = Microsoft.Graph.Models.Recipient;
using GraphEmailAddress = Microsoft.Graph.Models.EmailAddress;
using GraphItemBody = Microsoft.Graph.Models.ItemBody;
using GraphBodyType = Microsoft.Graph.Models.BodyType;
using GraphFileAttachment = Microsoft.Graph.Models.FileAttachment;

namespace SMTP_Service.Services
{
    public class GraphEmailService
    {
        private readonly ILogger<GraphEmailService> _logger;
        private readonly GraphSettings _settings;
        private GraphServiceClient? _graphClient;

        public GraphEmailService(ILogger<GraphEmailService> logger, GraphSettings settings)
        {
            _logger = logger;
            _settings = settings;
            InitializeGraphClient();
        }

        private void InitializeGraphClient()
        {
            try
            {
                _logger.LogInformation($"Initializing Graph client with Tenant: {_settings.TenantId}");
                
                var clientSecretCredential = new ClientSecretCredential(
                    _settings.TenantId,
                    _settings.ClientId,
                    _settings.ClientSecret
                );

                _graphClient = new GraphServiceClient(clientSecretCredential);
                _logger.LogInformation("Graph client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize Graph client: {ex.Message}");
            }
        }

        public async Task<bool> SendEmailAsync(EmailMessage email)
        {
            if (_graphClient == null)
            {
                _logger.LogError("Graph client not initialized");
                return false;
            }

            try
            {
                // Ensure body content is properly formatted for HTML emails
                string bodyContent = email.Body;
                
                // If it's HTML and doesn't have proper charset declaration, add it
                if (email.IsHtml && !string.IsNullOrEmpty(bodyContent))
                {
                    // Check if HTML already has proper encoding declaration
                    if (!bodyContent.Contains("charset", StringComparison.OrdinalIgnoreCase))
                    {
                        // Add charset meta tag if missing
                        if (bodyContent.Contains("<head>", StringComparison.OrdinalIgnoreCase))
                        {
                            bodyContent = bodyContent.Replace("<head>", 
                                "<head>\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />", 
                                StringComparison.OrdinalIgnoreCase);
                        }
                        else if (bodyContent.Contains("<html>", StringComparison.OrdinalIgnoreCase))
                        {
                            bodyContent = bodyContent.Replace("<html>", 
                                "<html>\n<head>\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n</head>", 
                                StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            // Wrap in proper HTML structure
                            bodyContent = $"<!DOCTYPE html>\n<html>\n<head>\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\n</head>\n<body>\n{bodyContent}\n</body>\n</html>";
                        }
                    }
                    
                    _logger.LogInformation("HTML email body prepared with UTF-8 charset");
                }

                var message = new GraphMessage
                {
                    Subject = email.Subject,
                    Body = new GraphItemBody
                    {
                        ContentType = email.IsHtml ? GraphBodyType.Html : GraphBodyType.Text,
                        Content = bodyContent
                    },
                    ToRecipients = email.To.Select(to => new GraphRecipient
                    {
                        EmailAddress = new GraphEmailAddress
                        {
                            Address = to
                        }
                    }).ToList()
                };

                // Add CC recipients if any
                if (email.Cc.Any())
                {
                    message.CcRecipients = email.Cc.Select(cc => new GraphRecipient
                    {
                        EmailAddress = new GraphEmailAddress
                        {
                            Address = cc
                        }
                    }).ToList();
                }

                // Add BCC recipients if any
                if (email.Bcc.Any())
                {
                    message.BccRecipients = email.Bcc.Select(bcc => new GraphRecipient
                    {
                        EmailAddress = new GraphEmailAddress
                        {
                            Address = bcc
                        }
                    }).ToList();
                }

                // Set the From address
                message.From = new GraphRecipient
                {
                    EmailAddress = new GraphEmailAddress
                    {
                        Address = string.IsNullOrEmpty(email.From) ? _settings.SenderEmail : email.From
                    }
                };

                // Add attachments if any
                if (email.Attachments.Any())
                {
                    _logger.LogInformation($"Adding {email.Attachments.Count} attachments to email");
                    message.Attachments = new List<Microsoft.Graph.Models.Attachment>();
                    
                    foreach (var attachment in email.Attachments)
                    {
                        var graphAttachment = new GraphFileAttachment
                        {
                            OdataType = "#microsoft.graph.fileAttachment",
                            Name = attachment.FileName,
                            ContentType = attachment.ContentType,
                            ContentBytes = attachment.Content,
                            Size = (int?)attachment.Size,
                            IsInline = attachment.IsInline
                        };
                        
                        // Add ContentId for inline attachments
                        if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
                        {
                            graphAttachment.ContentId = attachment.ContentId;
                        }
                        
                        message.Attachments.Add(graphAttachment);
                        _logger.LogInformation($"Added attachment: {attachment.FileName} ({attachment.Size} bytes, Inline: {attachment.IsInline})");
                    }
                }

                var requestBody = new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                };

                // Send the email using the configured sender account
                await _graphClient.Users[_settings.SenderEmail].SendMail.PostAsync(requestBody);

                _logger.LogInformation($"Email sent via Graph API: From={email.From}, To={string.Join(", ", email.To)}, Subject={email.Subject}, Attachments={email.Attachments.Count}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email via Graph API: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (_graphClient == null)
            {
                _logger.LogError("TestConnection: Graph client is null");
                return false;
            }

            try
            {
                _logger.LogInformation($"Testing Graph API connection for user: {_settings.SenderEmail}");
                
                // Try to get user information to verify connection
                var user = await _graphClient.Users[_settings.SenderEmail].GetAsync();
                
                _logger.LogInformation($"Graph API connection test successful. User: {user?.Mail}, DisplayName: {user?.DisplayName}");
                return true;
            }
            catch (Azure.Identity.AuthenticationFailedException ex)
            {
                _logger.LogError(ex, $"Authentication failed: {ex.Message}");
                _logger.LogError($"Tenant ID used: {_settings.TenantId}");
                _logger.LogError($"Client ID used: {_settings.ClientId}");
                
                // Check for specific error codes
                if (ex.Message.Contains("AADSTS7000215") || ex.Message.Contains("Invalid client secret"))
                {
                    _logger.LogError("CLIENT SECRET IS INVALID OR EXPIRED! Create a new client secret in Azure Portal.");
                }
                else if (ex.Message.Contains("AADSTS90002"))
                {
                    _logger.LogError("TENANT ID NOT FOUND! Verify your Tenant ID is correct.");
                }
                else if (ex.Message.Contains("AADSTS700016"))
                {
                    _logger.LogError("CLIENT ID (Application ID) NOT FOUND! Verify your Client ID is correct.");
                }
                else if (ex.Message.Contains("unauthorized_client"))
                {
                    _logger.LogError("APP REGISTRATION NOT AUTHORIZED! Check API permissions and admin consent.");
                }
                
                return false;
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
            {
                _logger.LogError(ex, $"Graph API OData error: {ex.Error?.Message}");
                _logger.LogError($"Error code: {ex.Error?.Code}");
                _logger.LogError($"Sender email: {_settings.SenderEmail}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Graph API connection test failed: {ex.Message}");
                _logger.LogError($"Exception type: {ex.GetType().FullName}");
                return false;
            }
        }

        public void UpdateSettings(GraphSettings newSettings)
        {
            if (_settings.TenantId != newSettings.TenantId ||
                _settings.ClientId != newSettings.ClientId ||
                _settings.ClientSecret != newSettings.ClientSecret)
            {
                // Credentials changed, reinitialize
                InitializeGraphClient();
            }
        }
    }
}
