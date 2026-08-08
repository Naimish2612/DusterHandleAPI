using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.Notifications;
using DUSTER.EComm.Services.Modules.Notifications.Models;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using static System.Net.WebRequestMethods;

namespace DUSTER.EComm.Services.Modules.Notification
{
    public class EmailNotificationServices : IEmailNotificationServices
    {
        private readonly EmailGmail _mailSettings;
        public readonly ICurrentUserService _currentUserService;
        private readonly IEIPLRepository<NotificationModel> _notificationRepo;
        private readonly IEIPLRepository<UsersModel> _usersRepo;
        private readonly IAlertEngineService _alertEngineService;

        public EmailNotificationServices(IOptions<EmailGmail> mailSettings, ICurrentUserService currentUserService, IEIPLRepository<NotificationModel> notificationRepo, IEIPLRepository<UsersModel> usersRepo, IAlertEngineService alertEngineService)
        {
            _mailSettings = mailSettings.Value;
            _currentUserService = currentUserService;
            _notificationRepo = notificationRepo;
            _usersRepo = usersRepo;
            _alertEngineService = alertEngineService;
        }

        public async Task<IActionResult> SendEmail(string email, string subject, string body, string otherValue = null)
        {
            try
            {
                if (email == null || subject == null || body == null)
                    return ResponseEntity<object>.Error(null, "Email, Subject and Body cannot be null.");

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_mailSettings.Mail);
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.IsBodyHtml = true;
                mailMessage.Body = body;

                var smtp = new System.Net.Mail.SmtpClient(_mailSettings.Host, Convert.ToInt32(_mailSettings.Port));
                smtp.Credentials = new System.Net.NetworkCredential(_mailSettings.Mail, _mailSettings.Password);
                smtp.EnableSsl = true;
                try
                {
                    //smtp.Send(mailMessage);

                    var emailPayload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "otp", otherValue },
                        { "user_name", body }
                    };
                    await _alertEngineService.QueueEventNotificationAsync("OTP", email, body, emailPayload);

                    long userCode = _currentUserService.User.user_code;
                    if (userCode == 0)
                    {
                        UsersModel usersModel = await _usersRepo.GetSingleOrDefaultAsync($"select * from tbl_users where email_id='{email}'");
                        if (usersModel != null)
                        {
                            userCode = usersModel.user_code;
                        }
                    }

                    NotificationModel notificationModel = new NotificationModel()
                    {
                        user_code = userCode,
                        notification_date = DateTime.Now,
                        notification_type = "EMAIL",
                        notification_key = email,
                        notification_value = otherValue,
                        notification_body = body,
                        status = 0
                    };

                    var insertResponse = await _notificationRepo.InsertAsync(notificationModel);

                    return ResponseEntity<object>.Success(null, "Email sent successfully.");
                }
                catch (Exception ss)
                {
                    throw ss;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
