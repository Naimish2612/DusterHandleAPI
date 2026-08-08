namespace DUSTER.EComm.Services.Modules.RightsMasters.Models
{
    [Table("tbl_permission_action_mapping")]
    public class PermissionActionMapping : BaseEntity
    {
        [Key]
        public long permission_action_code { get; set; } // Primary key for the mapping
        public long permission_code { get; set; }
        public long action_code { get; set; } // Foreign key to PermissionActionModel

        [Computed]
        public long[] action_codes { get; set; }

    }
}
