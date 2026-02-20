using AspNetCoreRateLimit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace Ecommerce.Infrastructure.Services
{
    public interface IRateLimitingService
    {
        Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
        Task<RateLimitResult> CheckRateLimitAsync(string endpoint, string? clientId = null);
    }

    public class RateLimitingService : IRateLimitingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
        private readonly IConnectionMultiplexer? _redis;
        private readonly IDatabase? _redisDatabase;
        private readonly bool _isDistributed;
        private readonly IReadOnlyList<RateLimitRule> _rules;
        private readonly int _defaultLimit;
        private readonly TimeSpan _defaultWindow;

        public RateLimitingService(
            IMemoryCache cache,
            ILogger<RateLimitingService> logger,
            IConnectionMultiplexer? connectionMultiplexer = null,
            IOptions<IpRateLimitOptions>? ipRateLimitOptions = null)
        {
            _cache = cache;
            _logger = logger;
            _redis = connectionMultiplexer;

            if (connectionMultiplexer != null && connectionMultiplexer.IsConnected)
            {
                _redisDatabase = connectionMultiplexer.GetDatabase();
                _isDistributed = true;
                _logger.LogInformation("Rate limiting is using Redis distributed storage.");
            }
            else
            {
                _isDistributed = false;
                if (connectionMultiplexer != null)
                {
                    _logger.LogWarning("Redis connection is not available. Falling back to in-memory rate limiting.");
                }
                else
                {
                    _logger.LogDebug("Redis connection not configured. Using in-memory rate limiting.");
                }
            }

            _rules = ipRateLimitOptions?.Value?.GeneralRules?.ToList() ?? new List<RateLimitRule>();

            var defaultRule = _rules.FirstOrDefault(r => string.Equals(r.Endpoint, "*", StringComparison.OrdinalIgnoreCase));
            if (defaultRule != null)
            {
                var limitValue = defaultRule.Limit > 0 ? defaultRule.Limit : 60;
                _defaultLimit = Convert.ToInt32(Math.Floor(limitValue));
                _defaultWindow = ParsePeriod(defaultRule.Period);
            }
            else
            {
                _defaultLimit = 60;
                _defaultWindow = TimeSpan.FromMinutes(1);
            }
        }

        public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
        {
            var result = await EvaluateRateLimitAsync(key, limit, window);
            return result.IsAllowed;
        }

        public async Task<RateLimitResult> CheckRateLimitAsync(string endpoint, string? clientId = null)
        {
            var key = string.IsNullOrEmpty(clientId) ? endpoint : $"{clientId}:{endpoint}";
            var (limit, window) = GetLimitConfiguration(endpoint);

            var result = await EvaluateRateLimitAsync(key, limit, window);
            return result;
        }

        private async Task<RateLimitResult> EvaluateRateLimitAsync(string key, int limit, TimeSpan window)
        {
            if (_isDistributed && _redisDatabase != null)
            {
                return await EvaluateDistributedAsync(key, limit, window);
            }

            return await EvaluateMemoryAsync(key, limit, window);
        }

        private async Task<RateLimitResult> EvaluateDistributedAsync(string key, int limit, TimeSpan window)
        {
            if (_redisDatabase == null)
            {
                throw new InvalidOperationException("Redis database is not available for distributed rate limiting.");
            }

            var redisKey = GetRedisKey(key);
            var count = await _redisDatabase.StringIncrementAsync(redisKey);

            TimeSpan? ttl;
            if (count == 1)
            {
                await _redisDatabase.KeyExpireAsync(redisKey, window);
                ttl = window;
            }
            else
            {
                ttl = await _redisDatabase.KeyTimeToLiveAsync(redisKey);
                if (!ttl.HasValue || ttl.Value <= TimeSpan.Zero)
                {
                    await _redisDatabase.KeyExpireAsync(redisKey, window);
                    ttl = window;
                }
            }

            var isAllowed = count <= limit;
            var remainingLong = isAllowed ? Math.Max(0, (long)limit - count) : 0;
            var remaining = (int)Math.Min(int.MaxValue, remainingLong);
            var resetTime = DateTime.UtcNow.Add(ttl ?? window);

            if (isAllowed)
            {
                _logger.LogDebug("Distributed rate limit check passed for key: {Key}. Count: {Count}, Limit: {Limit}", key, count, limit);
            }
            else
            {
                _logger.LogWarning("Distributed rate limit exceeded for key: {Key}. Count: {Count}, Limit: {Limit}", key, count, limit);
            }

            return new RateLimitResult
            {
                IsAllowed = isAllowed,
                Limit = limit,
                Remaining = remaining,
                ResetTime = resetTime
            };
        }

        private async Task<RateLimitResult> EvaluateMemoryAsync(string key, int limit, TimeSpan window)
        {
            var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var cacheKey = GetCacheKey(key);
                var current = _cache.GetOrCreate(cacheKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = window;
                    return new RateLimitCounter { Count = 0, ResetTime = DateTime.UtcNow.Add(window) };
                });

                var resetTime = DateTime.UtcNow.Add(window);

                if (current.Count >= limit)
                {
                    _logger.LogWarning("In-memory rate limit exceeded for key: {Key}, Count: {Count}, Limit: {Limit}", key, current.Count, limit);
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        Limit = limit,
                        Remaining = 0,
                        ResetTime = resetTime
                    };
                }

                current.Count++;
                current.ResetTime = resetTime;
                _cache.Set(cacheKey, current, window);

                var remaining = Math.Max(0, limit - current.Count);

                _logger.LogDebug("In-memory rate limit check passed for key: {Key}, Count: {Count}, Limit: {Limit}", key, current.Count, limit);

                return new RateLimitResult
                {
                    IsAllowed = true,
                    Limit = limit,
                    Remaining = remaining,
                    ResetTime = resetTime
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        private (int Limit, TimeSpan Window) GetLimitConfiguration(string endpoint)
        {
            foreach (var rule in _rules)
            {
                if (IsRuleMatch(rule.Endpoint, endpoint))
                {
                    var window = ParsePeriod(rule.Period);
                    var limitValue = rule.Limit > 0 ? rule.Limit : _defaultLimit;
                    var limit = Convert.ToInt32(Math.Floor(limitValue));
                    return (limit, window);
                }
            }

            return (_defaultLimit, _defaultWindow);
        }

        private static bool IsRuleMatch(string ruleEndpoint, string actualEndpoint)
        {
            if (string.IsNullOrWhiteSpace(ruleEndpoint) || ruleEndpoint == "*")
            {
                return true;
            }

            if (ruleEndpoint.Contains('*'))
            {
                var prefix = ruleEndpoint.TrimEnd('*');
                return actualEndpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(ruleEndpoint, actualEndpoint, StringComparison.OrdinalIgnoreCase);
        }

        private static TimeSpan ParsePeriod(string? period)
        {
            if (string.IsNullOrWhiteSpace(period))
            {
                return TimeSpan.FromMinutes(1);
            }

            period = period.Trim();
            var unit = period[^1];
            var valuePart = period[..^1];

            if (!double.TryParse(valuePart, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return TimeSpan.FromMinutes(1);
            }

            return unit switch
            {
                's' or 'S' => TimeSpan.FromSeconds(value),
                'm' or 'M' => TimeSpan.FromMinutes(value),
                'h' or 'H' => TimeSpan.FromHours(value),
                'd' or 'D' => TimeSpan.FromDays(value),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        private static string GetCacheKey(string key) => $"rate_limit_{key}";

        private static string GetRedisKey(string key) => $"rate_limit:{key}";
    }

    public class RateLimitCounter
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }

    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTime ResetTime { get; set; }
    }
}

