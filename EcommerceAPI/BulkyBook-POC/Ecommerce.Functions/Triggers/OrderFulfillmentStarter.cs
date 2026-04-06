using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Ecommerce.Functions.Triggers;

/// <summary>
/// HTTP trigger to start the Order Fulfillment Durable Function orchestrator.
/// This can be called by the Order Service when a new order is placed.
/// </summary>
public static class OrderFulfillmentStarter
{
    [Function(nameof(StartOrderFulfillment))]
    public static async Task<HttpResponseData> StartOrderFulfillment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/fulfill")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(OrderFulfillmentStarter));

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<Orchestrations.OrderFulfillmentInput>(requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (input == null || input.OrderId == Guid.Empty)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid order fulfillment request.");
            return badResponse;
        }

        logger.LogInformation("Starting order fulfillment orchestration for OrderId: {OrderId}", input.OrderId);

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestrations.OrderFulfillmentOrchestrator.RunOrderFulfillment),
            input);

        logger.LogInformation("Orchestration started with InstanceId: {InstanceId}", instanceId);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            instanceId,
            statusQueryUrl = $"/runtime/webhooks/durabletask/instances/{instanceId}",
            message = $"Order fulfillment orchestration started for order {input.OrderId}"
        });

        return response;
    }
}
