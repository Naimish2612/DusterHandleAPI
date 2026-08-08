namespace DUSTER.EComm.Data.Helpers.CacheMemory
{
    public interface IPostgresDistributedCache
    {
        Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);
        Task SetObjectAsync<T>(string key, T obj, TimeSpan? expiry = null);
        Task<T?> GetObjectAsync<T>(string key);
        Task RemoveAsync(string key);
        Task<bool> HasKey(string key);
    }
}
