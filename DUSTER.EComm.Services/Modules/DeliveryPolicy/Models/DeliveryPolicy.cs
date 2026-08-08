namespace DUSTER.EComm.Services.Modules.DeliveryPolicy.Models
{
    [Table("tbl_delivery_policies")]
    public class DeliveryPolicys : BaseEntity
    {
        [Key]
        public long delivery_policy_id { get; set; }
        public string? policy_name { get; set; }
        public DateTime effective_from { get; set; }
        public DateTime? effective_to { get; set; }
        // Calculation type can be FlatBracket,ProgressiveSlab
        public string? calculation_type { get; set; }
        public decimal min_charge { get; set; }
        public decimal max_charge { get; set; }
        public decimal tax_percentage { get; set; }
        public bool is_active { get; set; } = true;

        [Computed]
        public List<DeliveryPolicySlab> slabs { get; set; } = new List<DeliveryPolicySlab>();
    }

    public class DeliveryPolicyValidator : AbstractValidator<DeliveryPolicys>
    {
        public DeliveryPolicyValidator()
        {
            RuleFor(x => x.policy_name).NotEmpty().WithMessage("Policy Name is required.");
            RuleFor(x => x.calculation_type).NotEmpty().WithMessage("Calculation Type is required.");
            RuleFor(x => x.min_charge).GreaterThanOrEqualTo(0).WithMessage("Minimum Charge cannot be negative.");
            RuleFor(x => x.max_charge).GreaterThanOrEqualTo(x => x.min_charge).WithMessage("Maximum Charge must be greater than or equal to Minimum Charge.");
            RuleFor(x => x.tax_percentage).GreaterThanOrEqualTo(0).WithMessage("Tax Percentage cannot be negative.");
        }
    }
}
