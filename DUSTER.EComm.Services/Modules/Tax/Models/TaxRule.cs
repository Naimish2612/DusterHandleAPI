

using DUSTER.EComm.Data.Helpers.Pagination;

namespace DUSTER.EComm.Services.Modules.Tax.Models
{
    [Table("tbl_tax_rules")]
    public class TaxRule : BaseEntity
    {
        [Key]
        public int rule_id { get; set; }
        public int class_id { get; set; }
        public int component_id { get; set; }
        // Transaction type can be "Intra-State" or "Inter-State"
        public string? transaction_type { get; set; }
        public decimal rate { get; set; }
        // Calculation type can be "Percentage" or "FlatAmount"
        public string? calculation_type { get; set; } = "Percentage";
        public DateTime valid_from { get; set; }
        public DateTime? valid_to { get; set; }
        public bool is_active { get; set; } = true;

        [Computed]
        public string? class_name { get; set; }
        [Computed]
        public string? component_name { get; set; }
    }

    public class TaxRuleValidator : AbstractValidator<TaxRule>
    {
        public TaxRuleValidator()
        {
            RuleFor(x => x.class_id).GreaterThan(0).WithMessage("Tax Class is required.");
            RuleFor(x => x.component_id).GreaterThan(0).WithMessage("Tax Component is required.");
            RuleFor(x => x.transaction_type).NotEmpty().Must(x => new[] { "Intra-State", "Inter-State" }.Contains(x)).WithMessage("Invalid transaction type.");
            RuleFor(x => x.calculation_type).NotEmpty().Must(x => new[] { "Percentage", "FlatAmount" }.Contains(x))
                .WithMessage("Calculation type must be 'Percentage' or 'FlatAmount'.");
            RuleFor(x => x.rate).GreaterThanOrEqualTo(0).WithMessage("Rate cannot be negative.");
            RuleFor(x => x.valid_from).NotEmpty().WithMessage("Start date is required.");
        }
    }

    public class TaxRuleFilterDto
    {
        public int? class_id { get; set; }
        public int? component_id { get; set; }
        public string? transaction_type { get; set; }
        public bool? is_active { get; set; }
    }

    public class TaxPreviewRequestDto
    {
        public decimal entered_price { get; set; }
        public int tax_class_id { get; set; }
        public string? transaction_type { get; set; } // "Intra-State" or "Inter-State"
        public bool is_inclusive { get; set; } // true if the price includes GST, false if it's the base price
    }

    public class TaxPreviewResponseDto
    {
        public decimal entered_price { get; set; }
        public decimal net_base_price { get; set; }
        public decimal total_tax_amount { get; set; }
        public decimal final_customer_price { get; set; }

        // This will hold the dynamic breakdown (e.g., CGST: 9%, SGST: 9%, Cess: ₹50)
        public List<TaxComponentBreakdown> tax_breakdown { get; set; } = new List<TaxComponentBreakdown>();
    }

    public class TaxComponentBreakdown
    {
        public string? component_name { get; set; }
        public decimal rate { get; set; }
        public string? calculation_type { get; set; } // "Percentage" or "FlatAmount"
        public decimal calculated_amount { get; set; }
    }

}
