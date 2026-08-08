using DUSTER.EComm.Services.Modules.Customers.Models;

namespace DUSTER.EComm.Services.Modules.Customers
{
    public interface ICustomerServices
    {
        #region Address Book

        Task<IActionResult> AddAddressBook(AddressBook model);
        Task<IActionResult> UpdateAddressBook(AddressBook model);
        Task<IActionResult> DeleteAddressBook(long address_book_id);
        Task<IActionResult> GetAddressBooksByUserCode(long user_code);
        Task<IActionResult> GetAddressBookById(long address_book_id);
        Task<IActionResult> GetDefaultShippingAddressByUserCode(long user_code);
        Task<IActionResult> GetDefaultBillingAddressByUserCode(long user_code);
        Task<IActionResult> SetDefaultShippingAddress(long address_book_id, long user_code);
        Task<IActionResult> SetDefaultBillingAddress(long address_book_id, long user_code);

        #endregion
    }
}
