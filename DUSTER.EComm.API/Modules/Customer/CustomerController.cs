using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Catelog;
using DUSTER.EComm.Services.Modules.Catelog.Models;
using DUSTER.EComm.Services.Modules.Customers;
using DUSTER.EComm.Services.Modules.Customers.Models;

namespace DUSTER.EComm.API.Modules.Customer
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerServices _customerService;
        private readonly ICatelogServices _catelogServices;
        private readonly ICurrentUserService _currentUserService;

        public CustomerController(ICustomerServices customerServices, ICatelogServices catelogServices, ICurrentUserService currentUserService)
        {
            _customerService = customerServices;
            _catelogServices = catelogServices;
            _currentUserService = currentUserService;
        }

        #region Address Book

        [HttpPost("add/address")]
        public async Task<IActionResult> AddAddressBook([FromBody] AddressBook model)
        {
            return await _customerService.AddAddressBook(model);
        }

        [HttpPost("update/address")]
        public async Task<IActionResult> UpdateAddressBook([FromBody] AddressBook model)
        {
            return await _customerService.UpdateAddressBook(model);
        }

        [HttpGet("delete/address/{address_book_id}")]
        public async Task<IActionResult> DeleteAddressBook(long address_book_id)
        {
            return await _customerService.DeleteAddressBook(address_book_id);
        }

        [HttpGet("get/addresses/{user_code}")]
        public async Task<IActionResult> GetAddressBooksByUserCode(long user_code)
        {
            return await _customerService.GetAddressBooksByUserCode(user_code);
        }

        [HttpGet("get/address/{address_book_id}")]
        public async Task<IActionResult> GetAddressBookById(long address_book_id)
        {
            return await _customerService.GetAddressBookById(address_book_id);
        }

        [HttpGet("get/default-shipping-address/{user_code}")]
        public async Task<IActionResult> GetDefaultShippingAddressByUserCode(long user_code)
        {
            return await _customerService.GetDefaultShippingAddressByUserCode(user_code);
        }

        [HttpGet("get/default-billing-address/{user_code}")]
        public async Task<IActionResult> GetDefaultBillingAddressByUserCode(long user_code)
        {
            return await _customerService.GetDefaultBillingAddressByUserCode(user_code);
        }

        [HttpGet("set/default-shipping-address/{address_book_id}/{user_code}")]
        public async Task<IActionResult> SetDefaultShippingAddress(long address_book_id, long user_code)
        {
            return await _customerService.SetDefaultShippingAddress(address_book_id, user_code);
        }

        [HttpGet("set/default-billing-address/{address_book_id}/{user_code}")]
        public async Task<IActionResult> SetDefaultBillingAddress(long address_book_id, long user_code)
        {
            return await _customerService.SetDefaultBillingAddress(address_book_id, user_code);
        }

        #endregion

        #region Product Wishlist

        [HttpPost("add-to-wishlist")]
        public async Task<IActionResult> AddToWishlist([FromBody] ProductWishlistDTO product)
        {
            try
            {
                return await _catelogServices.AddToWishlist(product);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("remove-from-wishlist")]
        public async Task<IActionResult> RemoveFromWishlist([FromBody] ProductWishlistDTO product)
        {
            try
            {
                return await _catelogServices.RemoveFromWishlist(product);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("my-wishlist")]
        public async Task<IActionResult> GetWishlistProduct()
        {
            try
            {
                return await _catelogServices.GetProductWishlist(_currentUserService.User.user_code);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #endregion
    }
}
