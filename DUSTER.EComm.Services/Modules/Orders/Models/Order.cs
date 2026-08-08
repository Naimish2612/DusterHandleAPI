using DUSTER.EComm.Data.Helpers.Pagination;
using DUSTER.EComm.Services.Modules.Auth.Models;

namespace DUSTER.EComm.Services.Modules.Orders.Models
{
    [Table("tbl_order")]
    public class Order : BaseEntity
    {
        [Key]
        public long order_id { get; set; }
        public string? order_no { get; set; }
        public long user_code { get; set; }
        public DateTime order_date { get; set; }
        public decimal sub_total { get; set; }
        public string? order_status { get; set; }
        public decimal shipping_amount { get; set; }

        public decimal marketplace_fee { get; set; }
        public decimal net_amount { get; set; }
        public string? payment_method { get; set; }
        public string? payment_status { get; set; }
        public string? cart_details { get; set; }
        public long shipping_address_id { get; set; }
        public long billing_address_id { get; set; }
        public string? invoice_file_path { get; set; }
        public decimal? discount_amount { get; set; }
        public decimal? grand_total { get; set; }
        public string? shipment_ack_number { get; set; }
        public string? shipment_tracking_url { get; set; }
        public string? invoice_number { get; set; }

        [Computed]
        public string? user_name { get; set; }
        [Computed]
        public string? coupon_code { get; set; }
        [Computed]
        public string? coupon_category_name { get; set; }
    }

    public class OrderValidatior : AbstractValidator<Order>
    {
        public OrderValidatior()
        {
            RuleFor(x => x.user_code).GreaterThan(0).WithMessage("User code must be greater than 0.");
            RuleFor(x => x.order_no).NotEmpty().WithMessage("Order number is required.");
            RuleFor(x => x.order_date).LessThanOrEqualTo(DateTime.Now).WithMessage("Order date cannot be in the future.");
            RuleFor(x => x.sub_total).GreaterThanOrEqualTo(0).WithMessage("Sub total must be greater than or equal to 0.");
            RuleFor(x => x.shipping_amount).GreaterThanOrEqualTo(0).WithMessage("Shipping amount must be greater than or equal to 0.");
            RuleFor(x => x.marketplace_fee).GreaterThanOrEqualTo(0).WithMessage("Marketplace fee must be greater than or equal to 0.");
            RuleFor(x => x.net_amount).GreaterThanOrEqualTo(0).WithMessage("Net amount must be greater than or equal to 0.");
            RuleFor(x => x.shipping_address_id).GreaterThan(0).WithMessage("Shipping address ID must be greater than 0.");
            RuleFor(x => x.billing_address_id).GreaterThan(0).WithMessage("Billing address ID must be greater than 0.");
        }
    }

    public class OrderDto
    {
        public long order_id { get; set; }
        public string? order_no { get; set; }
        public long user_code { get; set; }
        public DateTime order_date { get; set; }
        public decimal sub_total { get; set; }
        public string? order_status { get; set; }
        public decimal shipping_amount { get; set; }
        public decimal marketplace_fee { get; set; }
        public decimal net_amount { get; set; }
        public string? payment_method { get; set; }
        public string? payment_status { get; set; }
        public string? cart_details { get; set; }
        public long shipping_address_id { get; set; }
        public long billing_address_id { get; set; }
        public List<OrderTxnDto> orderItems { get; set; } = new List<OrderTxnDto>();
        public string? cart_id { get; set; }
        [Computed]
        public object? userInfo { get; set; }
        [Computed]
        public object? delivery_charges { get; set; }
        [Computed]
        public object? tax_breakout { get; set; }
        public string? invoice_file_path { get; set; }
        public string? invoice_number { get; set; }
        public decimal? discount_amount { get; set; }
        public decimal? grand_total { get; set; }
        public string? shipment_ack_number { get; set; }
        public string? shipment_tracking_url { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Returned
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
    public class OrderFilter : PaginationParams
    {
        public string? order_no { get; set; }
        public DateTime? from_date { get; set; }
        public DateTime? to_date { get; set; }
        public string? order_status { get; set; }
        public string? payment_status { get; set; }

    }


}
