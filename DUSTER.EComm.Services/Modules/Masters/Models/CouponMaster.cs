using DUSTER.EComm.Data.Helpers.Pagination;

namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_coupon_masters")]
    public class CouponMaster : BaseEntity
    {
        public CouponMaster()
        {

        }

        [Key]
        public int coupon_id { get; set; }
        public int coupon_category_id { get; set; }
        public string coupon_name { get; set; }
        public string coupon_code { get; set; }
        public string? coupon_description { get; set; }
        [Computed]
        public string? category_name { get; set; }

        /// <summary>
        /// Discount formulation: 'Percentage' or 'Value'
        /// </summary>
        public string discount_type { get; set; } = "Value";
        public decimal discount_value { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public bool multiple_time_use { get; set; } = true;
        public int max_usages_total { get; set; } = 0; // 0 = Unlimited
        public int max_usages_per_user { get; set; } = 1;
        public decimal minimum_order_amount { get; set; } = 0.00m;
        public decimal? max_discount_amount { get; set; } // Max discount cap for Percentage values
        public bool is_active { get; set; } = true;
        public bool is_deleted { get; set; } = false;

        [Computed]
        public int? salesperson_id { get; set; }
        [Computed]
        public bool can_be_clubbed { get; set; } = false;
    }

    public class CouponMasterValidator : AbstractValidator<CouponMaster>
    {
        public CouponMasterValidator()
        {
            RuleFor(x => x.coupon_name).NotEmpty().WithMessage("Coupon Name is required.");
            RuleFor(x => x.coupon_code).NotEmpty().WithMessage("Coupon Code is required.");
            RuleFor(x => x.discount_type).Must(dt => dt == "Percentage" || dt == "Value")
                .WithMessage("Discount Type must be either 'Percentage' or 'Value'.");
            RuleFor(x => x.discount_value).GreaterThan(0).WithMessage("Discount Value must be greater than zero.");
            RuleFor(x => x.start_date).LessThan(x => x.end_date).WithMessage("Start Date must be before End Date.");
        }
    }

    public class ValidateCouponRequest
    {
        public string? coupon_code { get; set; }
        public long? user_code { get; set; }
        public decimal order_amount { get; set; }
        public List<string> current_applied_coupons { get; set; } = [];
        public int? salesperson_id { get; set; }
    }

    public class CouponFilter : PaginationParams
    {
        public int? coupon_category_id { get; set; }
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
    }
}
