using DUSTER.EComm.Data.Helpers.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Catelog.Models
{
    [Table("tbl_products")]
    public class ProductMaster : BaseEntity
    {
        [Key]
        public long product_code { get; set; }
        public string? name { get; set; }
        public string? sku { get; set; }
        public string? slug { get; set; }
        public string? description { get; set; }
        public decimal base_price { get; set; }
        public int category_id { get; set; }
        public int sub_category_id { get; set; }
        public int manufacturer_id { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? attributes { get; set; }
        [Computed]
        public Dictionary<string, object>? attribute { get; set; } = new();
        public bool is_active { get; set; }
        public bool in_stock { get; set; }
        public int stock_quantity { get; set; }
        public bool is_top_selling { get; set; }
        public bool is_new_arrival { get; set; }
        public int tax_class_id { get; set; }
        public int estimated_delivery_days { get; set; }

        public decimal actual_price { get; set; }

        public string? sap_sku_code { get; set; }

    }

    public class ProductMasterValidator : AbstractValidator<ProductMaster>
    {
        public ProductMasterValidator()
        {
            RuleFor(x => x.name).NotEmpty().WithMessage("Product name is required.");
            RuleFor(x => x.sku).NotEmpty().WithMessage("SKU is required.");
            RuleFor(x => x.base_price).GreaterThan(0).WithMessage("Base price must be greater than zero.");
            RuleFor(x => x.actual_price).GreaterThanOrEqualTo(x => x.base_price).WithMessage("Actual price should be greater than or equal to Base price");
            RuleFor(x => x.category_id).GreaterThan(0).WithMessage("Category must be greater than zero.");
            RuleFor(x => x.sub_category_id).GreaterThan(0).WithMessage("Sub Category must be greater than zero.");
            RuleFor(x => x.manufacturer_id).GreaterThan(0).WithMessage("Manufacturer must be greater than zero.");
            RuleFor(x => x.stock_quantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
            RuleFor(x => x.slug).NotEmpty().WithMessage("Slug is required.").Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be URL-friendly (lowercase letters, numbers, and hyphens only).");
            RuleFor(x => x.tax_class_id).GreaterThan(0).WithMessage("Tax Class must be selected.");
            RuleFor(x => x.estimated_delivery_days).GreaterThanOrEqualTo(0).WithMessage("Estimated Delivery Days must be greater than zero.");
        }
    }

    public class ProductMasterDto
    {
        public long product_code { get; set; }
        public string? name { get; set; }
        public string? sku { get; set; }
        public string? slug { get; set; }
        public string? description { get; set; }
        public decimal base_price { get; set; }
        public int category_id { get; set; }
        public string? category_name { get; set; }
        public int sub_category_id { get; set; }
        public string? sub_category_name { get; set; }
        public int manufacturer_id { get; set; }
        public string? manufacturer_name { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? attributes { get; set; }
        [Computed]
        public Dictionary<string, object>? attribute { get; set; } = new();

        public bool is_active { get; set; }
        public bool in_stock { get; set; }
        public int stock_quantity { get; set; }
        public dynamic? product_reviews { get; set; }
        public dynamic? product_images { get; set; }

        public bool is_top_selling { get; set; }
        public bool is_new_arrival { get; set; }
        public int total_review { get; set; }
        public string? rating { get; set; }
        public long product_wishlist_id { get; set; }
        public long user_code { get; set; }
        public int tax_class_id { get; set; }
        public string? tax_class_name { get; set; }
        public int estimated_delivery_days { get; set; }
        public decimal actual_price { get; set; }
        public string? sap_sku_code { get; set; }

    }

    public class ProductFilterDto : PaginationParams
    {
        public string? name { get; set; }
        public string? sku { get; set; }
        public int? category_id { get; set; }
        public int? sub_category_id { get; set; }
        public int? manufacturer_id { get; set; }
        public bool? is_active { get; set; }
        public bool? in_stock { get; set; }
    }

}