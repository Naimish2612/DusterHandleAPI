namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_manufacturer")]
    public class Manufacturer : BaseEntity
    {
        [Key]
        public int manufacturer_id { get; set; }
        public string? name { get; set; }
        public string? website { get; set; }
        public string? support_email { get; set; }
    }

    public class ManufacturerValidator : AbstractValidator<Manufacturer>
    {
        public ManufacturerValidator()
        {
            RuleFor(x => x.name).NotEmpty().WithMessage("Name is required.");
        }
    }
}
