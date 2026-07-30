using Ecommerce.Functions.Orchestrations;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Ecommerce.Functions.Triggers;

/// <summary>
/// HTTP trigger to raise external events for the order fulfillment orchestration.
/// </summary>
public static class OrderFulfillmentEventsTrigger
{
    [Function(nameof(NotifyFulfillmentEvent))]
    public static async Task<HttpResponseData> NotifyFulfillmentEvent(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/fulfill/{instanceId}/events/{eventName}")]
        HttpRequestData req,
        string instanceId,
        string eventName,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(OrderFulfillmentEventsTrigger));

        var eventType = ResolveEventType(eventName);
        if (eventType is null)
        {
            var invalidResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await invalidResponse.WriteStringAsync(
                "Invalid event name. Allowed values are ShipmentDispatched, OrderDelivered, shipped, delivered.");
            return invalidResponse;
        }

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var payload = ParsePayload(requestBody);

        try
        {
            await client.RaiseEventAsync(instanceId, eventType, payload);
            logger.LogInformation(
                "Raised fulfillment event {EventType} for instance {InstanceId} with payload {@Payload}",
                eventType,
                instanceId,
                payload);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId,
                eventName = eventType,
                status = "Accepted",
                message = $"Raised {eventType} event for orchestration {instanceId}."
            });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to raise fulfillment event for instance {InstanceId}", instanceId);
            var failedResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await failedResponse.WriteStringAsync("Unable to raise fulfillment event.");
            return failedResponse;
        }
    }

    private static string? ResolveEventType(string eventName)
    {
        return eventName?.Trim().ToLowerInvariant() switch
        {
            "shipmentdispatched" or "shipment-dispatched" or "shipped" => "ShipmentDispatched",
            "orderdelivered" or "order-delivered" or "delivered" => "OrderDelivered",
            _ => null
        };
    }

    private static FulfillmentTrackingPayload ParsePayload(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new FulfillmentTrackingPayload
            {
                EventTimeUtc = DateTime.UtcNow
            };
        }

        try
        {
            var payload = JsonSerializer.Deserialize<FulfillmentTrackingPayload>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload == null)
            {
                return new FulfillmentTrackingPayload
                {
                    EventTimeUtc = DateTime.UtcNow,
                    TrackingNumber = body.Trim()
                };
            }

            payload.EventTimeUtc ??= DateTime.UtcNow;
            return payload;
        }
        catch (JsonException)
        {
            return new FulfillmentTrackingPayload
            {
                EventTimeUtc = DateTime.UtcNow,
                TrackingNumber = body.Trim()
            };
        }
    }
}
