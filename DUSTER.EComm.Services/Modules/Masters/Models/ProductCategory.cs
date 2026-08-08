namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_product_category")]
    public class ProductCategory : BaseEntity
    {
        [Key]
        public int category_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public bool is_active { get; set; }
        public string? slug { get; set; }

    }

    public class ProductCategoryValidator : AbstractValidator<ProductCategory>
    {
        public ProductCategoryValidator()
        {
            RuleFor(x => x.name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.slug).NotEmpty().WithMessage("Slug is required.")
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be URL-friendly (lowercase letters, numbers, and hyphens only).");
            RuleFor(x => x.description).MaximumLength(190).WithMessage("Description cannot exceed 190 characters.");
        }
    }
}
