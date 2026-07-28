using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ecommerce.API.Security;

public sealed record OAuthAuthorizationGrant(
    string Token,
    long UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? Picture,
    string? ReturnTo,
    DateTimeOffset ExpiresAtUtc);

public interface IOAuthAuthorizationCodeStore
{
    Task<string> IssueAsync(
        string provider,
        OAuthAuthorizationGrant grant,
        CancellationToken cancellationToken = default);

    Task<OAuthAuthorizationGrant?> ConsumeAsync(
        string provider,
        string code,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores short-lived OAuth completion grants. Redis uses a Lua get-and-delete
/// operation so consumption is atomic across API replicas. When Redis is disabled,
/// the distributed-memory fallback is atomic only inside one API process; deployments
/// with multiple API replicas must enable Redis.
/// </summary>
public sealed class OAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private const int DefaultLifetimeSeconds = 90;
    private const int MinimumLifetimeSeconds = 30;
    private const int MaximumLifetimeSeconds = 300;
    private const string KeyPrefix = "oauth:authorization-grant";
    private const string ConsumeScript =
        "local value = redis.call('GET', KEYS[1]); " +
        "if value then redis.call('DEL', KEYS[1]); end; " +
        "return value;";

    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _lifetime;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _localLocks = new();

    public OAuthAuthorizationCodeStore(
        IDistributedCache cache,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<OAuthAuthorizationCodeStore> logger,
        TimeProvider? timeProvider = null)
    {
        _cache = cache;
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        _timeProvider = timeProvider ?? TimeProvider.System;

        var configuredLifetime = configuration.GetValue(
            "OAuth:AuthorizationCodeLifetimeSeconds",
            DefaultLifetimeSeconds);
        _lifetime = TimeSpan.FromSeconds(Math.Clamp(
            configuredLifetime,
            MinimumLifetimeSeconds,
            MaximumLifetimeSeconds));

        if (_redis is null)
        {
            logger.LogWarning(
                "OAuth authorization grants are using the process-local cache fallback. " +
                "Enable Redis before running more than one API replica.");
        }
    }

    public async Task<string> IssueAsync(
        string provider,
        OAuthAuthorizationGrant grant,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var key = BuildKey(provider, code);
        var expiresAt = _timeProvider.GetUtcNow().Add(_lifetime);
        var storedGrant = grant with { ExpiresAtUtc = expiresAt };

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(storedGrant),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _lifetime
            },
            cancellationToken);

        return code;
    }

    public async Task<OAuthAuthorizationGrant?> ConsumeAsync(
        string provider,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var key = BuildKey(provider, code);
        string? serializedGrant;

        if (_redis is not null)
        {
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                ConsumeScript,
                new RedisKey[] { key });
            serializedGrant = result.IsNull ? null : (string?)result;
        }
        else
        {
            serializedGrant = await ConsumeFromLocalCacheAsync(key, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(serializedGrant))
        {
            return null;
        }

        OAuthAuthorizationGrant? grant;
        try
        {
            grant = JsonSerializer.Deserialize<OAuthAuthorizationGrant>(serializedGrant);
        }
        catch (JsonException)
        {
            return null;
        }

        return grant is not null && grant.ExpiresAtUtc > _timeProvider.GetUtcNow()
            ? grant
            : null;
    }

    private async Task<string?> ConsumeFromLocalCacheAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var gate = _localLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            if (value is not null)
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }

            return value;
        }
        finally
        {
            gate.Release();
            _localLocks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, gate));
        }
    }

    private static string BuildKey(string provider, string code) =>
        $"{KeyPrefix}:{provider.Trim().ToLowerInvariant()}:{code}";
}
