using DUSTER.EComm.Services.Modules.Orders.Models;
using Microsoft.AspNetCore.Http;

namespace DUSTER.EComm.Services.Modules.Orders
{
    public interface IOrderServices
    {
        Task<IActionResult> AddToCart(CartItemRequest cart);
        Task<IActionResult> GetCartByCartId(string cart_id);
        Task<IActionResult> PlaceOrder(OrderDto order);
        Task<IActionResult> GetOrderByOrderId(string orderNo);
        Task<IActionResult> GetAllOrderByUserCode();
        Task<IActionResult> ChangeOrderStatus(string order_no, string order_status ,string? shipment_ack_number, string? shipment_tracking_url);
        Task<IActionResult> GetAllOrders(OrderFilter order);
        Task<IActionResult> ChangePaymentStatus(string order_no, string order_status);
        Task<IActionResult> UploadOrderInvoice(IFormCollection form);
    }
}
