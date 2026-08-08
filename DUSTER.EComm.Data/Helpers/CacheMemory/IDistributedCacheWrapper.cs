using Microsoft.Extensions.Caching.Distributed;
using DUSTER.EComm.Data.Helpers.CacheMemory.Models;

namespace DUSTER.EComm.Data.Helpers.CacheMemory
{
    public interface IDistributedCacheWrapper
    {
        Task<T> SetAsync<T>(string key, T value, DistributedCacheEntryOptions options);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<FreeActions> GetLatestFreeActions();
    }
}
