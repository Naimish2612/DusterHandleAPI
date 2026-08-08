using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_product_sub_category")]
    public class ProductSubCategory : BaseEntity
    {
        [Computed]
        public string? category_name { get; set; }
        [Key]
        public int sub_category_id { get; set; }
        public int category_id { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
    }

    public class ProductSubCategoryValidator : AbstractValidator<ProductSubCategory>
    {
        public ProductSubCategoryValidator()
        {
            RuleFor(x => x.name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.slug).NotEmpty().WithMessage("Slug is required.")
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be URL-friendly (lowercase letters, numbers, and hyphens only).");
        }
    }
}
