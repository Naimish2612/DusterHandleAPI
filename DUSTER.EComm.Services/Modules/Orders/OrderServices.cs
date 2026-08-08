using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Data.Helpers.Cloudinary;
using DUSTER.EComm.Data.Helpers.Strings;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.AlertEngine;
using DUSTER.EComm.Services.Modules.Auth.Models;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using DUSTER.EComm.Services.Modules.Customers.Models;
using DUSTER.EComm.Services.Modules.Masters;
using DUSTER.EComm.Services.Modules.Masters.Models;
using DUSTER.EComm.Services.Modules.Orders.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.Orders
{
    public class OrderServices : IOrderServices
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IEIPLRepository<Order> _orderRepository;
        private readonly IEIPLRepository<OrderTxn> _orderTxnRepository;
        private readonly IPostgresDistributedCache _cache;
        private readonly IEIPLRepository<ProductMaster> _productRepository;
        private readonly IEIPLRepository<UsersModel> _userRepo;
        private readonly IEIPLRepository<ProductImages> _productImageRepository;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private readonly IEIPLRepository<CouponMaster> _couponRepository;
        private readonly ICouponServices _couponServices;
        private readonly IEIPLRepository<OrderTax> _orderTaxRepository;
        private readonly IAlertEngineService _alertEngineService;
        private readonly IEIPLRepository<AddressBook> _addressBookRepo;
        public OrderServices(
            ICurrentUserService currentUser,
            IEIPLRepository<Order> orderRepository,
            IEIPLRepository<OrderTxn> orderTxnRepository,
            IPostgresDistributedCache cache,
            IEIPLRepository<ProductMaster> productRepository,
            IEIPLRepository<ProductImages> productImageRepository,
            IEIPLRepository<UsersModel> userRepo,
            IEIPLRepository<ProductReviews> productReviewRepository,
            IOptions<CloudinarySettings> cloudinarySettings,
            IEIPLRepository<CouponMaster> couponRepository,
            ICouponServices couponServices,
            IEIPLRepository<OrderTax> orderTaxRepository,
            IAlertEngineService alertEngineService,
            IEIPLRepository<AddressBook> addressBookRepo)
        {
            _currentUser = currentUser;
            _orderRepository = orderRepository;
            _orderTxnRepository = orderTxnRepository;
            _cache = cache;
            _productRepository = productRepository;
            _productImageRepository = productImageRepository;
            _userRepo = userRepo;
            _cloudinarySettings = cloudinarySettings;
            Account account = new Account(cloudinarySettings.Value.CloudName, cloudinarySettings.Value.ApiKey, cloudinarySettings.Value.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _couponRepository = couponRepository;
            _couponServices = couponServices;
            _orderTaxRepository = orderTaxRepository;
            _alertEngineService = alertEngineService;
            _addressBookRepo = addressBookRepo;
        }

        public async Task<IActionResult> PlaceOrder(OrderDto order)
        {
            try
            {
                decimal tax = 0;
                if (order == null)
                    return ResponseEntity<object>.Error(null, "Invalid order data");

                if (string.IsNullOrEmpty(order.cart_id))
                    return ResponseEntity<object>.Error(null, "Cart ID is required");

                bool isCartExists = await _cache.HasKey(order.cart_id);

                if (isCartExists)
                {
                    CurrentCart currentCart = await _cache.GetObjectAsync<CurrentCart>(order.cart_id);

                    if (currentCart.Items == null || !currentCart.Items.Any())
                        return ResponseEntity<object>.Error(null, "Cart is empty");

                    // Calculate net amount after discount
                    decimal netAmountAfterDiscount = currentCart.grand_total + order.shipping_amount;
                    currentCart.shipping_amount = order.shipping_amount;

                    Order newOrder = new Order
                    {
                        user_code = _currentUser.User.user_code,
                        order_date = DateTime.Now,
                        sub_total = currentCart.sub_total,
                        order_status = OrderStatus.Processing.ToString(),
                        shipping_amount = order.shipping_amount,
                        marketplace_fee = currentCart.marketplace_fee,
                        net_amount = netAmountAfterDiscount,
                        discount_amount = currentCart.coupon_discount,
                        payment_method = order.payment_method,
                        payment_status = PaymentStatus.Pending.ToString(),
                        shipping_address_id = order.shipping_address_id,
                        billing_address_id = order.billing_address_id,
                        order_no = $"{DateTime.Now.ToString("ssmmhhyyMMdd")}-{StringHelper.GetUniqueStringV2(10)}",
                        cart_details = System.Text.Json.JsonSerializer.Serialize(currentCart),
                        grand_total = currentCart.grand_total + order.shipping_amount
                    };

                    var orderResponse = await _orderRepository.InsertAsync(newOrder);

                    if (orderResponse == null)
                        return ResponseEntity<object>.Error(null, "Failed to place order, Please Contact your administrator");

                    long orderId = Convert.ToInt64(orderResponse);

                    foreach (var item in currentCart.Items)
                    {
                        OrderTxn orderItem = new OrderTxn
                        {
                            order_id = orderId,
                            product_code = item.product_code,
                            unit_price = item.unit_price,
                            qty = item.qty,
                            net_amount = item.net_amount,
                            tax_id = item.tax_id,
                            tax_amount = item.tax_amount,
                            discount_amount = item.discount_amount,
                            total_amount = item.total_amount
                        };

                        //await _orderTxnRepository.InsertAsync(orderItem);
                        var txnResponse = await _orderTxnRepository.InsertAsync(orderItem);
                        long orderTxnId = Convert.ToInt64(txnResponse);


                        // ✅ NEW: Insert tax breakdown for this item
                        if (item.tax_breakdown != null && item.tax_breakdown.Any())
                        {
                            foreach (var taxComp in item.tax_breakdown)
                            {
                                OrderTax orderTax = new OrderTax
                                {
                                    order_id = orderId,
                                    order_no = newOrder.order_no,
                                    order_txn_id = orderTxnId,
                                    product_id = item.product_code,
                                    tax_id = item.tax_id,
                                    tax_amount = item.tax_amount,
                                    total_amount = item.total_amount,
                                    component_name = taxComp.component_name,
                                    rate = taxComp.rate,
                                    calculation_type = taxComp.calculation_type,
                                    calculated_amount = taxComp.calculated_amount,
                                };

                                tax = item.tax_amount;
                                await _orderTaxRepository.InsertAsync(orderTax);
                            }
                        }
                    }

                    // ✅ Insert Coupon Usages to DB via ICouponServices
                    if (currentCart.Coupons != null && currentCart.Coupons.Any())
                    {
                        foreach (var coupon in currentCart.Coupons)
                        {
                            CouponUsage usage = new CouponUsage
                            {
                                coupon_id = coupon.coupon_id,
                                user_code = currentCart.user_code,
                                order_id = orderId,
                                discount_applied = coupon.discount_applied,
                                used_at = DateTime.Now
                            };

                            await _couponServices.AddCouponUsage(usage);
                        }
                    }

                    await _cache.RemoveAsync(order.cart_id);
                    await _cache.RemoveAsync($"cart_{_currentUser.User.user_code}");

                    // Prepare email payload with items containing product name, qty, price, and image url
                    var emailItems = new List<object>();
                    foreach (var item in currentCart.Items)
                    {
                        string imageUrl = "";
                        try
                        {
                            var image = await _productImageRepository.GetSingleOrDefaultAsync(
                                "select image_url from tbl_product_images where product_code = @product_code and is_primary = true",
                                new DynamicParameters(new { product_code = item.product_code })
                            );
                            imageUrl = image?.image_url ?? "";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[OrderServices] Error fetching image for product {item.product_code}: {ex.Message}");
                        }

                        emailItems.Add(new
                        {
                            product_name = item.product_name,
                            qty = item.qty,
                            unit_price = item.unit_price,
                            total_amount = item.total_amount,
                            image_url = imageUrl
                        });
                    }

                    var param = new DynamicParameters();
                    param.Add("user_code", currentCart.user_code);

                    var data = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where user_code=@user_code and is_active = true and is_default_shipping = true", param);

                    var emailPayload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "order_no", newOrder.order_no },
                        { "order_date", newOrder.order_date.ToString("dd MMM yyyy hh:mm tt") },
                        { "user_name", _currentUser.User.user_name },
                        { "sub_total", newOrder.sub_total },
                        { "shipping_amount", newOrder.shipping_amount },
                        { "discount_amount", newOrder.discount_amount ?? 0 },
                        { "grand_total", newOrder.grand_total ?? 0 },
                        { "payment_method", newOrder.payment_method },
                        { "payment_status", newOrder.payment_status },
                        { "address_line1", data.address_line1 },
                        { "address_line2", data.address_line2 },
                        { "city", data.city },
                        { "state", data.state },
                        { "pin_code", data.pin_code },
                        { "tax_amount", tax },
                        { "items", emailItems }
                    };

                    await _alertEngineService.QueueEventNotificationAsync("ORDER_PLACED", _currentUser.User.email, _currentUser.User.user_name, emailPayload);
                    return ResponseEntity<object>.Success(new
                    {
                        order_id = orderId,
                        order_no = newOrder.order_no,
                        order_status = newOrder.order_status,
                        payment_status = newOrder.payment_status,
                        sub_total = currentCart.sub_total,
                        coupon_discount = currentCart.coupon_discount,
                        grand_total = currentCart.grand_total
                    }, "Order placed successfully");
                }
                else
                    return ResponseEntity<object>.Error(null, "Cart not found, Please Contact your administrator");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> AddToCart(CartItemRequest cart)
        {
            try
            {
                // ── Validate: skip product check for coupon-only actions ──
                bool isCouponOnlyAction = cart.action_event == "ApplyCoupon" || (!string.IsNullOrEmpty(cart.coupon_action) && cart.product_code <= 0);

                if (!isCouponOnlyAction)
                {
                    if (cart.product_code <= 0)
                        return ResponseEntity<object>.Error(null, "Invalid product code");

                    if (cart.qty <= 0 && cart.action_event != "Delete")
                        return ResponseEntity<object>.Error(null, "Quantity must be greater than zero");
                }

                // ── 2. GET OR CREATE CART (from Method 1) ──────────────────
                CurrentCart currentCart;
                bool isCartExists = !string.IsNullOrEmpty(cart.cart_id) && await _cache.HasKey(cart.cart_id);

                if (isCartExists)
                {
                    currentCart = await _cache.GetObjectAsync<CurrentCart>(cart.cart_id);
                }
                else
                {
                    // Coupon-only on a non-existent cart = error
                    if (isCouponOnlyAction)
                        return ResponseEntity<object>.Error(null, "Cart not found");

                    // Create new cart
                    string cartId = $"{StringHelper.GetUniqueStringV2(15)}_{_currentUser.User.user_code}";
                    currentCart = new CurrentCart
                    {
                        cart_id = cartId,
                        user_code = _currentUser.User.user_code,
                        order_status = OrderStatus.Pending.ToString(),
                        payment_status = PaymentStatus.Pending.ToString(),
                        billing_address_id = 0,
                        shipping_address_id = 0,
                        marketplace_fee = 0,
                        shipping_amount = 0,
                        Items = new List<CartItem>(),
                        Coupons = new List<AppliedCouponInfo>()
                    };
                    cart.cart_id = cartId;
                }

                // ── 3. COUPON HANDLING – NEW STYLE (from Method 2) ─────────
                if (cart.action_event == "ApplyCoupon")
                {
                    // Clear first – sending empty array = remove all
                    currentCart.Coupons.Clear();

                    if (cart.applied_coupons != null && cart.applied_coupons.Any())
                    {
                        foreach (var c in cart.applied_coupons)
                        {
                            var parameters = new DynamicParameters();
                            parameters.Add("couponCode", c.coupon_code);

                            var sql = @"SELECT cm.*, cc.category_name, cc.can_be_clubbed 
                                FROM tbl_coupon_masters cm 
                                LEFT JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id 
                                WHERE cm.coupon_code = @couponCode AND cm.is_deleted = false";

                            CouponMaster coupon = await _couponRepository.GetSingleOrDefaultAsync(sql, parameters);
                            if (coupon == null) continue;

                            decimal discountApplied = 0;
                            if (coupon.discount_type == "Percentage")
                            {
                                discountApplied = currentCart.sub_total * (coupon.discount_value / 100);
                                if (coupon.max_discount_amount.HasValue && coupon.max_discount_amount > 0)
                                    discountApplied = Math.Min(discountApplied, coupon.max_discount_amount.Value);
                            }
                            else
                            {
                                discountApplied = coupon.discount_value;
                            }
                            discountApplied = Math.Min(discountApplied, currentCart.sub_total);

                            currentCart.Coupons.Add(new AppliedCouponInfo
                            {
                                coupon_id = coupon.coupon_id,
                                coupon_code = coupon.coupon_code,
                                discount_type = coupon.discount_type,
                                discount_value = coupon.discount_value,
                                discount_applied = discountApplied,
                                minimum_order_amount = coupon.minimum_order_amount,
                                can_be_clubbed = coupon.can_be_clubbed
                            });
                        }
                    }

                    RecalculateCartTotals(currentCart);
                    await _cache.SetObjectAsync(currentCart.cart_id, currentCart);

                    if (!isCartExists)
                        await _cache.SetStringAsync($"cart_{_currentUser.User.user_code}", currentCart.cart_id);

                    return ResponseEntity<object>.Success(currentCart, "Coupons updated successfully.");
                }

                // ── ✅ COUPON APPLY ───────────────────────────
                if (cart.coupon_action == "Apply" && !string.IsNullOrEmpty(cart.coupon_code))
                {
                    var applyResult = await ApplyCouponToCart(currentCart, cart.coupon_code.Trim().ToUpper());
                    if (!applyResult.Success)
                        return ResponseEntity<object>.Error(null, applyResult.Message);

                    RecalculateCartTotals(currentCart);
                    await _cache.SetObjectAsync(currentCart.cart_id, currentCart);
                    return ResponseEntity<object>.Success(currentCart, applyResult.Message);
                }

                // ── ✅ COUPON REMOVE ──────────────────────────
                if (cart.coupon_action == "Remove" && !string.IsNullOrEmpty(cart.coupon_code))
                {
                    var removeResult = RemoveCouponFromCart(currentCart, cart.coupon_code.Trim().ToUpper());

                    if (!removeResult.Success)
                        return ResponseEntity<object>.Error(null, removeResult.Message);

                    RecalculateCartTotals(currentCart);
                    await _cache.SetObjectAsync(currentCart.cart_id, currentCart);
                    return ResponseEntity<object>.Success(currentCart, removeResult.Message);
                }

                // ── 5. PRODUCT ADD / UPDATE / DELETE (from Method 1) ───────
                if (!isCouponOnlyAction)
                {
                    var product = await _productRepository.GetByIdAsync(cart.product_code);
                    if (product == null)
                        return ResponseEntity<object>.Error(null, "Product not found");

                    var existingItem = currentCart.Items.FirstOrDefault(i => i.product_code == cart.product_code);

                    if (existingItem == null && cart.action_event != "Delete")
                    {
                        currentCart.Items.Add(new CartItem
                        {
                            product_code = product.product_code,
                            product_name = product.name,
                            sku = product.sku,
                            unit_price = product.base_price,
                            qty = cart.qty,
                            net_amount = product.base_price * cart.qty,
                            tax_id = cart.tax_id,
                            tax_amount = cart.tax_amount,
                            discount_amount = 0,
                            total_amount = product.base_price * cart.qty,
                            tax_breakdown = cart.tax_breakdown ?? new List<CartItemTaxBreakdown>(),
                        });
                    }
                    else if (existingItem != null)
                    {
                        if (cart.action_event == "Delete")
                        {
                            currentCart.Items.RemoveAll(i => i.product_code == cart.product_code);
                            if (!currentCart.Items.Any())
                            {
                                currentCart.Coupons.Clear();
                                currentCart.coupon_discount = 0;
                            }
                        }
                        else
                        {
                            existingItem.qty = cart.action_event == "Update" ? cart.qty : existingItem.qty + cart.qty;
                            existingItem.tax_id = cart.tax_id;
                            existingItem.tax_amount = cart.tax_amount;
                            existingItem.net_amount = existingItem.unit_price * existingItem.qty;
                            // existingItem.total_amount = existingItem.net_amount + existingItem.tax_amount - existingItem.discount_amount;
                            existingItem.total_amount = existingItem.net_amount - existingItem.discount_amount;

                            if (cart.tax_breakdown != null && cart.tax_breakdown.Any())
                            {
                                existingItem.tax_breakdown = cart.tax_breakdown;
                            }
                        }
                    }
                }

                // ✅ ADD THIS — Update shipping address in cart if provided
                if (cart.shipping_address_id > 0)
                {
                    currentCart.shipping_address_id = cart.shipping_address_id;
                }

                // ── 6. SAVE ────────────────────────────────────────────────
                RecalculateCartTotals(currentCart);
                await _cache.SetObjectAsync(currentCart.cart_id, currentCart);

                if (!isCartExists)
                    await _cache.SetStringAsync($"cart_{_currentUser.User.user_code}", currentCart.cart_id);

                return ResponseEntity<object>.Success(currentCart);
            }
            catch (Exception ex)
            {
                return ResponseEntity<object>.Error(null, ex.Message);
            }
        }

        public async Task<IActionResult> GetCartByCartId(string cart_id)
        {
            try
            {
                if (string.IsNullOrEmpty(cart_id))
                    return ResponseEntity<object>.Error(null, "Invalid cart ID");

                bool isCartExists = await _cache.HasKey(cart_id);

                if (!isCartExists)
                    return ResponseEntity<object>.Error(null, "Cart not found");

                CurrentCart currentCart = await _cache.GetObjectAsync<CurrentCart>(cart_id);

                return ResponseEntity<object>.Success(currentCart);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetOrderByOrderId(string orderNo)
        {
            try
            {
                if (string.IsNullOrEmpty(orderNo))
                    return ResponseEntity<object>.Error(null, "Invalid order ID");


                var order = await _orderRepository.GetSingleOrDefaultAsync("select * from tbl_order where order_no= @order_no", new DynamicParameters(new { order_no = orderNo }));

                if (order == null)
                    return ResponseEntity<object>.Error(null, "Order not found");

                var orderItems = await _orderTxnRepository.QueryDynamicAsync("select * from tbl_order_txn where order_id= @order_id", new DynamicParameters(new { order_id = order.order_id }));

                if (orderItems == null)
                    return ResponseEntity<object>.Error(null, "Order items not found");

                OrderDto orderDto = new OrderDto();
                orderDto.order_id = order.order_id;
                orderDto.order_no = order.order_no;
                orderDto.user_code = order.user_code;
                orderDto.order_date = order.order_date;
                orderDto.sub_total = order.sub_total;
                orderDto.order_status = order.order_status;
                orderDto.shipping_amount = order.shipping_amount;
                orderDto.marketplace_fee = order.marketplace_fee;
                orderDto.net_amount = order.net_amount;
                orderDto.payment_method = order.payment_method;
                orderDto.payment_status = order.payment_status;
                orderDto.shipping_address_id = order.shipping_address_id;
                orderDto.billing_address_id = order.billing_address_id;
                orderDto.invoice_file_path = order.invoice_file_path;

                var param = new DynamicParameters();
                string sql = @"SELECT 
                                u.user_name, 
                                u.mobile_no, 
                                u.email_id, 
                                CONCAT_WS(' ', sa.address_line1, sa.address_line2) AS shipping_address, 
                                CONCAT_WS(' ', ba.address_line1, ba.address_line2) AS billing_address 
                            FROM tbl_users u 
                            LEFT JOIN tbl_customer_address_book sa ON sa.address_book_id = @shipping_address_id
                            LEFT JOIN tbl_customer_address_book ba ON ba.address_book_id = @billing_address_id
                            WHERE u.user_code = @user_code";
                param.Add("@user_code", order.user_code);
                param.Add("@shipping_address_id", order.shipping_address_id);
                param.Add("@billing_address_id", order.billing_address_id);
                var userDetails = await _userRepo.GetSingleOrDefaultAsync(sql, param);
                orderDto.userInfo = new
                {
                    user_name = userDetails.user_name,
                    email_id = userDetails.email_id,
                    mobile_no = userDetails.mobile_no,
                    shipping_address = userDetails.shipping_address,
                    billing_address = userDetails.billing_address
                };

                orderDto.delivery_charges = await _orderRepository.QueryDynamicAsync("select * from tbl_order_delivery_calculations where order_id = @order_id", new DynamicParameters(new { order_id = order.order_id }));
                orderDto.tax_breakout = await _orderRepository.QueryDynamicAsync("select * from tbl_order_tax where order_id = @order_id", new DynamicParameters(new { order_id = order.order_id }));

                foreach (var item in orderItems)
                {
                    string image_url = "";
                    var product = await _productRepository.GetByIdAsync(item.product_code);
                    var image = await _productImageRepository.GetSingleOrDefaultAsync("select image_url from tbl_product_images where product_code = @product_code and is_primary = true", new DynamicParameters(new { product_code = item.product_code }));
                    if (image is null)
                    {
                        image_url = "";
                    }
                    else
                    {
                        image_url = image.image_url;
                    }
                    OrderTxnDto orderItem = new OrderTxnDto
                    {
                        order_id = item.order_id,
                        product_code = item.product_code,
                        product_name = product.name,
                        sku = product.sku,
                        description = product.description,
                        unit_price = item.unit_price,
                        qty = item.qty,
                        net_amount = item.net_amount,
                        tax_id = item.tax_id,
                        tax_amount = item.tax_amount,
                        discount_amount = item.discount_amount,
                        total_amount = item.total_amount,
                        image_url = image_url
                    };
                    orderDto.orderItems.Add(orderItem);
                }

                return ResponseEntity<object>.Success(orderDto);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<IActionResult> GetAllOrderByUserCode()
        {
            long userCode = _currentUser.User.user_code;

            var param = new DynamicParameters();
            param.Add("UserCode", userCode);

            var orders = (await _orderRepository.QueryAsync<OrderDto>(
                "SELECT * FROM tbl_order WHERE user_code = @UserCode ORDER BY order_date DESC", param)).ToList();

            if (!orders.Any())
                return ResponseEntity<object>.Success(null, "No orders found");

            var orderIds = orders.Select(o => o.order_id).ToArray();

            var sql = @"
                SELECT DISTINCT 
                    t.order_id, t.product_code, t.unit_price, t.qty, t.net_amount, 
                    t.tax_id, t.tax_amount, t.discount_amount, t.total_amount,
                    p.name AS product_name, p.sku, p.description, pi.image_url,
                    CASE WHEN pr.product_code IS NOT NULL THEN TRUE ELSE FALSE END AS is_reviewed
                FROM tbl_order_txn t
                LEFT JOIN tbl_products p ON t.product_code = p.product_code
                LEFT JOIN tbl_product_images pi ON pi.product_code = p.product_code AND pi.is_primary = TRUE
                LEFT JOIN tbl_product_reviews pr ON pr.product_code = t.product_code 
                     AND pr.user_id = @UserCode AND pr.is_delete = FALSE
                WHERE t.order_id = ANY(@OrderIds)";

            param.Add("OrderIds", orderIds);

            var orderItems = await _orderTxnRepository.QueryAsync<OrderTxnDto>(sql, param);

            if (orderItems == null || !orderItems.Any())
                return ResponseEntity<object>.Error(null, "Order items not found");

            var itemsByOrder = orderItems.ToLookup(x => x.order_id);

            foreach (var order in orders)
            {
                order.orderItems = itemsByOrder[order.order_id].ToList();
                order.cart_details = null;
            }

            return ResponseEntity<object>.Success(orders);
        }

        public async Task<IActionResult> ChangeOrderStatus(string order_no, string order_status, string? shipment_ack_number, string? shipment_tracking_url)
        {
            try
            {
                if (string.IsNullOrEmpty(order_no))
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");
                if (string.IsNullOrEmpty(order_status))
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");


                var order = await _orderRepository.GetSingleOrDefaultAsync("select * from tbl_order where order_no = @order_no", new DynamicParameters(new { order_no = order_no }));

                if (order == null)
                    return ResponseEntity<object>.Error(null, "Order not found");

                if (order != null)
                {
                    if (order.order_status == OrderStatus.Cancelled.ToString())
                        return ResponseEntity<object>.Error(null, $"You can not change status for {OrderStatus.Cancelled.ToString()} order.");

                    if (order.order_status == OrderStatus.Shipped.ToString() && order_status == OrderStatus.Cancelled.ToString())
                        return ResponseEntity<object>.Error(null, $"You can not Cancelled the order, currently your order are in shipped mode.");

                    string oldStatus = order.order_status;
                    order.order_status = order_status;
                    order.shipment_ack_number = shipment_ack_number;
                    order.shipment_tracking_url = shipment_tracking_url;

                    var result = await _orderRepository.UpdateAsync(order);

                    var param = new DynamicParameters();
                    var sql = @"SELECT * FROM tbl_users where user_code = @user_code";
                    param.Add("user_code", order.user_code);
                    var users = await _userRepo.GetSingleOrDefaultAsync(sql, param);

                    order.user_name = users.user_name;

                    var orderItems = await _orderTxnRepository.QueryDynamicAsync("select * from tbl_order_txn where order_id= @order_id", new DynamicParameters(new { order_id = order.order_id }));

                    var emailItems = new List<object>();
                    foreach (var item in orderItems)
                    {
                        string imageUrl = "";
                        string product_name = "";
                        try
                        {
                            var image = await _productImageRepository.GetSingleOrDefaultAsync("select image_url from tbl_product_images where product_code = @product_code and is_primary = true",
                                new DynamicParameters(new { product_code = item.product_code }));
                            imageUrl = image?.image_url ?? "";

                            var productname = await _productRepository.GetSingleOrDefaultAsync("select name from tbl_products where product_code = @product_code", new DynamicParameters(new { product_code = item.product_code }));
                            product_name = productname?.name ?? "";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[OrderServices] Error fetching image for product {item.product_code}: {ex.Message}");
                        }

                        emailItems.Add(new
                        {
                            product_name = product_name,
                            qty = item.qty,
                            unit_price = item.unit_price,
                            total_amount = item.total_amount,
                            image_url = imageUrl
                        });
                    }

                    var emailPayload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "order_no", order.order_no },
                        { "order_date", order.order_date.ToString("dd MMM yyyy hh:mm tt") },
                        { "user_name", order.user_name },
                        { "items", emailItems }
                    };

                    if (order_status == "Processing")
                    {
                        await _alertEngineService.QueueEventNotificationAsync("PROCESSING_ORDER_STATUS", users.email_id, order.user_name, emailPayload);
                    }
                    else if (order_status == "Shipped")
                    {
                        await _alertEngineService.QueueEventNotificationAsync("SHIPPED_ORDER_STATUS", users.email_id, order.user_name, emailPayload);
                    }
                    else if (order_status == "Delivered")
                    {
                        await _alertEngineService.QueueEventNotificationAsync("DELIVERED_ORDER_STATUS", users.email_id, order.user_name, emailPayload);
                    }
                    else if (order_status == "Cancelled")
                    {
                        await _alertEngineService.QueueEventNotificationAsync("CANCELLED_ORDER_STATUS", users.email_id, order.user_name, emailPayload);
                    }
                    else
                    {
                        await _alertEngineService.QueueEventNotificationAsync("ORDER_STATUS_CHANGE", users.email_id, order.user_name, emailPayload);
                    }

                    return ResponseEntity<object>.Success(null, $"Order Status Update {oldStatus} To {order_status}.");

                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAllOrders(OrderFilter order)
        {
            try
            {
                var parameters = new DynamicParameters();
                string sql = @"SELECT 
                                o.order_id, 
                                o.order_no, 
                                o.order_date, 
                                o.sub_total, 
                                o.order_status, 
                                o.net_amount, 
                                o.payment_status, 
                                o.payment_method,
                                o.invoice_file_path,
                                u.user_name,
                                (SELECT STRING_AGG(cm.coupon_code, ', ') 
                                 FROM tbl_coupon_usages cu 
                                 JOIN tbl_coupon_masters cm ON cu.coupon_id = cm.coupon_id 
                                 WHERE cu.order_id = o.order_id) AS coupon_code,
                                (SELECT STRING_AGG(cc.category_name, ', ') 
                                 FROM tbl_coupon_usages cu 
                                 JOIN tbl_coupon_masters cm ON cu.coupon_id = cm.coupon_id 
                                 JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id
                                 WHERE cu.order_id = o.order_id) AS coupon_category_name
                            FROM tbl_order o
                            LEFT JOIN tbl_users u ON o.user_code = u.user_code
                            WHERE 1=1";

                if (order.from_date.HasValue && order.to_date.HasValue)
                {
                    sql += " AND (o.order_date >= @StartDateFilter AND o.order_date <= @EndDateFilter)";
                    parameters.Add("StartDateFilter", order.from_date.Value.Date);
                    parameters.Add("EndDateFilter", order.to_date.Value.Date.AddDays(1).AddTicks(-1));
                }
                else if (order.from_date.HasValue)
                {
                    sql += " AND o.order_date >= @StartDateFilter";
                    parameters.Add("StartDateFilter", order.from_date.Value);
                }
                else if (order.to_date.HasValue)
                {
                    sql += " AND o.order_date <= @EndDateFilter";
                    parameters.Add("EndDateFilter", order.to_date.Value);
                }

                if (!string.IsNullOrEmpty(order.order_status))
                {
                    sql += " AND o.order_status = @order_status";
                    parameters.Add("order_status", order.order_status);
                }
                if (!string.IsNullOrEmpty(order.payment_status))
                {
                    sql += " AND o.payment_status = @payment_status";
                    parameters.Add("payment_status", order.payment_status);
                }

                if (!string.IsNullOrEmpty(order.order_no))
                {
                    sql += " AND o.order_no LIKE @order_no";
                    parameters.Add("order_no", $"%{order.order_no}%");
                }

                //var orders = await _orderRepository.QueryPagedAsync<Order>(sql, order, parameters);
                var orders = await _orderRepository.QueryAsync<Order>(sql, parameters);

                if (orders != null)
                {
                    var filteredResponse = orders.Select(o => new
                    {
                        order_id = o.order_id,
                        order_no = o.order_no,
                        order_date = o.order_date,
                        sub_total = o.sub_total,
                        coupon_code = o.coupon_code,
                        coupon_category_name = o.coupon_category_name,
                        user_name = o.user_name,
                        order_status = o.order_status,
                        net_amount = o.net_amount,
                        payment_status = o.payment_status,
                        payment_method = o.payment_method,
                        cart_details = o.cart_details,
                        invoice_file_path = o.invoice_file_path,
                    }).ToList();

                    return ResponseEntity<object>.Success(filteredResponse);
                }
                else
                    return ResponseEntity<object>.Error(null, "Order not available.");


            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> ChangePaymentStatus(string order_no, string payment_status)
        {
            try
            {
                if (string.IsNullOrEmpty(order_no))
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");
                if (string.IsNullOrEmpty(payment_status))
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty");


                var order = await _orderRepository.GetSingleOrDefaultAsync("select * from tbl_order where order_no = @order_no", new DynamicParameters(new { order_no = order_no }));

                if (order == null)
                    return ResponseEntity<object>.Error(null, "Order not found");

                if (order.payment_status == payment_status)
                    return ResponseEntity<object>.Error(null, "You can not update the payment status to current status");

                if (order != null)
                {
                    string oldPaymentStatus = order.payment_status;
                    order.payment_status = payment_status;

                    var result = await _orderRepository.UpdateAsync(order);

                    var param = new DynamicParameters();
                    var sql = @"SELECT * FROM tbl_users where user_code = @user_code";
                    param.Add("user_code", order.user_code);
                    var users = await _userRepo.GetSingleOrDefaultAsync(sql, param);

                    order.user_name = users.user_name;

                    var orderItems = await _orderTxnRepository.QueryDynamicAsync("select * from tbl_order_txn where order_id= @order_id", new DynamicParameters(new { order_id = order.order_id }));

                    var emailItems = new List<object>();
                    foreach (var item in orderItems)
                    {
                        string imageUrl = "";
                        string product_name = "";

                        try
                        {
                            var image = await _productImageRepository.GetSingleOrDefaultAsync("select image_url from tbl_product_images where product_code = @product_code and is_primary = true", new DynamicParameters(new { product_code = item.product_code }));
                            imageUrl = image?.image_url ?? "";

                            var productname = await _productRepository.GetSingleOrDefaultAsync("select name from tbl_products where product_code = @product_code", new DynamicParameters(new { product_code = item.product_code }));
                            product_name = productname?.name ?? "";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[OrderServices] Error fetching image for product {item.product_code}: {ex.Message}");
                        }

                        emailItems.Add(new
                        {
                            product_name = product_name,
                            qty = item.qty,
                            unit_price = item.unit_price,
                            total_amount = item.total_amount,
                            image_url = imageUrl
                        });
                    }

                    var emailPayload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "order_no", order.order_no },
                        { "order_date", order.order_date.ToString("dd MMM yyyy hh:mm tt") },
                        { "user_name", order.user_name },
                        { "items", emailItems }
                    };

                    await _alertEngineService.QueueEventNotificationAsync("PAYMENT_STATUS_CHANGE", users.email_id, order.user_name, emailPayload);

                    return ResponseEntity<object>.Success(null, $"Payment Status Update {oldPaymentStatus} To {payment_status}.");

                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UploadOrderInvoice(IFormCollection form)
        {
            try
            {
                if (form == null)
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.", HttpStatusCode.InternalServerError);

                if (form.Files == null || form.Files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "Order Invoice is required.", HttpStatusCode.InternalServerError);

                var files = form.Files;
                string orderCode = form["order_code"];
                string invoice_number = form["invoice_number"];

                return await UploadOrderInvoice(files, orderCode, invoice_number);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UploadOrderInvoice(IFormFileCollection files, string orderCode, string invoice_number)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                    return ResponseEntity<object>.Error(null, "Passing value are null or empty.", HttpStatusCode.InternalServerError);

                if (files.Count <= 0)
                    return ResponseEntity<object>.Error(null, "Order Invoice is required.", HttpStatusCode.InternalServerError);

                Order order = await _orderRepository.GetSingleOrDefaultAsync("select * from tbl_order where order_no = @order_no", new DynamicParameters(new { order_no = orderCode }));

                if (order == null || string.IsNullOrEmpty(order.order_no))
                    return ResponseEntity<object>.Error(null, "Order not found.", HttpStatusCode.InternalServerError);

                string[] array = { "pdf" };

                IFormFile _file;
                _file = files[0];

                using var stream = _file.OpenReadStream();

                string fileExtension = Path.GetExtension(_file.FileName);

                if (array.Contains(fileExtension))
                    return ResponseEntity<object>.Error(null, "Only pdf file are allowed.", HttpStatusCode.InternalServerError);

                string file_name = $"{orderCode}_{StringHelper.GetUniqueString(10)}{Path.GetExtension(files[0].FileName)}";
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file_name, stream),
                    Folder = "EComm/Order/Invoice",// the specific folder you want in Cloudinary
                    UseFilename = true,                   // use original filename
                    UniqueFilename = true,                // let Cloudinary add uniqueness
                    Overwrite = false,                    // do not overwrite existing
                                                          //ResourceType = ResourceType.Image
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK && uploadResult.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    return ResponseEntity<object>.Error(null, "Failed to upload order invoice, Please contact your administrator.", HttpStatusCode.InternalServerError);
                }

                // public url (https)
                var publicUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Uri?.ToString();

                //update invoice path in order table
                order.invoice_file_path = publicUrl;
                order.invoice_number = invoice_number;
                var result = await _orderRepository.UpdateAsync(order);

                if (Convert.ToInt32(result) > 0)
                    return ResponseEntity<object>.Success(null, "Order Invoice uploaded successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to upload order invoice, Please contact your administrator.", HttpStatusCode.InternalServerError);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void RecalculateCartTotals(CurrentCart cart)
        {
            cart.sub_total = cart.Items.Sum(i => i.net_amount);
            decimal baseNetAmount = cart.Items.Sum(i => i.total_amount) + cart.shipping_amount + cart.marketplace_fee;
            decimal totalCouponDiscount = 0;
            foreach (var coupon in cart.Coupons)
            {
                decimal discountApplied = coupon.discount_applied;
                coupon.discount_applied = discountApplied;
                totalCouponDiscount += discountApplied;
            }

            cart.coupon_discount = totalCouponDiscount;
            cart.net_amount = baseNetAmount;
            cart.grand_total = Math.Max(0, baseNetAmount - totalCouponDiscount);
        }

        private async Task<(bool Success, string Message)> ApplyCouponToCart(CurrentCart cart, string couponCode)
        {
            bool alreadyApplied = cart.Coupons.Any(c => c.coupon_code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

            if (alreadyApplied)
                return (false, $"Coupon '{couponCode}' is already applied.");

            var parameters = new DynamicParameters();
            parameters.Add("couponCode", couponCode);
            CouponMaster coupon = await _couponRepository.GetSingleOrDefaultAsync("SELECT cm.*,cc.category_name,cc.can_be_clubbed FROM tbl_coupon_masters cm LEFT JOIN tbl_coupon_category cc ON cm.coupon_category_id = cc.coupon_category_id WHERE cm.coupon_code = @couponCode AND cm.is_deleted = false", parameters);

            if (coupon == null || coupon.is_deleted)
                return (false, "Coupon code not found.");

            if (!coupon.is_active)
                return (false, "This coupon is no longer active.");

            DateTime now = DateTime.Now;
            if (now < coupon.start_date || now > coupon.end_date)
                return (false, "This coupon has expired or is not yet valid.");

            if (cart.sub_total < coupon.minimum_order_amount)
                return (false, $"Minimum order amount of ₹{coupon.minimum_order_amount:F2} required.");

            if (cart.Coupons.Count > 0 && !coupon.can_be_clubbed)
                return (false, "This coupon cannot be combined with other coupons.");

            bool existingNonClubbable = cart.Coupons.Any(c => !c.can_be_clubbed);
            if (existingNonClubbable)
                return (false, "An already applied coupon cannot be combined with other coupons.");

            if (coupon.max_usages_total > 0)
            {
                var param = new DynamicParameters();
                param.Add("Id", coupon.coupon_id);

                string couponQuery = @"SELECT COUNT(*) as max_usages_total FROM tbl_coupon_usages WHERE coupon_id = @couponId AND is_deleted = = false";
                var coupans = await _couponRepository.GetSingleOrDefaultAsync(couponQuery, param);
                if (coupans.max_usages_total >= coupon.max_usages_total)
                    return (false, "This coupon has reached its maximum usage limit.");
            }

            if (coupon.max_usages_per_user > 0)
            {
                var param = new DynamicParameters();
                param.Add("couponId", coupon.coupon_id);
                param.Add("userCode", cart.user_code);

                var couponQuery = @"SELECT COUNT(*) as max_usages_per_user FROM tbl_coupon_usages WHERE coupon_id = @couponId AND user_code = @userCode AND is_deleted = false";
                var coupansdata = await _couponRepository.GetSingleOrDefaultAsync(couponQuery, param);

                if (coupansdata.max_usages_per_user >= coupon.max_usages_per_user)
                    return (false, "You have already used this coupon the maximum allowed times.");
            }

            decimal discountApplied = 0;
            if (coupon.discount_type == "Percentage")
            {
                discountApplied = cart.sub_total * (coupon.discount_value / 100);
                if (coupon.max_discount_amount.HasValue && coupon.max_discount_amount > 0)
                    discountApplied = Math.Min(discountApplied, coupon.max_discount_amount.Value);
            }
            else
            {
                discountApplied = coupon.discount_value;
            }

            discountApplied = Math.Min(discountApplied, cart.sub_total);

            cart.Coupons.Add(new AppliedCouponInfo
            {
                coupon_id = coupon.coupon_id,
                coupon_code = coupon.coupon_code,
                discount_type = coupon.discount_type,
                discount_value = coupon.discount_value,
                discount_applied = discountApplied,
                minimum_order_amount = coupon.minimum_order_amount,
                can_be_clubbed = coupon.can_be_clubbed
            });

            return (true, $"Coupon '{couponCode}' applied successfully!");
        }

        private (bool Success, string Message) RemoveCouponFromCart(CurrentCart cart, string couponCode)
        {
            AppliedCouponInfo couponToRemove = cart.Coupons.FirstOrDefault(c => c.coupon_code.Equals(couponCode, StringComparison.OrdinalIgnoreCase));

            if (couponToRemove == null)
                return (false, $"Coupon '{couponCode}' is not applied.");

            cart.Coupons.Remove(couponToRemove);
            return (true, $"Coupon '{couponCode}' removed successfully.");
        }

    }
}