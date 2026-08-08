using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Orders.Models
{
    [Table("tbl_order_txn")]
    public class OrderTxn : BaseEntity
    {
        [Key]
        public long order_txn_id { get; set; }
        public long order_id { get; set; }
        public long product_code { get; set; }
        public int qty { get; set; }
        public decimal unit_price { get; set; }
        public decimal net_amount { get; set; }
        public int tax_id { get; set; }
        public decimal tax_amount { get; set; }
        public decimal discount_amount { get; set; }
        public decimal total_amount { get; set; }
    }

    public class OrderTxnValidatior : AbstractValidator<OrderTxn>
    {
        public OrderTxnValidatior()
        {
            RuleFor(x => x.order_id).GreaterThan(0).WithMessage("Order ID must be greater than 0.");
            RuleFor(x => x.product_code).GreaterThan(0).WithMessage("Product code must be greater than 0.");
            RuleFor(x => x.qty).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            RuleFor(x => x.unit_price).GreaterThanOrEqualTo(0).WithMessage("Unit price must be greater than or equal to 0.");
            RuleFor(x => x.net_amount).GreaterThanOrEqualTo(0).WithMessage("Net amount must be greater than or equal to 0.");
            RuleFor(x => x.tax_id).GreaterThanOrEqualTo(0).WithMessage("Tax ID must be greater than or equal to 0.");
            RuleFor(x => x.tax_amount).GreaterThanOrEqualTo(0).WithMessage("Tax amount must be greater than or equal to 0.");
            RuleFor(x => x.total_amount).GreaterThanOrEqualTo(0).WithMessage("Total amount must be greater than or equal to 0.");
            RuleFor(x => x.discount_amount).GreaterThanOrEqualTo(0).WithMessage("Discount amount must be greater than or equal to 0.");
        }
    }

    public class OrderTxnDto
    {
        public long order_txn_id { get; set; }
        public long order_id { get; set; }
        public long product_code { get; set; }
        public string? product_name { get; set; }
        public string? sku { get; set; }
        public string? description { get; set; }
        public int qty { get; set; }
        public decimal unit_price { get; set; }
        public decimal net_amount { get; set; }
        public int tax_id { get; set; }
        public decimal tax_amount { get; set; }
        public decimal discount_amount { get; set; }
        public decimal total_amount { get; set; }
        [Computed]
        public string? image_url { get; set; }
        [Computed]
        public bool is_reviewed { get; set; }
    }
}
