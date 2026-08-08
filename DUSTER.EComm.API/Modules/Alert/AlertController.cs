using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Services.Modules.AlertEngine;
using DUSTER.EComm.Services.Modules.AlertEngine.Models;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.Notifications;
using DUSTER.EComm.Services.Modules.Notifications.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace DUSTER.EComm.API.Modules.Alert
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertController : ControllerBase
    {
        private readonly IEmailNotificationServices _emailNotification;
        private readonly IAlertTemplateService _alertTemplateService;
        private readonly IAlertEngineService _alertEngineService;

        IEIPLRepository<UsersModel> _userRepo;
        IEIPLRepository<NotificationModel> _notificationRepo;
        public AlertController(IEmailNotificationServices emailNotification, IAlertTemplateService alertTemplateService, IAlertEngineService alertEngineService,
            IEIPLRepository<UsersModel> userRepo, IEIPLRepository<NotificationModel> notificationRepo)
        {
            _emailNotification = emailNotification;
            _alertTemplateService = alertTemplateService;
            _alertEngineService = alertEngineService;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
        }


        [HttpGet]
        [Route("send/email/for/email/verification")]
        public async Task<IActionResult> SendEmailVerificationOTP(string? email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return ResponseEntity<object>.Error(null, "Email cannot be null or empty.", System.Net.HttpStatusCode.InternalServerError);


                string otpTemplate = @"<!DOCTYPE html>
                                        <html>
                                        <head>
                                            <meta charset=""UTF-8"">
                                            <title>OTP Verification</title>
                                        </head>
                                        <body style=""font-family: Arial, sans-serif; background-color:#f4f4f4; padding:20px;"">
                                            <div style=""max-width:500px; margin:auto; background:#ffffff; padding:20px; border-radius:8px; text-align:center;"">
        
                                                <h2 style=""color:#333;"">Verify Your Email</h2>
        
                                                <p>Your One-Time Password (OTP) is:</p>
        
                                                <div style=""font-size:28px; font-weight:bold; letter-spacing:5px; color:#2c3e50; margin:20px 0;"">
                                                    ##OTP##
                                                </div>
        
                                                <p>This OTP is valid for <strong>5 minutes</strong>.</p>
        
                                                <p style=""color:#888; font-size:12px;"">
                                                    Do not share this code with anyone. If you didn’t request this, ignore this email.
                                                </p>
        
                                                <hr>
        
                                                <p style=""font-size:12px; color:#aaa;"">
                                                    © ##YEAR## ##APP_NAME##. All rights reserved.
                                                </p>
                                            </div>
                                        </body>
                                        </html>";

                string otp = new Random().Next(100000, 999999).ToString();
                otpTemplate = otpTemplate.Replace("##OTP##", otp);
                otpTemplate = otpTemplate.Replace("##YEAR##", DateTime.Now.Year.ToString());
                otpTemplate = otpTemplate.Replace("##APP_NAME##", "Everest E-Commerce");

                return await _emailNotification.SendEmail(email, "OTP Code for Email Verification", otpTemplate, otp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        [Route("send/otp/for/password/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOTPForPasswordReset(string? email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return ResponseEntity<object>.Error(null, "Email cannot be null or empty.", System.Net.HttpStatusCode.InternalServerError);


                string otpTemplate = @"<!DOCTYPE html>
                                        <html>
                                        <head>
                                            <meta charset=""UTF-8"">
                                            <title>Reset Passowrd OTP</title>
                                        </head>
                                        <body style=""font-family: Arial, sans-serif; background-color:#f4f4f4; padding:20px;"">
                                            <div style=""max-width:500px; margin:auto; background:#ffffff; padding:20px; border-radius:8px; text-align:center;"">
        
                                                <h2 style=""color:#333;"">Password Reset OTP</h2>
        
                                                <p>Your One-Time Password (OTP) is:</p>
        
                                                <div style=""font-size:28px; font-weight:bold; letter-spacing:5px; color:#2c3e50; margin:20px 0;"">
                                                    ##OTP##
                                                </div>
        
                                                <p>This OTP is valid for <strong>5 minutes</strong>.</p>
        
                                                <p style=""color:#888; font-size:12px;"">
                                                    Do not share this code with anyone. If you didn’t request this, ignore this email.
                                                </p>
        
                                                <hr>
        
                                                <p style=""font-size:12px; color:#aaa;"">
                                                    © ##YEAR## ##APP_NAME##. All rights reserved.
                                                </p>
                                            </div>
                                        </body>
                                        </html>";

                string otp = new Random().Next(100000, 999999).ToString();
                otpTemplate = otpTemplate.Replace("##OTP##", otp);
                otpTemplate = otpTemplate.Replace("##YEAR##", DateTime.Now.Year.ToString());
                otpTemplate = otpTemplate.Replace("##APP_NAME##", "Everest E-Commerce");

                var param = new DynamicParameters();
                param.Add("email", email);
                UsersModel userData = await _userRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_users WHERE email_id = @email", param);

                if (userData == null)
                    return ResponseEntity<object>.Error(null, "User not found, please contact your administrator.");

                var parameters = new DynamicParameters();
                parameters.Add("user_code", userData.user_code);

                var notification = await _notificationRepo.GetSingleOrDefaultAsync("SELECT * FROM tbl_notification WHERE user_code = @user_code AND status =0 ", parameters);
                if (notification != null)
                {
                    notification.status = 1;
                    var notificationUpdateResponse = await _notificationRepo.UpdateAsync(notification);
                }
                 
                return await _emailNotification.SendEmail(email, "OTP Code for Password Reset.", userData.user_name, otp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("add/template")]
        public async Task<IActionResult> Add([FromBody] EmailTemplate model)
        {
            return await _alertTemplateService.AddAsync(model);
        }

        [HttpPost("edit/template")]
        public async Task<IActionResult> Update([FromBody] EmailTemplate model)
        {
            return await _alertTemplateService.UpdateAsync(model);
        }

        [HttpGet("get/template/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return await _alertTemplateService.GetByIdAsync(id);
        }

        [HttpGet("get/template/list")]
        public async Task<IActionResult> List()
        {
            return await _alertTemplateService.ListAsync();
        }

        [HttpGet("get/email/queue/{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            return await _alertEngineService.GetEmailQueueByIdAsync(id);
        }

        [HttpGet("get/email/queue/list/{status}")]
        public async Task<IActionResult> List(string? status)
        {
            return await _alertEngineService.EmailQueueListAsync(status);
        }

        [HttpPost("send/email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailQueue model)
        {
            return await _alertEngineService.QueueEventNotificationAsync(model.event_code, model.to_email, model.to_name, model.payload_json_obj);
        }
    }
}
