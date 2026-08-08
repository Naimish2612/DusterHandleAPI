using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DUSTER.EComm.Data.Helpers.CacheMemory.Models;


namespace DUSTER.EComm.Data.Helpers.CacheMemory
{
    public class DistributedCacheWrapper : IDistributedCacheWrapper
    {
        private readonly IDistributedCache _distributedCache;

        public DistributedCacheWrapper() { }

        public DistributedCacheWrapper(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task<T> SetAsync<T>(string key, T getDataFunc, DistributedCacheEntryOptions options)
        {
            var newData = getDataFunc;

            var serializedData = JsonSerializer.Serialize(newData);

            await _distributedCache.SetStringAsync(key, serializedData, options);

            return newData;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var cachedData = await _distributedCache.GetStringAsync(key);

            if (cachedData == null)
                return default(T);

            return JsonSerializer.Deserialize<T>(cachedData);

        }

        public async Task<FreeActions> GetLatestFreeActions()
        {
            try
            {
                var freeActions = await _distributedCache.GetStringAsync("free_actions");

                if (freeActions != null)
                {
                    return JsonSerializer.Deserialize<FreeActions>(freeActions);
                }

                return new FreeActions();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task RemoveAsync(string key)
        {
            return _distributedCache.RemoveAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _distributedCache.GetAsync(key) != null;
        }

    }
}
