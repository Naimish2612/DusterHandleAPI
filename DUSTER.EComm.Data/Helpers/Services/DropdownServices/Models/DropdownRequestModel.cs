namespace DUSTER.EComm.Data.Helpers.Services.DropdownServices.Models
{
    public class DropdownRequestModel
    {
        public string table_name { get; set; } = string.Empty;
        public string? table_columns { get; set; } = string.Empty;
        public Dictionary<string, object>? StaticFilters { get; set; }  // e.g. { "is_active": true }
    }
}
