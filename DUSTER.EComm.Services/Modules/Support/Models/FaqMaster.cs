using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Support.Models
{
    [Table("tbl_faq_master")]
    public class FaqMaster : BaseEntity
    {
        [Key]
        public int faq_id { get; set; }
        public string? question { get; set; }
        public string? answer { get; set; }
        public bool is_active { get; set; } = true;
        [Computed]
        public string? associated_field { get; set; }
    }

    [Table("tbl_product_faq_mapping")]
    public class ProductFaqMapping : BaseEntity
    {
        [Key]
        public int mapping_id { get; set; }
        public int faq_id { get; set;    }

        // Allowed values: 'Product', 'SubCategory', 'Category'
        public string? target_type { get; set; }
        public long target_id { get; set; }
    }

    public class FaqMasterValidator : AbstractValidator<FaqMaster>
    {
        public FaqMasterValidator()
        {
            RuleFor(x => x.question).NotEmpty().WithMessage("FAQ question cannot be blank.");
            RuleFor(x => x.answer).NotEmpty().WithMessage("FAQ answer cannot be blank.");
        }
    }

    public class ProductFaqMappingValidator : AbstractValidator<ProductFaqMapping>
    {
        public ProductFaqMappingValidator()
        {
            RuleFor(x => x.faq_id).GreaterThan(0).WithMessage("A valid FAQ ID is required for mapping.");
            RuleFor(x => x.target_id).GreaterThan(0).WithMessage("A valid Target ID is required.");
            RuleFor(x => x.target_type)
                .NotEmpty()
                .Must(x => new[] { "Product", "SubCategory", "Category" }.Contains(x))
                .WithMessage("Target type must be exactly 'Product', 'SubCategory', or 'Category'.");
        }
    }

}
