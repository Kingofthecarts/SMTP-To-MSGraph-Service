using System;
using System.Threading;
using SMTP_Service.Models;
using Serilog;
using ConfigManager = SMTP_Service.Managers.ConfigurationManager;

namespace EmailService.Flow
{
    public class SmtpFlowControl
    {
        private static SmtpFlowControl? _instance;
        private static readonly object _lock = new object();
        private volatile bool _isFlowEnabled = true;
        private int _sendDelayMs = 1000;

        public static SmtpFlowControl Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SmtpFlowControl();
                    }
                }
                return _instance;
            }
        }

        private SmtpFlowControl()
        {
            // Load initial state from SMTP config
            var configManager = new ConfigManager();
            var smtpConfig = configManager.LoadSmtpConfiguration();
            _isFlowEnabled = smtpConfig.SmtpFlowEnabled;
            _sendDelayMs = smtpConfig.SendDelayMs;
        }

        // Properties
        public bool IsFlowEnabled => _isFlowEnabled;
        public int SendDelayMs => _sendDelayMs;

        // Events
        public event Action<bool>? OnFlowStateChanged;

        // Methods
        public void HaltFlow()
        {
            _isFlowEnabled = false;
            OnFlowStateChanged?.Invoke(false);
            
            // Log with color to console
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HALT SMTP");
            Console.ResetColor();
            
            Log.Warning("SMTP flow HALTED (runtime state)");
        }

        public void ResumeFlow()
        {
            _isFlowEnabled = true;
            OnFlowStateChanged?.Invoke(true);
            
            // Log with color to console
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("FLOW SMTP");
            Console.ResetColor();
            
            Log.Information("SMTP flow RESUMED (runtime state)");
        }

        public void ToggleFlow()
        {
            if (_isFlowEnabled)
            {
                HaltFlow();
            }
            else
            {
                ResumeFlow();
            }
        }

        public void UpdateSendDelay(int delayMs)
        {
            if (delayMs < 100 || delayMs > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(delayMs), "Send delay must be between 100 and 10000 ms");
            }
            
            _sendDelayMs = delayMs;
            Log.Information($"Send delay updated to {delayMs}ms (runtime state)");
        }

        public void ApplySendDelay()
        {
            if (_sendDelayMs > 0)
            {
                Thread.Sleep(_sendDelayMs);
            }
        }
    }
}
