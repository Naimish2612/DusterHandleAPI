namespace DUSTER.EComm.Services.Modules.Catelog.Models
{
    [Table("tbl_product_wishlist")]
    public class ProductWishlist : BaseEntity
    {
        public ProductWishlist()
        {

        }

        [Key]
        public long product_wishlist_id { get; set; }
        public long user_id { get; set; }
        public long product_code { get; set; }

    }

    public class ProductWishlistValidator : AbstractValidator<ProductWishlist>
    {
        public ProductWishlistValidator()
        {
            RuleFor(x => x.product_code).NotEmpty().WithMessage("Product Required.");
        }
    }

    public class ProductWishlistDTO
    {
        public long product_wishlist_id { get; set; }
        public long user_id { get; set; }
        public string? user_name { get; set; }
        public long product_code { get; set; }
        public string? product_name { get; set; }

    }
}
