using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Ecommerce.Functions.Triggers;

public class ProductUpdatedProcessor
{
    private readonly IConnectionMultiplexer? _redisConnection;
    private readonly ILogger<ProductUpdatedProcessor> _logger;

    public ProductUpdatedProcessor(
        IConnectionMultiplexer? redisConnection,
        ILogger<ProductUpdatedProcessor> logger)
    {
        _redisConnection = redisConnection;
        _logger = logger;
    }

    [Function(nameof(HandleProductUpdated))]
    public async Task HandleProductUpdated(
        [EventGridTrigger] EventGridEventData eventGridEvent,
        FunctionContext executionContext)
    {
        _logger.LogInformation(
            "ProductUpdated event received: Subject={Subject}, EventType={EventType}, EventTime={EventTime}",
            eventGridEvent.Subject,
            eventGridEvent.EventType,
            eventGridEvent.EventTime);

        try
        {
            var productId = ExtractProductId(eventGridEvent);
            var eventType = eventGridEvent.EventType;

            if (productId.HasValue)
            {
                await InvalidateProductCacheAsync(productId.Value);
                _logger.LogInformation("Cache invalidated for product ID: {ProductId}", productId.Value);
            }

            if (eventType?.EndsWith("Deleted", StringComparison.OrdinalIgnoreCase) == true)
            {
                await InvalidateProductListCachesAsync();
                _logger.LogInformation("Product list caches invalidated due to deletion");
            }

            _logger.LogInformation("ProductUpdated event processed successfully for: {Subject}", eventGridEvent.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProductUpdated event for: {Subject}", eventGridEvent.Subject);
            throw;
        }
    }

    private async Task InvalidateProductCacheAsync(int productId)
    {
        if (_redisConnection == null || !_redisConnection.IsConnected)
        {
            _logger.LogWarning("Redis connection unavailable, skipping cache invalidation");
            return;
        }

        var database = _redisConnection.GetDatabase();
        var cacheKey = $"product:{productId}";
        await database.KeyDeleteAsync(cacheKey);
    }

    private async Task InvalidateProductListCachesAsync()
    {
        if (_redisConnection == null || !_redisConnection.IsConnected)
        {
            _logger.LogWarning("Redis connection unavailable, skipping list cache invalidation");
            return;
        }

        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        var keys = server.Keys(pattern: "products:*").ToArray();

        if (keys.Length > 0)
        {
            var database = _redisConnection.GetDatabase();
            var tasks = keys.Select(key => database.KeyDeleteAsync(key));
            await Task.WhenAll(tasks);
            _logger.LogInformation("Invalidated {Count} product list cache keys", keys.Length);
        }
    }

    private int? ExtractProductId(EventGridEventData eventGridEvent)
    {
        try
        {
            if (eventGridEvent.Data is JsonElement dataElement)
            {
                if (dataElement.TryGetProperty("productId", out var productIdElement))
                {
                    return productIdElement.GetInt32();
                }
            }

            var subjectParts = eventGridEvent.Subject.Split('/');
            foreach (var part in subjectParts)
            {
                if (int.TryParse(part, out var id))
                {
                    return id;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract product ID from event");
        }

        return null;
    }
}

public class EventGridEventData
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public object? Data { get; set; }
}
