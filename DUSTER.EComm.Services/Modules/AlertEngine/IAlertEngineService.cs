namespace DUSTER.EComm.Services.Modules.AlertEngine
{
    public interface IAlertEngineService
    {
        Task<string> RenderTemplate(string templateContent, string jsonPayload);
        Task<IActionResult> QueueEventNotificationAsync(string eventCode, string toEmail, string toName, object dynamicPayload);
        Task<IActionResult> GetEmailQueueByIdAsync(long id);
        Task<IActionResult> EmailQueueListAsync(string? status = null);
    }
}
