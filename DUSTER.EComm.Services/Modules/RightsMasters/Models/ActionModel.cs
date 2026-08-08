namespace DUSTER.EComm.Services.Modules.RightsMasters.Models
{
    [Table("tbl_actions")]
    public class ActionModel : BaseEntity
    {
        [Key]
        public long action_code { get; set; } // Primary key for the action
        public string? action_name { get; set; } // Name of the action
        public string? action_special_name { get; set; }
        public string? action_path { get; set; }
        public string? action_description { get; set; } // Description of the action
        public Boolean is_active { get; set; } // Indicates if the action is active
        public Boolean is_block { get; set; } // Indicates if the action is blocked
        public long parent_code { get; set; }
        public string? action_for { get; set; } // PORTAL,MOBILE,FREE_ACTION
        public string? key_name { get; set; }
        public string? icon { get; set; }
        public Boolean is_navigable { get; set; } // true if action is navigable otherwise false.
    }

    public class ActionTreeNodeDto
    {
        public string Key { get; set; } = string.Empty;     // action_code
        public string Title { get; set; } = string.Empty;   // action_name
        public string? Icon { get; set; }
        public bool IsLeaf { get; set; }
        public bool Disabled { get; set; }
        public bool IsChecked { get; set; }
        public List<ActionTreeNodeDto> Children { get; set; } = new();
    }

    public class SidebarMenuDto
    {
        public long ActionCode { get; set; }      // unique identifier
        public string Title { get; set; } = null!;
        public string? Route { get; set; }         // null for pure parents
        public string? Icon { get; set; }
        public List<SidebarMenuDto> Children { get; set; } = new();
    }


}
