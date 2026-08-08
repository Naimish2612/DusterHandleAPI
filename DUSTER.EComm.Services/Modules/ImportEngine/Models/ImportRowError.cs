namespace DUSTER.EComm.Services.Modules.ImportEngine.Models
{
    public class ImportRowError
    {
        public Dictionary<string, string> OriginalRow { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
