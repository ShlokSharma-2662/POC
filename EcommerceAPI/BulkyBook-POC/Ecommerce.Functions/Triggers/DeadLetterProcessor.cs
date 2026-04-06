using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Functions.Triggers;

public class DeadLetterProcessor
{
    private readonly ILogger<DeadLetterProcessor> _logger;
    private readonly QueueClient _dlqClient;

    public DeadLetterProcessor(
        ILogger<DeadLetterProcessor> logger,
        QueueClient dlqClient)
    {
        _logger = logger;
        _dlqClient = dlqClient;
    }

    [Function(nameof(ProcessDeadLetterQueue))]
    public async Task ProcessDeadLetterQueue(
        [QueueTrigger("%DeadLetterQueueName%")] QueueMessage message,
        FunctionContext context)
    {
        var messageId = message.MessageId;
        var messageText = message.Body.ToString();

        _logger.LogWarning(
            "Processing dead-letter message. MessageId: {MessageId}, DequeueCount: {DequeueCount}",
            messageId,
            message.DequeueCount);

        try
        {
            var eventData = JsonSerializer.Deserialize<DeadLetterEventData>(messageText);

            if (eventData != null)
            {
                _logger.LogWarning(
                    "Dead-letter event details: EventType={EventType}, Subject={Subject}, DeliveryAttempts={Attempts}",
                    eventData.EventType,
                    eventData.Subject,
                    eventData.DeliveryAttempts);

                await HandleDeadLetterAsync(eventData);
            }

            await _dlqClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            _logger.LogInformation("Successfully processed and deleted dead-letter message: {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dead-letter message: {MessageId}", messageId);
            throw;
        }
    }

    private async Task HandleDeadLetterAsync(DeadLetterEventData eventData)
    {
        switch (eventData.EventType)
        {
            case "OrderPlaced":
            case "OrderShipped":
            case "OrderDelivered":
                await HandleOrderEventDeadLetterAsync(eventData);
                break;

            case "ProductUpdated":
                await HandleProductEventDeadLetterAsync(eventData);
                break;

            default:
                _logger.LogWarning("Unhandled dead-letter event type: {EventType}", eventData.EventType);
                break;
        }

        await LogDeadLetterToCosmosAsync(eventData);
    }

    private Task HandleOrderEventDeadLetterAsync(DeadLetterEventData eventData)
    {
        _logger.LogError(
            "Critical: Order event failed after retries. EventType={EventType}, OrderId={OrderId}",
            eventData.EventType,
            eventData.OrderId);

        return Task.CompletedTask;
    }

    private Task HandleProductEventDeadLetterAsync(DeadLetterEventData eventData)
    {
        _logger.LogError(
            "Critical: Product event failed after retries. EventType={EventType}, ProductId={ProductId}",
            eventData.EventType,
            eventData.ProductId);

        return Task.CompletedTask;
    }

    private Task LogDeadLetterToCosmosAsync(DeadLetterEventData eventData)
    {
        _logger.LogInformation(
            "Dead-letter would be logged to CosmosDB: {EventType}, {Error}",
            eventData.EventType,
            eventData.LastError);

        return Task.CompletedTask;
    }
}

public class DeadLetterEventData
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public int DeliveryAttempts { get; set; }
    public string? LastError { get; set; }
    public Guid? OrderId { get; set; }
    public int? ProductId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
