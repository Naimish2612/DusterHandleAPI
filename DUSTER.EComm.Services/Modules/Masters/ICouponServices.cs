using DUSTER.EComm.Services.Modules.Masters.Models;

namespace DUSTER.EComm.Services.Modules.Masters
{
    public interface ICouponServices
    {

        #region CouponCategory

        Task<IActionResult> AddCouponCategory(CouponCategory category);
        Task<IActionResult> UpdateCouponCategory(CouponCategory category);
        Task<IActionResult> GetCouponCategories();
        Task<IActionResult> GetCouponCategoryById(int categoryId);
        Task<IActionResult> CouponCategoryDropdown();

        #endregion

        #region Coupon Master

        Task<IActionResult> GenerateUniquePromoCode();
        Task<IActionResult> AddCouponMaster(CouponMaster master);
        Task<IActionResult> UpdateCouponMaster(CouponMaster master);
        Task<IActionResult> GetAllCoupons(CouponFilter filter);
        Task<IActionResult> DeleteCoupon(int coupon_id);
        Task<IActionResult> ValidateAndApplyCoupon(ValidateCouponRequest request);

        #endregion


        #region Coupon Usage 
        Task<IActionResult> AddCouponUsage(CouponUsage usage);

        #endregion
    }
}
