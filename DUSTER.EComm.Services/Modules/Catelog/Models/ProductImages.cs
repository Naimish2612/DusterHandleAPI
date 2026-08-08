namespace DUSTER.EComm.Services.Modules.Catelog.Models
{
    [Table("tbl_product_images")]
    public class ProductImages : BaseEntity
    {
        [Key]
        public int product_image_id { get; set; }
        public long product_code { get; set; }
        public string? image_url { get; set; }
        public string? alt_text { get; set; }
        public int display_order { get; set; }
        public bool is_primary { get; set; }
        public string image_public_id { get; set; }
    }

    public class ProductImagesValidator : AbstractValidator<ProductImages>
    {
        public ProductImagesValidator()
        {
            RuleFor(x => x.product_code).GreaterThan(0).WithMessage("Product is require");
            RuleFor(x => x.alt_text).NotEmpty().MaximumLength(490);
            RuleFor(x => x.display_order).GreaterThanOrEqualTo(0);
        }
    }
    
    public class ProductImagesDto
    {
        public int product_image_id { get; set; }
        public long product_code { get; set; }
        public string? image_url { get; set; }
        public string? alt_text { get; set; }
        public int display_order { get; set; }
        public bool is_primary { get; set; }

        public long[] image_ids { get; set; }
    }
}
