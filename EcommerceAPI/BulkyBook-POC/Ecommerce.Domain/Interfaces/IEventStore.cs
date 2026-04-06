using Ecommerce.Domain.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Domain.Interfaces;

public interface IEventStore
{
    Task<IEnumerable<OrderEvent>> GetEventsForOrderAsync(Guid orderId);
    Task AppendEventAsync(OrderEvent orderEvent);
    Task<IEnumerable<OrderEvent>> GetEventsByTypeAsync(string eventType, DateTime from, DateTime to);
}

public class CosmosEventStore : IEventStore
{
    private readonly Container _container;
    private readonly ILogger<CosmosEventStore> _logger;

    public CosmosEventStore(
        CosmosClient cosmosClient,
        IConfiguration configuration,
        ILogger<CosmosEventStore> logger)
    {
        var databaseName = configuration["CosmosDB:DatabaseName"] ?? "EcommerceEvents";
        var containerName = configuration["CosmosDB:OrderEventsContainer"] ?? "OrderEvents";
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public async Task<IEnumerable<OrderEvent>> GetEventsForOrderAsync(Guid orderId)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.OrderId = @orderId ORDER BY c.EventTime ASC")
                .WithParameter("@orderId", orderId.ToString());

            var iterator = _container.GetItemQueryIterator<OrderEvent>(query);
            var events = new List<OrderEvent>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                events.AddRange(response);
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task AppendEventAsync(OrderEvent orderEvent)
    {
        try
        {
            orderEvent.Id = Guid.NewGuid().ToString();
            var response = await _container.CreateItemAsync(orderEvent, new PartitionKey(orderEvent.OrderId.ToString()));
            _logger.LogInformation(
                "Event {EventType} appended for order {OrderId}. RU consumed: {RU}",
                orderEvent.EventType,
                orderEvent.OrderId,
                response.RequestCharge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending event {EventType} for order {OrderId}",
                orderEvent.EventType, orderEvent.OrderId);
            throw;
        }
    }

    public async Task<IEnumerable<OrderEvent>> GetEventsByTypeAsync(string eventType, DateTime from, DateTime to)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.EventType = @eventType AND c.EventTime >= @from AND c.EventTime <= @to ORDER BY c.EventTime ASC")
                .WithParameter("@eventType", eventType)
                .WithParameter("@from", from)
                .WithParameter("@to", to);

            var iterator = _container.GetItemQueryIterator<OrderEvent>(query);
            var events = new List<OrderEvent>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                events.AddRange(response);
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events of type {EventType}", eventType);
            throw;
        }
    }
}

public class NullEventStore : IEventStore
{
    private readonly ILogger<NullEventStore> _logger;

    public NullEventStore(ILogger<NullEventStore> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<OrderEvent>> GetEventsForOrderAsync(Guid orderId)
    {
        _logger.LogDebug("Event store disabled - returning empty events for order {OrderId}", orderId);
        return Task.FromResult<IEnumerable<OrderEvent>>(Array.Empty<OrderEvent>());
    }

    public Task AppendEventAsync(OrderEvent orderEvent)
    {
        _logger.LogDebug("Event store disabled - event {EventType} for order {OrderId} would have been persisted",
            orderEvent.EventType, orderEvent.OrderId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<OrderEvent>> GetEventsByTypeAsync(string eventType, DateTime from, DateTime to)
    {
        return Task.FromResult<IEnumerable<OrderEvent>>(Array.Empty<OrderEvent>());
    }
}
