using DUSTER.EComm.Services.Modules.SystemConfig;
using DUSTER.EComm.Services.Modules.SystemConfig.Models;

namespace DUSTER.EComm.API.Modules.SystemConfig
{
    [Route("api")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISmtpConfigurationService _smtpConfigService;

        public SystemConfigController(ISmtpConfigurationService smtpConfigService)
        {
            _smtpConfigService = smtpConfigService;
        }

        [HttpPost("add/smtp/config")]
        public async Task<IActionResult> Add([FromBody] SmtpConfiguration model)
        {
            return await _smtpConfigService.AddAsync(model);
        }

        [HttpPost("edit/smtp/config")]
        public async Task<IActionResult> Update([FromBody] SmtpConfiguration model)
        {
            return await _smtpConfigService.UpdateAsync(model);
        }

        [HttpGet("get/smtp/config/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            return await _smtpConfigService.GetByIdAsync(id);
        }

        [HttpGet("get/smtp/config/list")]
        public async Task<IActionResult> List()
        {
            return await _smtpConfigService.ListAsync();
        }

        [HttpGet("dropdown/smtp/category")]
        public async Task<IActionResult> GetSmtpCategoryDropdown()
        {
            return await _smtpConfigService.GetSmtpCategoryDropdownAsync();
        }

        [HttpGet("dropdown/smtp/email/{smtpCategory}")]
        public async Task<IActionResult> GetEmailDropdownByCategory(string smtpCategory)
        {
            return await _smtpConfigService.GetEmailDropdownByCategoryAsync(smtpCategory);
        }
    }
}
