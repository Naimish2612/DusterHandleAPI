using DUSTER.EComm.Services.Modules.Masters;
using DUSTER.EComm.Services.Modules.Masters.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DUSTER.EComm.API.Modules.Masters
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        ICouponServices _couponServices;
        public CouponController(ICouponServices couponServices)
        {
            _couponServices = couponServices;
        }

        #region Coupon Category

        [HttpPost("add/category")]
        public async Task<IActionResult> AddCouponCategory([FromBody] CouponCategory category)
        {
            return await _couponServices.AddCouponCategory(category);
        }

        [HttpPost("update/category")]
        public async Task<IActionResult> UpdateCouponCategory([FromBody] CouponCategory category)
        {
            return await _couponServices.UpdateCouponCategory(category);
        }

        [HttpGet("get-coupon-category")]
        public async Task<IActionResult> GetCouponCategories()
        {
            return await _couponServices.GetCouponCategories();
        }

        [HttpGet("get-coupon-category/{categoryId}")]
        public async Task<IActionResult> GetCouponCategoryById(int categoryId)
        {
            return await _couponServices.GetCouponCategoryById(categoryId);
        }

        [HttpGet("category/dropdown")]
        public async Task<IActionResult> CouponCategoryDropdown()
        {
            return await _couponServices.CouponCategoryDropdown();
        }

        #endregion

        #region Coupon Master

        [HttpPost("create")]
        public async Task<IActionResult> AddCouponMaster([FromBody] CouponMaster master)
        {
            return await _couponServices.AddCouponMaster(master);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateCouponMaster([FromBody] CouponMaster master)
        {
            return await _couponServices.UpdateCouponMaster(master);
        }

        [HttpPost("get/all")]
        public async Task<IActionResult> GetAllCoupons([FromBody] CouponFilter filter)
        {
            return await _couponServices.GetAllCoupons(filter);
        }

        [HttpDelete("delete/{coupon_id}")]
        public async Task<IActionResult> DeleteCoupon(int coupon_id)
        {
            return await _couponServices.DeleteCoupon(coupon_id);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateAndApplyCoupon([FromBody] ValidateCouponRequest request)
        {
            return await _couponServices.ValidateAndApplyCoupon(request);
        }

        [HttpGet("get/new/coupon")]
        public async Task<IActionResult> GetCouponsCode()
        {
            return await _couponServices.GenerateUniquePromoCode();
        }

        #endregion

    }
}
