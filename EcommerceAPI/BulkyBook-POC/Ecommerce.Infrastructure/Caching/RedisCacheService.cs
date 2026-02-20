using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;

namespace Ecommerce.Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer? _connectionMultiplexer;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer? connectionMultiplexer,
            IConfiguration configuration)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_isCacheEnabled != "true" || _connectionMultiplexer == null)
                return null;

            try
            {
                // Check if Redis connection is healthy
                if (!_connectionMultiplexer.IsConnected)
                    return null;

                var cachedData = await _distributedCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                    return null;

                return JsonConvert.DeserializeObject<T>(cachedData);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache retrieval failure should not affect application functionality
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (_isCacheEnabled != "true" || _connectionMultiplexer == null)
                return;

            try
            {
                // Check if Redis connection is healthy
                if (!_connectionMultiplexer.IsConnected)
                    return;

                var serializedData = JsonConvert.SerializeObject(value);
                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Use default expiration from configuration
                    var defaultExpirationMinutes = _configuration.GetValue<int>("Redis:DefaultExpirationMinutes", 30);
                    options.SetAbsoluteExpiration(TimeSpan.FromMinutes(defaultExpirationMinutes));
                }

                await _distributedCache.SetStringAsync(key, serializedData, options);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache storage failure should not affect application functionality
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (_isCacheEnabled != "true" || _connectionMultiplexer == null)
                return;

            try
            {
                // Check if Redis connection is healthy
                if (!_connectionMultiplexer.IsConnected)
                    return;

                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache removal failure should not affect application functionality
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            if (_isCacheEnabled != "true" || _connectionMultiplexer == null)
                return;

            try
            {
                // Check if Redis connection is healthy
                if (!_connectionMultiplexer.IsConnected)
                    return;

                var database = _connectionMultiplexer.GetDatabase();
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                
                var keys = server.Keys(pattern: pattern);
                var tasks = keys.Select(key => database.KeyDeleteAsync(key));
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking the application
                // Cache pattern removal failure should not affect application functionality
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (_isCacheEnabled != "true" || _connectionMultiplexer == null)
                return false;

            try
            {
                // Check if Redis connection is healthy
                if (!_connectionMultiplexer.IsConnected)
                    return false;

                var cachedData = await _distributedCache.GetStringAsync(key);
                return !string.IsNullOrEmpty(cachedData);
            }
            catch (Exception)
            {
                // Cache existence check failure should not affect application functionality
                return false;
            }
        }
    }
}
