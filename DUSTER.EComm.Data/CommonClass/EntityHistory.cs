using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUSTER.EComm.Data.CommonClass
{
    [Table("entity_history")]
    public class EntityHistory : BaseEntity
    {
        [Key]
        public long id { get; set; }
        public string table_name { get; set; } = string.Empty;
        public string primary_key_id { get; set; }
        public string change_type { get; set; } = string.Empty;
        public string changed_by { get; set; } = string.Empty;
        public DateTime changed_at { get; set; } = DateTime.Now;
        public string data_before { get; set; } = string.Empty;
        public string data_after { get; set; } = string.Empty;

    }
}
