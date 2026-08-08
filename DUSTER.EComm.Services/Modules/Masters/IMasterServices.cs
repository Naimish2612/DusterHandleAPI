using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.AspNetCore.Http;

namespace DUSTER.EComm.Services.Modules.Masters
{
    public interface IMasterServices
    {
        #region Product Category

        Task<IActionResult> AddProductCategory(ProductCategory model);
        Task<IActionResult> EditProductCategory(ProductCategory model);
        Task<IActionResult> GetAllProductCategories();
        Task<IActionResult> GetProductCategoryById(int id);
        Task<IActionResult> GetProductCategoriesDropdown();

        #endregion

        #region Product Sub Category

        Task<IActionResult> AddProductSubCategory(ProductSubCategory model);
        Task<IActionResult> EditProductSubCategory(ProductSubCategory model);
        Task<IActionResult> GetAllProductSubCategories(int categoryId);
        Task<IActionResult> GetProductSubCategoryById(int id);
        Task<IActionResult> GetProductSubCategoriesDropdown(int categoryId);
        Task<IActionResult> GetProductbySubCategoryId(int categoryId);

        #endregion

        #region Manufacturer

        Task<IActionResult> AddManufacturer(Manufacturer model);
        Task<IActionResult> EditManufacturer(Manufacturer model);
        Task<IActionResult> GetAllManufacturers();
        Task<IActionResult> GetManufacturerById(int id);
        Task<IActionResult> GetManufacturersDropdown();
        Task<IActionResult> GetManufacturersWithFilter(string? search);

        #endregion

        #region Banner Master
        Task<IActionResult> AddBanner(IFormCollection model);
        Task<IActionResult> EditBanner(IFormCollection model);
        Task<IActionResult> DeleteBanner(long bannerId);
        Task<IActionResult> GetActiveBanners(int? stateId, string? platform = "Both");
        Task<IActionResult> GetAllBanners(BannerFilters filter);
        Task<IActionResult> GetBannerByBannerId(long banner_id);
        Task<IActionResult> ToggleActication(long banner_id);

        #endregion
    }
}
