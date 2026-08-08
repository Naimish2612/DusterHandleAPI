namespace DUSTER.EComm.Services.Modules.Customers.Models
{
    [Table("tbl_customer_address_book")]
    public class AddressBook : BaseEntity
    {
        [Key]
        public long address_book_id { get; set; }
        public long user_code { get; set; }
        public string? full_name { get; set; }
        public string? mobile_number { get; set; }
        public string? address_line1 { get; set; }
        public string? address_line2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }
        public string? pin_code { get; set; }
        public bool is_default_shipping { get; set; }
        public bool is_default_billing { get; set; }
        public bool is_active { get; set; } = true;

    }

    public class AddressBookValidator : AbstractValidator<AddressBook>
    {
        public AddressBookValidator()
        {
            RuleFor(x => x.user_code).NotEmpty().WithMessage("User code is required.");
            RuleFor(x => x.full_name).NotEmpty().WithMessage("Full name is required.");
            RuleFor(x => x.mobile_number).NotEmpty().WithMessage("Mobile number is required.");
            RuleFor(x => x.address_line1).NotEmpty().WithMessage("Address line 1 is required.");
            RuleFor(x => x.city).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.state).NotEmpty().WithMessage("State is required.");
            RuleFor(x => x.country).NotEmpty().WithMessage("Country is required.");
            RuleFor(x => x.pin_code).NotEmpty().WithMessage("Pin code is required.");
        }
    }

    public class AddressBookDTO
    {
        public long address_book_id { get; set; }
        public long user_code { get; set; }
        public string? full_name { get; set; }
        public string? mobile_number { get; set; }
        public string? address_line1 { get; set; }
        public string? address_line2 { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? country { get; set; }
        public string? pin_code { get; set; }
        public bool is_default_shipping { get; set; }
        public bool is_default_billing { get; set; }
    }
}
