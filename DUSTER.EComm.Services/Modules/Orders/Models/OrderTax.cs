using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Orders.Models
{
    [Table("tbl_order_tax")]
    public class OrderTax : BaseEntity
    {
        [Key]
        public long order_tax_id { get; set; }

        public long order_id { get; set; }

        public string? order_no { get; set; }

        public long order_txn_id { get; set; }

        public long product_id { get; set; }

        public int tax_id { get; set; }

        public decimal tax_amount { get; set; }

        public decimal total_amount { get; set; }

        public string? component_name { get; set; }

        public decimal rate { get; set; }

        public string? calculation_type { get; set; }

        public decimal calculated_amount { get; set; }
    }

    public class OrderTaxValidatior : AbstractValidator<OrderTax>
    {
        public OrderTaxValidatior()
        {
            RuleFor(x => x.order_id).GreaterThan(0).WithMessage("Order ID must be greater than 0.");
            RuleFor(x => x.product_id).GreaterThan(0).WithMessage("Product code must be greater than 0.");
            RuleFor(x => x.tax_id).GreaterThanOrEqualTo(0).WithMessage("Tax ID must be greater than or equal to 0.");
            RuleFor(x => x.tax_amount).GreaterThanOrEqualTo(0).WithMessage("Tax amount must be greater than or equal to 0.");
            RuleFor(x => x.total_amount).GreaterThanOrEqualTo(0).WithMessage("Total amount must be greater than or equal to 0.");
        }
    }
}
