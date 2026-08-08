using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Services.Modules.Catelog;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly ICatelogServices _catelogServices;
        private readonly IPostgresDistributedCache _cache;

        public CatalogController(ICatelogServices catelogServices, IPostgresDistributedCache cache)
        {
            _catelogServices = catelogServices;
            _cache = cache;
        }


        [HttpPost("add/product")]
        public async Task<IActionResult> AddProducts(IFormCollection model)
        {
            var result = await _catelogServices.AddProduct(model);
            return result;
        }

        [HttpGet("get/product/{slug}")]
        public async Task<IActionResult> GetProductsBySlugValue(string? slug)
        {
            var result = await _catelogServices.GetProductsBySlugValue(slug);
            return result;
        }

        [HttpPost("edit/product")]
        public async Task<IActionResult> EditProducts(ProductMasterDto model)
        {
            var result = await _catelogServices.EditProduct(model);
            return result;
        }

        [HttpPost("add/product/images")]
        public async Task<IActionResult> AddProductImage(IFormCollection model)
        {
            var result = await _catelogServices.AddProductImages(model);
            return result;
        }

        [HttpPost("remove/product/image")]
        public async Task<IActionResult> DeleteProductImages([FromBody] ProductImagesDto model)
        {
            var result = await _catelogServices.DeleteImages(model.image_ids, model.product_code);
            return result;
        }

        [HttpPost("list")]
        public async Task<IActionResult> ProductList(ProductFilterDto model)
        {
            var result = await _catelogServices.ProductList(model);
            return result;
        }
        [HttpPost("admin/list")]
        public async Task<IActionResult> AdminProductList(ProductFilterDto model)
        {
            var result = await _catelogServices.AdminProductList(model);
            return result;
        }

        [HttpGet("top/selling")]
        public async Task<IActionResult> TopSellingProduct()
        {
            string key = "top_selling_products";

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data, "Top selling products retrieved successfully.");
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.TopSellingProduct();

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }
        }

        [HttpGet("new/arrival")]
        public async Task<IActionResult> NewArrivalProduct()
        {
            string key = "new_arrival_products";

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data, "New arrival products retrieved successfully.");
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.NewArrivalProduct();

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }

        }

        [HttpGet("get/product/images/{product_code}")]
        public async Task<IActionResult> GetProductImagesByProductCode(long product_code)
        {
            var result = await _catelogServices.GetCurrentImagesByProductCode(product_code);
            return result;
        }

        [HttpGet("search/{keyword}")]
        public async Task<IActionResult> ProductSearch(string? keyword)
        {
            try
            {
                return await _catelogServices.ProductSearch(keyword);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("cache/top/selling")]
        [AllowAnonymous]
        public async Task<IActionResult> TopSellingProductFromCache()
        {
            string key = "top_selling_products";

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data, "Top selling products retrieved successfully.");
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.TopSellingProduct();

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }
        }

        [HttpGet("cache/new/arrival")]
        [AllowAnonymous]
        public async Task<IActionResult> NewArrivalProductFromCache()
        {
            string key = "new_arrival_products";

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data, "New arrival products retrieved successfully.");
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.NewArrivalProduct();

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }

        }

        [HttpGet("cache/get/product/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsBySlugValueFromCache(string? slug)
        {
            if (string.IsNullOrEmpty(slug))
                return ResponseEntity<object>.Error(null, "Product Not Found.");

            string key = slug;

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data);
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.GetProductsBySlugValue(slug);

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }
        }

        [HttpPost("cache/list")]
        [AllowAnonymous]
        public async Task<IActionResult> ProductListFromCache(ProductFilterDto model)
        {
            string key = StringHelper.GetBase64String(JsonSerializer.Serialize(model));

            if (await _cache.HasKey(key))
            {
                var data = await _cache.GetObjectAsync<dynamic>(key);
                return ResponseEntity<object>.Success(data);
            }
            else
            {
                var result = (ResponseEntity<object>)await _catelogServices.ProductList(model);

                if (result.StatusCode == 200)
                    await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                return result;
            }
        }
    }
}