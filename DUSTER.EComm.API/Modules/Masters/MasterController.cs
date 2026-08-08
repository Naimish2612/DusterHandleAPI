using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Services.Modules.Masters;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.AspNetCore.Authorization;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterController : ControllerBase
    {
        private readonly IMasterServices _masterServices;
        private readonly IPostgresDistributedCache _cache;

        public MasterController(IMasterServices masterServices, IPostgresDistributedCache cache)
        {
            _masterServices = masterServices;
            _cache = cache;
        }

        #region Product Category

        [HttpGet("get/product/category/list")]
        public async Task<IActionResult> GetAllProductCategories()
        {
            try
            {
                return await _masterServices.GetAllProductCategories();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/product/category/{id}")]
        public async Task<IActionResult> GetProductCategoryById(int id)
        {
            try
            {
                return await _masterServices.GetProductCategoryById(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("dropdown/product/category")]
        public async Task<IActionResult> GetProductCategoriesDropdown()
        {
            try
            {
                return await _masterServices.GetProductCategoriesDropdown();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("create/product/category")]
        public async Task<IActionResult> AddProductCategory([FromBody] ProductCategory model)
        {
            try
            {
                return await _masterServices.AddProductCategory(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("edit/product/category")]
        public async Task<IActionResult> EditProductCategory([FromBody] ProductCategory model)
        {
            try
            {
                return await _masterServices.EditProductCategory(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("cache/product/category")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductCategoriesFromCache()
        {
            try
            {
                string key = "cache_category";

                if (await _cache.HasKey(key))
                {
                    var data = await _cache.GetObjectAsync<dynamic>(key);
                    return ResponseEntity<object>.Success(data);
                }
                else
                {
                    var result = (ResponseEntity<object>)await _masterServices.GetProductCategoriesDropdown();

                    if (result.StatusCode == 200)
                        await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                    return result;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        #endregion

        #region Product Sub Category

        [HttpGet("get/product/subcategory/list/{categoryId}")]
        public async Task<IActionResult> GetAllProductSubCategories(int categoryId)
        {
            try
            {
                return await _masterServices.GetAllProductSubCategories(categoryId);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/product/subcategory/{id}")]
        public async Task<IActionResult> GetProductSubCategoryById(int id)
        {
            try
            {
                return await _masterServices.GetProductSubCategoryById(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("dropdown/product/subcategory/{categoryId}")]
        public async Task<IActionResult> GetProductSubCategoriesDropdown(int categoryId)
        {
            try
            {
                return await _masterServices.GetProductSubCategoriesDropdown(categoryId);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("create/product/subcategory")]
        public async Task<IActionResult> AddProductSubCategory([FromBody] ProductSubCategory model)
        {
            try
            {
                return await _masterServices.AddProductSubCategory(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("edit/product/subcategory")]
        public async Task<IActionResult> EditProductSubCategory([FromBody] ProductSubCategory model)
        {
            try
            {
                return await _masterServices.EditProductSubCategory(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("cache/product/subcategory/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductSubCategoriesFromCache(int categoryId)
        {
            try
            {
                string key = $"cache_subcategory_{categoryId}";

                if (await _cache.HasKey(key))
                {
                    var data = await _cache.GetObjectAsync<dynamic>(key);
                    return ResponseEntity<object>.Success(data);
                }
                else
                {
                    var result = (ResponseEntity<object>)await _masterServices.GetProductSubCategoriesDropdown(categoryId);

                    if (result.StatusCode == 200)
                        await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpGet("dropdown/product/by/subcategory/{subcategoryId}")]
        public async Task<IActionResult> GetProductbySubCategoryId(int subcategoryId)
        {
            try
            {
                return await _masterServices.GetProductbySubCategoryId(subcategoryId);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region Manufacturer

        [HttpGet("get/manufacturer/list")]
        public async Task<IActionResult> GetAllManufacturers()
        {
            try
            {
                return await _masterServices.GetAllManufacturers();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/manufacturer/{id}")]
        public async Task<IActionResult> GetManufacturerById(int id)
        {
            try
            {
                return await _masterServices.GetManufacturerById(id);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("dropdown/manufacturer")]
        public async Task<IActionResult> GetManufacturersDropdown()
        {
            try
            {
                return await _masterServices.GetManufacturersDropdown();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("get/manufacturer/filter")]
        public async Task<IActionResult> GetManufacturersWithFilter(string? search = "")
        {
            try
            {
                return await _masterServices.GetManufacturersWithFilter(search);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("create/manufacturer")]
        public async Task<IActionResult> AddManufacturer([FromBody] Manufacturer model)
        {
            try
            {
                return await _masterServices.AddManufacturer(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("edit/manufacturer")]
        public async Task<IActionResult> EditManufacturer([FromBody] Manufacturer model)
        {
            try
            {
                return await _masterServices.EditManufacturer(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("cache/manufacturer")]
        [AllowAnonymous]
        public async Task<IActionResult> GetManufacturersFromCache()
        {
            try
            {
                string key = "cache_manufacturer";

                if (await _cache.HasKey(key))
                {
                    var data = await _cache.GetObjectAsync<dynamic>(key);
                    return ResponseEntity<object>.Success(data);
                }
                else
                {
                    var result = (ResponseEntity<object>)await _masterServices.GetManufacturersDropdown();

                    if (result.StatusCode == 200)
                        await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                    return result;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

    }
}
