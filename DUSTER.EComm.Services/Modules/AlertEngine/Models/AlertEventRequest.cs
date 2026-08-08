using System.Text.Json;

namespace DUSTER.EComm.Services.Modules.AlertEngine.Models
{
    public class AlertEventRequest
    {
        public string event_code { get; set; }
        public string? to_email { get; set; }
        public string? to_name { get; set; }
        public object payload { get; set; }
    }
}
