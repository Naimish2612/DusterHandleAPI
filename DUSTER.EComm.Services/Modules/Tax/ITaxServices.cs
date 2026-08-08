using DUSTER.EComm.Services.Modules.Tax.Models;

namespace DUSTER.EComm.Services.Modules.Tax
{
    public interface ITaxServices
    {
        Task<IActionResult> ManageTaxComponent(TaxComponent component);
        Task<IActionResult> TaxComponentList();
        Task<IActionResult> TaxComponentDropdown();
        Task<IActionResult> ManageClassComponent(TaxClass tClass);
        Task<IActionResult> TaxClassList();
        Task<IActionResult> TaxClassDropdown();
        Task<IActionResult> ManageTaxRule(TaxRule rule);
        Task<IActionResult> TaxRulesList(TaxRuleFilterDto dto);
        Task<IActionResult> PreviewProductTax(TaxPreviewRequestDto request);
    }
}
