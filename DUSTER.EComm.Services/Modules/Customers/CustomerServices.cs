using Dapper;
using DUSTER.EComm.Data;
using DUSTER.EComm.Data.Infrastructure;
using DUSTER.EComm.Services.Modules.Customers.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DUSTER.EComm.Services.Modules.Customers
{
    public class CustomerServices : ICustomerServices
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IEIPLRepository<AddressBook> _addressBookRepo;

        #region Address Book

        public CustomerServices(ICurrentUserService currentUserService, IEIPLRepository<AddressBook> addressBookRepo)
        {
            _currentUserService = currentUserService;
            _addressBookRepo = addressBookRepo;
        }

        public async Task<IActionResult> AddAddressBook(AddressBook model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Passing data are null or empty.");

                var validator = await _addressBookRepo.ModelValidating(new ValidationModel() { ValidateModel = new AddressBookValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var response = await _addressBookRepo.InsertAsync(model);

                if (Convert.ToInt64(response) > 0)
                    return ResponseEntity<object>.Success(null, "Address added successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to add address");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAddressBooksByUserCode(long user_code)
        {
            try
            {
                if (user_code == 0)
                    return ResponseEntity<object>.Error(null, "User code is required.");

                var param = new DynamicParameters();
                param.Add("user_code", user_code);

                var data = await _addressBookRepo.QueryAsync<AddressBook>("select * from tbl_customer_address_book where user_code=@user_code and is_active = true", param);

                if (data.Count() <= 0)
                    return ResponseEntity<object>.Success(null, "No address books found for the user");

                var response = data.Select(data => new
                {
                    data.address_book_id,
                    data.user_code,
                    data.full_name,
                    data.mobile_number,
                    data.address_line1,
                    data.address_line2,
                    data.city,
                    data.state,
                    data.country,
                    data.pin_code,
                    data.is_default_billing,
                    data.is_default_shipping,
                    data.is_active
                }).ToList();

                if (response != null)
                    return ResponseEntity<object>.Success(response, "Address books retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No address books found for the given user code.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> UpdateAddressBook(AddressBook model)
        {
            try
            {
                if (model == null)
                    return ResponseEntity<object>.Error(null, "Passing data are null or empty.");

                var validator = await _addressBookRepo.ModelValidating(new ValidationModel() { ValidateModel = new AddressBookValidator(), Model = model });

                if (validator is ResponseEntity<object> errorResponse && errorResponse.StatusCode != 200)
                    return errorResponse;

                var response = await _addressBookRepo.UpdateAsync(model);

                if (response > 0)
                    return ResponseEntity<object>.Success(response, "Address updated successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to update address");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> DeleteAddressBook(long address_book_id)
        {
            try
            {
                if (address_book_id == 0)
                    return ResponseEntity<object>.Error(null, "passing data are null or empty.");

                var param = new DynamicParameters();
                param.Add("address_book_id", address_book_id);

                AddressBook address = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where address_book_id=@address_book_id", param);

                if (address == null)
                    return ResponseEntity<object>.Success(null, "No address books found for the user");

                address.is_active = false;
                var response = await _addressBookRepo.UpdateAsync(address);

                if (response > 0)
                    return ResponseEntity<object>.Success(response, "Address deleted successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to delete address");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetAddressBookById(long address_book_id)
        {
            try
            {
                if (address_book_id == 0)
                    return ResponseEntity<object>.Error(null, "Address book id is required.");

                var data = await _addressBookRepo.GetByIdAsync(address_book_id);

                if (data == null)
                    return ResponseEntity<object>.Error(null, "No address book found for the given data");

                var response = new
                {
                    data.address_book_id,
                    data.user_code,
                    data.full_name,
                    data.address_line1,
                    data.address_line2,
                    data.city,
                    data.state,
                    data.country,
                    data.pin_code,
                    data.is_default_billing,
                    data.is_default_shipping
                };
                if (response != null)
                    return ResponseEntity<object>.Success(response, "Address book retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No address book found for the given id.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetDefaultShippingAddressByUserCode(long user_code)
        {
            try
            {
                if (user_code == 0)
                    return ResponseEntity<object>.Error(null, "User code is required.");
                var param = new DynamicParameters();
                param.Add("user_code", user_code);

                var data = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where user_code=@user_code and is_active = true and is_default_shipping = true", param);

                if (data == null)
                    return ResponseEntity<object>.Error(null, "No default shipping address found for the user");

                var response = new
                {
                    data.address_book_id,
                    data.user_code,
                    data.full_name,
                    data.address_line1,
                    data.address_line2,
                    data.city,
                    data.state,
                    data.country,
                    data.pin_code,
                    data.is_default_billing,
                    data.is_default_shipping
                };
                if (response != null)
                    return ResponseEntity<object>.Success(response, "Default shipping address retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No default shipping address found for the given user code.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> GetDefaultBillingAddressByUserCode(long user_code)
        {
            try
            {
                if (user_code == 0)
                    return ResponseEntity<object>.Error(null, "User code is required.");

                var param = new DynamicParameters();
                param.Add("user_code", user_code);

                var data = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where user_code=@user_code and is_active = true and is_default_billing = true", param);

                if (data == null)
                    return ResponseEntity<object>.Error(null, "No default billing address found for the user");

                var response = new
                {
                    data.address_book_id,
                    data.user_code,
                    data.full_name,
                    data.address_line1,
                    data.address_line2,
                    data.city,
                    data.state,
                    data.country,
                    data.pin_code,
                    data.is_default_billing,
                    data.is_default_shipping
                };

                if (response != null)
                    return ResponseEntity<object>.Success(response, "Default billing address retrieved successfully.");
                else
                    return ResponseEntity<object>.Error(null, "No default billing address found for the given user code.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> SetDefaultShippingAddress(long address_book_id, long user_code)
        {
            try
            {
                if (address_book_id == 0 || user_code == 0)
                    return ResponseEntity<object>.Error(null, "passing data are null or empty");

                var param = new DynamicParameters();
                param.Add("address_book_id", address_book_id);
                param.Add("user_code", user_code);

                List<AddressBook> addressBooks = (await _addressBookRepo.QueryAsync<AddressBook>($"select * from tbl_customer_address_book where is_active = true and user_code={user_code}")).ToList();

                if (addressBooks == null || addressBooks.Count == 0)
                    return ResponseEntity<object>.Error(null, "No address book found for the given user");

                foreach (AddressBook addressBook in addressBooks.Where(x => x.is_default_shipping == true))
                {
                    addressBook.is_default_shipping = false;
                    var update = await _addressBookRepo.UpdateAsync(addressBook);
                }

                AddressBook response = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where is_active = true and user_code=@user_code and address_book_id=@address_book_id", param);

                if (response == null)
                    return ResponseEntity<object>.Error(null, "No address book found for the given data");

                response.is_default_shipping = true;

                var updateResponse = await _addressBookRepo.UpdateAsync(response);

                if (updateResponse > 0)
                    return ResponseEntity<object>.Success(null, "Default shipping address set successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to set default shipping address");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> SetDefaultBillingAddress(long address_book_id, long user_code)
        {
            try
            {
                if (address_book_id == 0 || user_code == 0)
                    return ResponseEntity<object>.Error(null, "passing data are null or empty");

                var param = new DynamicParameters();
                param.Add("address_book_id", address_book_id);
                param.Add("user_code", user_code);

                List<AddressBook> addressBooks = (await _addressBookRepo.QueryAsync<AddressBook>($"select * from tbl_customer_address_book where is_active = true and user_code={user_code}")).ToList();

                if (addressBooks == null || addressBooks.Count == 0)
                    return ResponseEntity<object>.Error(null, "No address book found for the given user");

                foreach (AddressBook addressBook in addressBooks.Where(x => x.is_default_billing == true))
                {
                    addressBook.is_default_billing = false;
                    var update = await _addressBookRepo.UpdateAsync(addressBook);
                }

                AddressBook response = await _addressBookRepo.GetSingleOrDefaultAsync("select * from tbl_customer_address_book where is_active = true and user_code=@user_code and address_book_id=@address_book_id", param);

                if (response == null)
                    return ResponseEntity<object>.Error(null, "No address book found for the given data");

                response.is_default_billing = true;

                var updateResponse = await _addressBookRepo.UpdateAsync(response);

                if (updateResponse > 0)
                    return ResponseEntity<object>.Success(null, "Default billing address set successfully.");
                else
                    return ResponseEntity<object>.Error(null, "Failed to set default shipping address");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

    }
}
