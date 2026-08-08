using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Catelog.Models
{
    [Table("tbl_product_reviews")]
    public class ProductReviews : BaseEntity
    {
        [Key]
        public long product_review_id { get; set; }
        public long product_code { get; set; }
        public long user_id { get; set; }
        public string? rating { get; set; }
        public string? review_title { get; set; }
        public string? comment { get; set; }
        public bool is_verified_purchase { get; set; }
        public bool is_publish { get; set; }
        public bool is_delete { get; set; }
    }

    public class ProductReviewsDto
    {
        public long product_review_id { get; set; }
        public long product_code { get; set; }
        public long user_id { get; set; }
        public string? user_name { get; set; }
        public string? rating { get; set; }
        public string? review_title { get; set; }
        public string? comment { get; set; }
        public bool is_verified_purchase { get; set; }
        public bool is_publish { get; set; }
        public DateTime review_datetime { get; set; }
    }

    public class ProductReviewsValidator : AbstractValidator<ProductReviews>
    {
        public ProductReviewsValidator()
        {
            RuleFor(x => x.product_code).GreaterThan(0).WithMessage("Product ID must be greater than 0.");
            RuleFor(x => x.user_id).GreaterThan(0).WithMessage("User ID must be greater than 0.");
            RuleFor(x => x.rating).NotEmpty().WithMessage("Rating must be between 1 and 5.");
            RuleFor(x => x.review_title).MaximumLength(100).WithMessage("Review title cannot exceed 100 characters.");
            RuleFor(x => x.comment).MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
        }
    }
}
