using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.Services.Modules.Notifications
{
    public interface IEmailNotificationServices
    {
        Task<IActionResult> SendEmail(string email, string subject, string body, string otherValue = null);
    }
}
