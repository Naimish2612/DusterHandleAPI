using DUSTER.EComm.Services.Modules.SystemConfig.Models;

namespace DUSTER.EComm.Services.Modules.SystemConfig
{
    public interface ISmtpConfigurationService
    {
        Task<IActionResult> AddAsync(SmtpConfiguration model);
        Task<IActionResult> UpdateAsync(SmtpConfiguration model);
        Task<IActionResult> GetByIdAsync(int id);
        Task<IActionResult> ListAsync();
        Task<IActionResult> GetSmtpCategoryDropdownAsync();
        Task<IActionResult> GetEmailDropdownByCategoryAsync(string smtpCategory);
    }
}
