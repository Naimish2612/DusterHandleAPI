using DUSTER.EComm.Data.CommonClass;
using DUSTER.EComm.Data.Helpers.CacheMemory;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Orders;
using DUSTER.EComm.Services.Modules.Orders.Models;

namespace DUSTER.EComm.API.Modules.Orders
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderServices _orderService;
        private readonly IPostgresDistributedCache _cache;
        private readonly ICurrentUserService _currentUser;
        public OrderController(IOrderServices orderService, IPostgresDistributedCache cache, ICurrentUserService currentUser)
        {
            _orderService = orderService;
            _cache = cache;
            _currentUser = currentUser;
        }

        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] CartItemRequest cart)
        {
            var result = await _orderService.AddToCart(cart);
            return result;
        }

        [HttpGet("get-cart/{cart_id}")]
        public async Task<IActionResult> GetCart(string cart_id)
        {
            var result = await _orderService.GetCartByCartId(cart_id);
            return result;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderDto order)
        {
            var result = await _orderService.PlaceOrder(order);
            return result;
        }

        [HttpGet("get-order/{order_no}")]
        public async Task<IActionResult> GetOrder(string order_no)
        {
            var result = await _orderService.GetOrderByOrderId(order_no);
            return result;
        }

        [HttpGet("get-my-order")]
        public async Task<IActionResult> GetMyAllOrder()
        {
            var result = await _orderService.GetAllOrderByUserCode();
            return result;
        }

        [HttpPost("change/order/status")]
        public async Task<IActionResult> ChangeOrderStatus([FromBody] OrderDto order)
        {
            return await _orderService.ChangeOrderStatus(order.order_no, order.order_status, order.shipment_ack_number, order.shipment_tracking_url);
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetAllOrders([FromBody] OrderFilter order)
        {
            var result = await _orderService.GetAllOrders(order);
            return result;
        }

        [HttpPost("change/payment/status")]
        public async Task<IActionResult> ChangePayment([FromBody] OrderDto order)
        {
            return await _orderService.ChangePaymentStatus(order.order_no, order.payment_status);
        }

        [HttpGet("get/open-cart")]
        public async Task<IActionResult> GetOpenCart()
        {
            //await _cache.SetStringAsync($"cart_{_currentUser.User.user_code}", newCart.cart_id);

            if (await _cache.HasKey($"cart_{_currentUser.User.user_code}"))
            {
                string cart_id = await _cache.GetStringAsync($"cart_{_currentUser.User.user_code}");

                if (string.IsNullOrEmpty(cart_id))
                    return ResponseEntity<object>.Success(null, "No open cart found for the current user.");

                return ResponseEntity<object>.Success(new { current_cart_id = cart_id });
            }
            else
                return ResponseEntity<object>.Success(null, "No open cart found for the current user.");
        }

        [HttpPost("upload/invoice")]
        public async Task<IActionResult> UploadInvoice(IFormCollection form)
        {
            var result = await _orderService.UploadOrderInvoice(form);
            return result;
        }
    }
}
