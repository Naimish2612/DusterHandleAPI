using DUSTER.EComm.Services.Modules.SystemConfig.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.SystemConfig
{
    public interface ISmtpDeliveryService
    {
        Task<string> SendEmailAsync(SmtpConfiguration smtpConfig, string toEmail, string toName, string subject, string bodyHtml, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null);
    }
}
