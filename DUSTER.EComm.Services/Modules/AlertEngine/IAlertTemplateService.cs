using DUSTER.EComm.Services.Modules.AlertEngine.Models;

namespace DUSTER.EComm.Services.Modules.AlertEngine
{
    public interface IAlertTemplateService
    {
        Task<IActionResult> AddAsync(EmailTemplate model);
        Task<IActionResult> UpdateAsync(EmailTemplate model);
        Task<IActionResult> GetByIdAsync(int id);
        Task<IActionResult> ListAsync();
    }
}
