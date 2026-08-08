using System.Text.Json.Serialization;

namespace DUSTER.EComm.Data.CommonClass
{
    public abstract class BaseEntity : object
    {
        [JsonIgnore]
        public DateTime created_at { get; set; }
        [JsonIgnore]
        public string created_by { get; set; } = "system";
        [JsonIgnore]
        public DateTime? updated_at { get; set; }
        [JsonIgnore]
        public string? updated_by { get; set; }
    }
}
