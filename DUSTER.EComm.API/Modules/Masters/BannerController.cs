using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Services.Modules.Masters;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.AspNetCore.Authorization;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : ControllerBase
    {
        private readonly IMasterServices _masterServices;
        private readonly IPostgresDistributedCache _cache;

        public BannerController(IMasterServices masterServices, IPostgresDistributedCache cache)
        {
            _masterServices = masterServices;
            _cache = cache;
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddBanner(IFormCollection model)
        {
            try
            {
                return await _masterServices.AddBanner(model);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBanner(IFormCollection model)
        {
            try
            {
                return await _masterServices.EditBanner(model);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpDelete("delete/{banner_id}")]
        public async Task<IActionResult> DeleteBanner(long banner_id)
        {
            try
            {
                return await _masterServices.DeleteBanner(banner_id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("active-banners/{stateId}/{platform}")]
        public async Task<IActionResult> GetActiveBanners(int? stateId = 0, string? platform = "Both")
        {
            try
            {
                return await _masterServices.GetActiveBanners(stateId, platform);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetBannerList([FromBody] BannerFilters filters)
        {
            try
            {
                return await _masterServices.GetAllBanners(filters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("{banner_id}")]
        public async Task<IActionResult> GetBannerByBannerId(int banner_id)
        {
            try
            {
                return await _masterServices.GetBannerByBannerId(banner_id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("{banner_id}")]
        public async Task<IActionResult> ToggleActivation(int banner_id)
        {
            try
            {
                return await _masterServices.ToggleActication(banner_id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("cache/active-banners/{stateId}/{platform}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveBannersFromCache(int? stateId = 0, string? platform = "Both")
        {
            try
            {
                string key = $"cache_banner_{stateId}_{platform}";

                if (await _cache.HasKey(key))
                {
                    var data = await _cache.GetObjectAsync<dynamic>(key);
                    return ResponseEntity<object>.Success(data);
                }
                else
                {
                    var result = (ResponseEntity<object>)await _masterServices.GetActiveBanners(stateId, platform);

                    if (result.StatusCode == 200)
                        await _cache.SetObjectAsync<dynamic>(key, result.Data, TimeSpan.FromMinutes(5));

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
