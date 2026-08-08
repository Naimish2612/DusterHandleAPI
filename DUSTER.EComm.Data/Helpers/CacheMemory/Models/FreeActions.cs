namespace DUSTER.EComm.Data.Helpers.CacheMemory.Models
{
    public class FreeActions
    {
        public FreeActions()
        {
            free_action_url = new List<string>();
        }

        public List<string> free_action_url { get; set; }
        public DateTime created_date { get; set; } = DateTime.Now;
    }
}
