using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.DeliveryPolicy.Models
{
    [Table("tbl_delivery_policy_slabs")]
    public class DeliveryPolicySlab : BaseEntity
    {
        [Key]
        public long slab_id { get; set; }
        public long delivery_policy_id { get; set; }
        public decimal range_start { get; set; }
        public decimal range_end { get; set; }
        public decimal percentage { get; set; }
    }

    public class DeliveryPolicySlabValidator : AbstractValidator<DeliveryPolicySlab>
    {
        public DeliveryPolicySlabValidator()
        {
            RuleFor(x => x.delivery_policy_id).GreaterThan(0).WithMessage("Valid Policy ID is required.");
            RuleFor(x => x.range_end).GreaterThan(x => x.range_start).WithMessage("Range End must be greater than Range Start.");
            RuleFor(x => x.percentage).GreaterThanOrEqualTo(0).WithMessage("Slab Percentage cannot be negative.");
        }
    }
}
