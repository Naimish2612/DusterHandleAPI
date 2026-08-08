using DUSTER.EComm.Data.Helpers.Pagination;

namespace DUSTER.EComm.Services.Modules.Masters.Models
{
    [Table("tbl_banners")]
    public class BannerMaster : BaseEntity
    {
        public BannerMaster()
        {

        }

        [Key]
        public long banner_id { get; set; }
        public string? banner_title { get; set; }
        public string? description { get; set; }
        public string? image_url { get; set; }
        public string? redirect_url { get; set; }

        // Options: 'Web', 'Mobile', 'Both'
        public string platform { get; set; } = "Both";

        // 0 for Pan India, specific ID for state-wise
        public int state_id { get; set; } = 0;
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public bool is_active { get; set; } = true;
        public bool is_default { get; set; } = false;
        public int display_order { get; set; } = 0;
        public string? image_public_id { get; set; }
    }

    public class BannerMasterValidator : AbstractValidator<BannerMaster>
    {
        public BannerMasterValidator()
        {
            RuleFor(x => x.banner_title).NotEmpty().WithMessage("Banner Title Required.");
            //RuleFor(x => x.image_url).NotEmpty().WithMessage("Image URL Required.");
            RuleFor(x => x.platform).NotEmpty().WithMessage("Please Select Platform");

        }
    }

    public class BannerFilters : PaginationParams
    {
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public bool? is_active { get; set; }
        public string? platform { get; set; }
        public int? state_id { get; set; }
    }
}
