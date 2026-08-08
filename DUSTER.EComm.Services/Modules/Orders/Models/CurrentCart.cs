using DUSTER.EComm.Services.Modules.Masters.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Orders.Models
{
    public class CurrentCart
    {
        public string? cart_id { get; set; }
        public long user_code { get; set; }
        public decimal sub_total { get; set; }
        public string? order_status { get; set; }
        public decimal shipping_amount { get; set; }
        public decimal marketplace_fee { get; set; }
        public decimal net_amount { get; set; }
        public string? payment_method { get; set; }
        public string? payment_status { get; set; }
        public long shipping_address_id { get; set; }
        public long billing_address_id { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        
        // ✅ Coupon Fields
        public List<AppliedCouponInfo> Coupons { get; set; } = new List<AppliedCouponInfo>();
        public decimal coupon_discount { get; set; } = 0;       // Total coupon discount
        public decimal grand_total { get; set; } = 0;           // net_amount - coupon_discount
    }

    public class CartItem
    {
        public long product_code { get; set; }
        public string? product_name { get; set; }
        public string? sku { get; set; }
        public int qty { get; set; }
        public decimal unit_price { get; set; }
        public decimal net_amount { get; set; }
        public int tax_id { get; set; }
        public decimal tax_amount { get; set; }
        public decimal discount_amount { get; set; }
        public decimal total_amount { get; set; }

        public List<CartItemTaxBreakdown> tax_breakdown { get; set; } = new();
    }

    public class CartItemRequest
    {
        public string? cart_id { get; set; } = "NONE";
        public long product_code { get; set; }
        public int qty { get; set; }
        public string? action_event { get; set; }

        // ✅ Coupon action fields
        public string? coupon_action { get; set; }  // "Apply" or "Remove"
        public string? coupon_code { get; set; }    // Coupon code to apply/remove

        public int tax_id { get; set; }
        public decimal tax_amount { get; set; }
        public List<CouponPayload>? applied_coupons { get; set; }
        public List<CartItemTaxBreakdown> tax_breakdown { get; set; } = new();

        public long shipping_address_id { get; set; } = 0;

    }

    public class CouponPayload
    {
        public long coupon_id { get; set; }
        public string coupon_code { get; set; }
        public decimal discount_amount { get; set; }
    }

    public class AppliedCouponInfo
    {
        public int coupon_id { get; set; }
        public string coupon_code { get; set; } = string.Empty;
        public string discount_type { get; set; } = "Value";     
        public decimal discount_value { get; set; }              
        public decimal discount_applied { get; set; }            
        public decimal minimum_order_amount { get; set; }
        public bool can_be_clubbed { get; set; }
    }

    public class CartItemTaxBreakdown
    {
        public string? component_name { get; set; }
        public decimal rate { get; set; }
        public string? calculation_type { get; set; }
        public decimal calculated_amount { get; set; }
    }
}