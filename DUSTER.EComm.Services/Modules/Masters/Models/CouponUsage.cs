namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_coupon_usages")]
    public class CouponUsage : BaseEntity
    {
        public CouponUsage()
        {
        }

        [Key]
        public int coupon_usages_id { get; set; }
        public int coupon_id { get; set; }
        public long user_code { get; set; }
        public long order_id { get; set; }
        public decimal discount_applied { get; set; }
        public DateTime used_at { get; set; }
    }

    public class CouponUsageValidator : AbstractValidator<CouponUsage>
    {
        public CouponUsageValidator()
        {
            RuleFor(x => x.coupon_id).GreaterThan(0).WithMessage("Coupon ID must be greater than zero.");
            RuleFor(x => x.user_code).GreaterThan(0).WithMessage("User Code must be greater than zero.");
            RuleFor(x => x.order_id).GreaterThan(0).WithMessage("Order ID must be greater than zero.");
            RuleFor(x => x.discount_applied).GreaterThanOrEqualTo(0).WithMessage("Discount Applied must be non-negative.");
            RuleFor(x => x.used_at).LessThanOrEqualTo(DateTime.Now).WithMessage("Used At cannot be in the future.");
        }
    }
}
