using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ecommerce.Infrastructure.Services;

/// <summary>
/// Service for publishing domain events to Azure Event Grid.
/// Supports ProductUpdated, OrderPlaced, OrderShipped, and OrderDelivered events.
/// </summary>
public interface IEventGridPublisherService
{
    Task PublishProductUpdatedAsync(int productId, string productName, string changeType);
    Task PublishOrderEventAsync(Guid orderId, string userId, string eventType, object? additionalData = null);
}

public class EventGridPublisherService : IEventGridPublisherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EventGridPublisherService> _logger;
    private readonly string? _topicEndpoint;
    private readonly string? _topicKey;

    public EventGridPublisherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EventGridPublisherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _topicEndpoint = configuration["EventGrid:TopicEndpoint"];
        _topicKey = configuration["EventGrid:TopicKey"];
    }

    public async Task PublishProductUpdatedAsync(int productId, string productName, string changeType)
    {
        var eventData = new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            Subject = $"products/{productId}",
            EventType = $"Ecommerce.Product.{changeType}",
            EventTime = DateTime.UtcNow,
            DataVersion = "1.0",
            Data = new { ProductId = productId, ProductName = productName, ChangeType = changeType }
        };

        await PublishEventAsync(eventData);
    }

    public async Task PublishOrderEventAsync(Guid orderId, string userId, string eventType, object? additionalData = null)
    {
        var eventData = new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            Subject = $"orders/{orderId}",
            EventType = $"Ecommerce.Order.{eventType}",
            EventTime = DateTime.UtcNow,
            DataVersion = "1.0",
            Data = new { OrderId = orderId, UserId = userId, EventType = eventType, AdditionalData = additionalData }
        };

        await PublishEventAsync(eventData);
    }

    private async Task PublishEventAsync(EventGridEvent eventData)
    {
        if (string.IsNullOrEmpty(_topicEndpoint) || string.IsNullOrEmpty(_topicKey))
        {
            _logger.LogWarning(
                "Event Grid not configured. Skipping event publish: {EventType} for {Subject}",
                eventData.EventType, eventData.Subject);
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _topicEndpoint)
            {
                Content = JsonContent.Create(new[] { eventData })
            };
            request.Headers.Add("aeg-sas-key", _topicKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Event published to Event Grid: {EventType} for {Subject}",
                eventData.EventType, eventData.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event to Event Grid: {EventType} for {Subject}",
                eventData.EventType, eventData.Subject);
        }
    }
}

internal class EventGridEvent
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string DataVersion { get; set; } = "1.0";
    public object? Data { get; set; }
}
