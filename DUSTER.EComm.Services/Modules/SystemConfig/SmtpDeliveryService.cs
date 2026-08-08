using DUSTER.EComm.Services.Modules.SystemConfig.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DUSTER.EComm.Services.Modules.SystemConfig
{
    public class SmtpDeliveryService : ISmtpDeliveryService
    {
        private readonly ILogger<SmtpDeliveryService> _logger;
        // private readonly IEncryptionService _encryptionService; // Uncomment if passwords need decryption

        public SmtpDeliveryService(ILogger<SmtpDeliveryService> logger)
        {
            _logger = logger;
        }

        public async Task<string> SendEmailAsync(SmtpConfiguration smtpConfig, string toEmail, string toName, string subject, string bodyHtml, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null)
        {
            var message = new MimeMessage();

            // 1. Set Sender
            message.From.Add(new MailboxAddress(smtpConfig.from_name ?? "System Alerts", smtpConfig.from_email));

            // 2. Set Primary Recipient
            message.To.Add(new MailboxAddress(toName ?? string.Empty, toEmail));

            // 3. CC Logic
            if (ccEmails != null)
            {
                foreach (var cc in ccEmails)
                {
                    if (!string.IsNullOrWhiteSpace(cc))
                        message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
                }
            }

            // 4. BCC Logic
            if (bccEmails != null)
            {
                foreach (var bcc in bccEmails)
                {
                    if (!string.IsNullOrWhiteSpace(bcc))
                        message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));
                }
            }

            // 5. Construct Email Subject and Body
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
            message.Body = bodyBuilder.ToMessageBody();

            // 6. Connect, Authenticate, and Dispatch
            using var client = new SmtpClient();
            try
            {
                // SecureSocketOptions.Auto allows MailKit to determine TLS based on the port (587, 465, etc.)
                await client.ConnectAsync(smtpConfig.host, smtpConfig.port, SecureSocketOptions.Auto);

                // Note: If you are encrypting passwords in the DB, decrypt them here before authenticating.
                // string decryptedPassword = _encryptionService.Decrypt(smtpConfig.password_encrypted);
                await client.AuthenticateAsync(smtpConfig.username, smtpConfig.password_encrypted);

                var mailSendResponse = await client.SendAsync(message);

                _logger.LogInformation($"Successfully dispatched email to: {toEmail} via SMTP: {smtpConfig.host}");
                
                return mailSendResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MailKit SMTP Failure. Could not send email to {toEmail}");

                // We rethrow the exception so the BackgroundWorker catches it
                // and increments the retry_count in the tbl_email_queue table.
                throw;
            }
            finally
            {
                // Ensure graceful disconnection
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}

