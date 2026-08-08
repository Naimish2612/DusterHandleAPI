using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DUSTER.EComm.Data.Helpers.CacheMemory
{
    public class PostgresDistributedCache : IPostgresDistributedCache
    {
        private readonly IDistributedCache _cache;

        public PostgresDistributedCache(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
                options.AbsoluteExpirationRelativeToNow = expiry;

            await _cache.SetStringAsync(key, value, options);
        }

        public async Task<string?> GetStringAsync(string key)
        {
            return await _cache.GetStringAsync(key);
        }

        public async Task SetObjectAsync<T>(string key, T obj, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(obj);
            await SetStringAsync(key, json, expiry);
        }

        public async Task<T?> GetObjectAsync<T>(string key)
        {
            var json = await _cache.GetStringAsync(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<bool> HasKey(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return value != null;
        }
    }
}
