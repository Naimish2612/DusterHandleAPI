using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Masters.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Masters
{
    public class CouponServices : ICouponServices
    {
        private readonly IEIPLRepository<CouponCategory> _categoryRepo;
        private readonly IEIPLRepository<CouponMaster> _couponRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IEIPLRepository<CouponUsage> _couponUsageRepo;

        public CouponServices(ICurrentUserService currentUser, IEIPLRepository<CouponCategory> categoryRepo, IEIPLRepository<CouponMaster> couponRepo, IEIPLRepository<CouponUsage> couponUsageRepo)
        {
            _currentUser = currentUser;
            _categoryRepo = categoryRepo;
            _couponRepo = couponRepo;
            _couponUsageRepo = couponUsageRepo;
        }

        #region CouponCategory

        public async Task<IActionResult> AddCouponCategory(CouponCategory category)
        {
            try
            {
                if (category == null)
                    return ResponseEntity<object>.Error(null, "Invalid category data");

                var validator = await _categoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new CouponCategoryValidator(), Model = category });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _categoryRepo.InsertAsync(category));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Category added coupon successfully");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add coupon category");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UpdateCouponCategory(CouponCategory category)
        {
            try
            {
                if (category == null || category.coupon_category_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid category data");

                var validator = await _categoryRepo.ModelValidating(new ValidationModel() { ValidateModel = new CouponCategoryValidator(), Model = category });
                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _categoryRepo.UpdateAsync(category));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Coupon Category updated successfully");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update coupon category");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetCouponCategories()
        {
            try
            {
                var categories = await _categoryRepo.QueryAsync<CouponCategory>("select * from tbl_coupon_category");

                if (categories.Any())
                    return ResponseEntity<object>.Success(categories, "Coupon categories retrieved successfully");
                else
                    return ResponseEntity<object>.Error(null, "Coupon categories not found");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetCouponCategoryById(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid category ID");

                var param = new DynamicParameters();
                param.Add("categoryId", categoryId);

                var category = await _categoryRepo.GetSingleOrDefaultAsync("select * from tbl_coupon_category where coupon_category_id = @categoryId and is_active= true", param);
                if (category != null)
                    return ResponseEntity<object>.Success(category, "Coupon category retrieved successfully");
                else
                    return ResponseEntity<object>.Error(null, "Coupon category not found");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> CouponCategoryDropdown()
        {
            try
            {

                var categories = await _categoryRepo.GetDropdownAsync(new DropdownRequestModel() { table_name = "tbl_coupon_category", table_columns = "coupon_category_id as id, category_name as value" });
                if (categories.Any())
                    return ResponseEntity<object>.Success(categories, "Coupon categories retrieved successfully");
                else
                    return ResponseEntity<object>.Error(null, "Coupon categories not found");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Coupon Master

        public async Task<IActionResult> GenerateUniquePromoCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            bool isUnique = false;

            do
            {
                var sb = new StringBuilder(7);
                for (int i = 0; i < 6; i++)
                {
                    sb.Append(chars[random.Next(chars.Length)]);
                }
                code = sb.ToString();

                var checkSql = "SELECT COUNT(1) FROM tbl_coupon_masters WHERE coupon_code = @Code";

                var parameters = new DynamicParameters();
                parameters.Add("Code", code);

                var exists = await _couponRepo.QueryAsync<int>(checkSql, parameters);

                if (exists.FirstOrDefault() == 0)
                {
                    isUnique = true;
                }

            } while (!isUnique);

            return ResponseEntity<object>.Success(new { coupon_code = code }, "Unique promo code generated successfully");
        }

        public async Task<IActionResult> AddCouponMaster(CouponMaster master)
        {
            try
            {
                if (master == null)
                    return ResponseEntity<object>.Error(null, "Invalid coupon data");

                var checkSql = "SELECT COUNT(1) FROM tbl_coupon_masters WHERE coupon_code = @Code";
                var parameters = new DynamicParameters();
                parameters.Add("Code", master.coupon_code);

                var exists = await _couponRepo.QueryAsync<int>(checkSql, parameters);

                if (exists.FirstOrDefault() > 0)
                    return ResponseEntity<object>.Error(null, "Duplicate Coupon Code found, Check and re-enter.");

                var validator = await _couponRepo.ModelValidating(new ValidationModel() { ValidateModel = new CouponMasterValidator(), Model = master });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _couponRepo.InsertAsync(master));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Coupon added successfully");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add coupon");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UpdateCouponMaster(CouponMaster master)
        {
            try
            {
                if (master == null || master.coupon_id <= 0)
                    return ResponseEntity<object>.Error(null, "Invalid coupon data");

                var checkSql = "SELECT COUNT(1) FROM tbl_coupon_masters WHERE coupon_code = @Code AND coupon_id != @Id";

                var parameters = new DynamicParameters();
                parameters.Add("Code", master.coupon_code);
                parameters.Add("Id", master.coupon_id);

                var exists = await _couponRepo.QueryAsync<int>(checkSql, parameters);

                if (exists.FirstOrDefault() > 0)
                    return ResponseEntity<object>.Error(null, "Duplicate Coupon Code found, Check and re-enter.");

                var validator = await _couponRepo.ModelValidating(new ValidationModel() { ValidateModel = new CouponMasterValidator(), Model = master });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _couponRepo.UpdateAsync(master));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Coupon updated successfully");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update coupon");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllCoupons(CouponFilter filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                string sql = @"
                    SELECT cm.*, cc.category_name, cc.salesperson_id, cc.can_be_clubbed, cm.is_active
                    FROM tbl_coupon_masters cm
                    INNER JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id
                    WHERE cm.is_deleted = false";

                if (filter.coupon_category_id.HasValue && filter.coupon_category_id.Value > 0)
                {
                    sql += " AND cm.coupon_category_id = @coupon_category_id";
                    parameters.Add("coupon_category_id", filter.coupon_category_id.Value);
                }

                if (filter.start_date.HasValue && filter.end_date.HasValue)
                {
                    sql += " AND (cm.start_date <= @EndDateFilter AND cm.end_date >= @StartDateFilter)";
                    parameters.Add("StartDateFilter", filter.start_date.Value);
                    parameters.Add("EndDateFilter", filter.end_date.Value);
                }
                else if (filter.start_date.HasValue)
                {
                    sql += " AND cm.start_date >= @StartDateFilter";
                    parameters.Add("StartDateFilter", filter.start_date.Value.Date);
                }
                else if (filter.end_date.HasValue)
                {
                    sql += " AND cm.end_date <= @EndDateFilter";
                    parameters.Add("EndDateFilter", filter.end_date.Value.Date.AddDays(1).AddTicks(-1));
                }

                sql += " ORDER BY cm.created_at DESC";

                //var data = await _couponRepo.QueryPagedAsync<CouponMaster>(sql, filter, parameters);
                var data = await _couponRepo.QueryAsync<CouponMaster>(sql, parameters);

                if (data != null && data.Any())
                    return ResponseEntity<object>.Success(data, "Coupons retrieved successfully");
                else
                    return ResponseEntity<object>.Error(null, "No coupons found");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> DeleteCoupon(int coupon_id)
        {
            if (coupon_id <= 0)
                return ResponseEntity<object>.Error(null, "Invalid coupon ID");

            var param = new DynamicParameters();
            param.Add("Id", coupon_id);

            var coupon = await _couponRepo.GetSingleOrDefaultAsync("SELECT * FROM coupon_masters WHERE coupon_id = @Id", param);

            if (coupon == null)
                return ResponseEntity<object>.Error(null, "Coupon not found");

            coupon.is_deleted = true;
            int response = Convert.ToInt32(await _couponRepo.UpdateAsync(coupon));

            if (response > 0)
                return ResponseEntity<object>.Success(null, "Coupon soft-deleted successfully");
            else
                return ResponseEntity<object>.Error(null, "Failed to delete coupon");
        }

        public async Task<IActionResult> ValidateAndApplyCoupon(ValidateCouponRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.coupon_code))
                    return ResponseEntity<object>.Error(null, "Coupon code is required");

                //coupon code available or not.
                string couponQuery = @"
                    SELECT cm.*, cc.can_be_clubbed, cc.salesperson_id
                    FROM tbl_coupon_masters cm
                    INNER JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id
                    WHERE cm.coupon_code = @Code AND cm.is_active = true AND cm.is_deleted = false";

                var param = new DynamicParameters();
                param.Add("Code", request.coupon_code.ToUpper().Trim());

                var coupon = await _couponRepo.GetSingleOrDefaultAsync(couponQuery, param);

                if (coupon == null)
                    return ResponseEntity<object>.Error(null, "Coupon code not found.");

                // Convert dynamic back to logic fields
                DateTime startDate = coupon.start_date;
                DateTime endDate = coupon.end_date;
                decimal minOrderAmt = coupon.minimum_order_amount;
                int maxUsagesTotal = coupon.max_usages_total;
                int maxUsagesPerUser = coupon.max_usages_per_user;
                bool canBeClubbed = coupon.can_be_clubbed;
                int? categorySalespersonId = coupon.salesperson_id;
                int couponId = coupon.coupon_id;
                string discountType = coupon.discount_type;
                decimal discountValue = coupon.discount_value;
                decimal? maxDiscountCap = coupon.max_discount_amount;

                //Date Range Verification
                DateTime now = DateTime.Now;
                if (now < startDate || now > endDate)
                    return ResponseEntity<object>.Error(null, "This promo code is either not active yet or has expired.");

                //Minimum Order Value Check
                if (request.order_amount < minOrderAmt)
                    return ResponseEntity<object>.Error(null, $"This coupon requires a minimum purchase order value of {minOrderAmt:F2}.");

                //Salesperson Specific Verification
                if (categorySalespersonId.HasValue && categorySalespersonId.Value != 0)
                {
                    if (!request.salesperson_id.HasValue || request.salesperson_id.Value != categorySalespersonId.Value)
                        return ResponseEntity<object>.Error(null, "This coupon is reserved only for transactions verified by designated sales personnel.");
                }

                //Coupon Combination / Clubbing Rules Check
                if (request.current_applied_coupons != null && request.current_applied_coupons.Any())
                {
                    // If current coupon can't be clubbed, reject straight away
                    if (!canBeClubbed)
                        return ResponseEntity<object>.Error(null, "This coupon cannot be combined with other active discount programs.");

                    //Check if existing applied coupons allow clubbing
                    string clubCheckSql = @"
                        SELECT COUNT(1) 
                        FROM tbl_coupon_masters cm
                        JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id
                        WHERE cm.coupon_code = ANY(@ExistingCodes) AND cc.can_be_clubbed = false";

                    var clubParams = new DynamicParameters();
                    clubParams.Add("ExistingCodes", request.current_applied_coupons.ToArray());

                    var restrictedCount = await _couponRepo.QueryAsync<int>(clubCheckSql, clubParams);
                    if (restrictedCount.FirstOrDefault() > 0)
                        return ResponseEntity<object>.Error(null, "One or more coupons currently applied in your cart cannot be combined with additional promotions.");
                }

                //Absolute Coupon Global Usage Limit check
                if (maxUsagesTotal > 0)
                {
                    string totalUsageSql = "SELECT COUNT(1) FROM tbl_coupon_usages WHERE coupon_id = @CouponId";
                    var totalUsageParams = new DynamicParameters();
                    totalUsageParams.Add("CouponId", couponId);

                    var timesUsed = await _couponRepo.QueryAsync<int>(totalUsageSql, totalUsageParams);
                    if (timesUsed.FirstOrDefault() >= maxUsagesTotal)
                        return ResponseEntity<object>.Error(null, "This promotion has reached its maximum utilization capacity.");
                }

                //Single-User Redemption Limit check (Per customer profile)
                if (maxUsagesPerUser > 0)
                {
                    string userUsageSql = "SELECT COUNT(1) FROM tbl_coupon_usages WHERE coupon_id = @CouponId AND user_code = @UserId";
                    var userUsageParams = new DynamicParameters();
                    userUsageParams.Add("CouponId", couponId);
                    userUsageParams.Add("UserId", request.user_code);

                    var userTimesUsed = await _couponRepo.QueryAsync<int>(userUsageSql, userUsageParams);
                    if (userTimesUsed.FirstOrDefault() >= maxUsagesPerUser)
                        return ResponseEntity<object>.Error(null, $"You have reached the maximum allowed usage limit ({maxUsagesPerUser}) for this discount code.");
                }

                //Calculate Final Discount Value based on formula type
                decimal discountAmount = 0.00m;
                if (discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                {
                    discountAmount = request.order_amount * (discountValue / 100m);

                    // Enforce percentage discount capping
                    if (maxDiscountCap.HasValue && maxDiscountCap.Value > 0 && discountAmount > maxDiscountCap.Value)
                    {
                        discountAmount = maxDiscountCap.Value;
                    }
                }
                else // Fixed Flat Amount Value representation
                {
                    discountAmount = discountValue;
                }

                // Ensure the coupon does not discount more than the actual order size
                if (discountAmount > request.order_amount)
                {
                    discountAmount = request.order_amount;
                }

                return ResponseEntity<object>.Success(new
                {
                    coupon_id = couponId,
                    coupon_code = request.coupon_code.ToUpper().Trim(),
                    discount_amount = Math.Round(discountAmount, 2)
                }, "Coupon applied successfully!");

            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, "An error occurred while validating the coupon: ");
            }
        }

        #endregion

        #region Coupon Usage
         
        public async Task<IActionResult> AddCouponUsage(CouponUsage usage)
        {
            try
            {
                if (usage == null)
                    return ResponseEntity<object>.Error(null, "Invalid Coupan usage data");

                var validator = await _couponUsageRepo.ModelValidating(new ValidationModel() { ValidateModel = new CouponUsageValidator(), Model = usage });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                int response = Convert.ToInt32(await _couponUsageRepo.InsertAsync(usage));

                if (response > 0)
                    return ResponseEntity<object>.Success(null, "Coupon usage added successfully");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add coupon usage");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
