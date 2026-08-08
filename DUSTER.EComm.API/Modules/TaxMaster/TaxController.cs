using DUSTER.EComm.Services.Modules.Tax;
using DUSTER.EComm.Services.Modules.Tax.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.API.Modules.TaxMaster
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxController : ControllerBase
    {
        private readonly ITaxServices _taxServices;
        public TaxController(ITaxServices taxServices)
        {
            _taxServices = taxServices;
        }

        [HttpPost("manage/component")]
        public async Task<IActionResult> ManageTaxComponent([FromBody] TaxComponent component)
        {
            return await _taxServices.ManageTaxComponent(component);
        }

        [HttpGet("get/component/list")]
        public async Task<IActionResult> GetComponentList()
        {
            return await _taxServices.TaxComponentList();
        }

        [HttpGet("get/component/dropdown")]
        public async Task<IActionResult> GetComponentDropdown()
        {
            return await _taxServices.TaxComponentDropdown();
        }

        [HttpPost("manage/class")]
        public async Task<IActionResult> ManageClassComponent([FromBody] TaxClass tClass)
        {
            return await _taxServices.ManageClassComponent(tClass);
        }

        [HttpGet("get/class/list")]
        public async Task<IActionResult> GetClassList()
        {
            return await _taxServices.TaxClassList();
        }

        [HttpGet("get/class/dropdown")]
        public async Task<IActionResult> GetClassDropdown()
        {
            return await _taxServices.TaxClassDropdown();
        }

        [HttpPost("manage/rule")]
        public async Task<IActionResult> ManageTaxRule([FromBody] TaxRule rule)
        {
            return await _taxServices.ManageTaxRule(rule);
        }

        [HttpPost("get/rules/list")]
        public async Task<IActionResult> GetTaxRulesList([FromBody] TaxRuleFilterDto dto)
        {
            return await _taxServices.TaxRulesList(dto);
        }

        [HttpPost("simulator")]
        public async Task<IActionResult> PreviewProductTax([FromBody] TaxPreviewRequestDto request)
        {
            return await _taxServices.PreviewProductTax(request);
        }
    }
}
