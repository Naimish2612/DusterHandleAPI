using Newtonsoft.Json;

namespace DUSTER.EComm.Services.Modules.DeliveryPolicy.Models
{
    [Table("tbl_order_delivery_calculations")]
    public class OrderDeliveryCalculation : BaseEntity
    {
        [Key]
        public long calculation_id { get; set; }
        public long order_id { get; set; }
        public string? order_no { get; set; }
        public long delivery_policy_id { get; set; }
        public decimal delivery_base_fee { get; set; }
        public decimal delivery_tax_amount { get; set; }
        //IGST or CGST/SGST
        public string? delivery_tax_type { get; set; }
        public decimal delivery_discount_amount { get; set; }
        public decimal delivery_final_total { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "jsonb")]
        public string? calculation_snapshot { get; set; }

        [Computed]
        [JsonIgnore]
        public object? calculation_snapshot_obj { get; set; }
    }

    public class OrderDeliveryCalculationValidator : AbstractValidator<OrderDeliveryCalculation>
    {
        public OrderDeliveryCalculationValidator()
        {
            RuleFor(x => x.order_id).GreaterThan(0).WithMessage("Valid Order ID is required.");
            RuleFor(x => x.order_no).NotEmpty().WithMessage("Order No is required.");
            RuleFor(x => x.delivery_policy_id).GreaterThan(0).WithMessage("Valid Policy ID is required.");
            RuleFor(x => x.calculation_snapshot_obj).NotNull().WithMessage("Calculation Snapshot Object is required.");
        }
    }


    public class DeliveryFeeRequest
    {
        public decimal post_discount_cart_total { get; set; }
        public string? destination_state { get; set; }
        public decimal delivery_discount_amount { get; set; }
    }

    public class DeliveryFeeRequestValidator : AbstractValidator<DeliveryFeeRequest>
    {
        public DeliveryFeeRequestValidator()
        {
            RuleFor(x => x.post_discount_cart_total).GreaterThanOrEqualTo(0).WithMessage("Cart Total cannot be negative.");
            RuleFor(x => x.destination_state).NotEmpty().WithMessage("Destination State is required.");
        }
    }
}
