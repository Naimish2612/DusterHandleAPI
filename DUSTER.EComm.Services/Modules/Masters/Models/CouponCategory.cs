namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_coupon_category")]
    public class CouponCategory : BaseEntity
    {
        public CouponCategory()
        {

        }

        [Key]
        public int coupon_category_id { get; set; }
        public string? category_name { get; set; }
        public int? salesperson_id { get; set; }
        public bool can_be_clubbed { get; set; } = false;
        public bool is_active { get; set; } = true;
    }

    public class CouponCategoryValidator : AbstractValidator<CouponCategory>
    {
        public CouponCategoryValidator()
        {
            RuleFor(x => x.category_name).NotEmpty().WithMessage("Category Name is required.");
        }
    }
}
