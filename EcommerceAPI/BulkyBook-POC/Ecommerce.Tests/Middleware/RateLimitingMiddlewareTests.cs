using System.Globalization;
using System.Text.Json;
using Ecommerce.API.Middleware;
using Ecommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WritesUnixResetTimestampAndIsoCompanionHeader()
    {
        var resetTime = DateTime.UtcNow.AddMinutes(1);
        var service = new Mock<IRateLimitingService>();
        service.Setup(x => x.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = true,
                Limit = 100,
                Remaining = 99,
                ResetTime = resetTime
            });
        var nextWasCalled = false;
        var middleware = new RateLimitingMiddleware(
            _ =>
            {
                nextWasCalled = true;
                return Task.CompletedTask;
            },
            service.Object,
            Mock.Of<ILogger<RateLimitingMiddleware>>());
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextWasCalled.Should().BeTrue();
        var resetHeader = context.Response.Headers["X-RateLimit-Reset"].ToString();
        long.TryParse(resetHeader, NumberStyles.None, CultureInfo.InvariantCulture, out var resetTimestamp)
            .Should().BeTrue();
        resetTimestamp.Should().Be(new DateTimeOffset(resetTime).ToUnixTimeSeconds());
        context.Response.Headers["X-RateLimit-Reset-At"].ToString()
            .Should().Be(resetTime.ToString("O", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task InvokeAsync_WhenBlocked_UsesSameRetryAfterInHeaderAndBody()
    {
        var service = new Mock<IRateLimitingService>();
        service.Setup(x => x.CheckRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = false,
                Limit = 5,
                Remaining = 0,
                ResetTime = DateTime.UtcNow.AddSeconds(30)
            });
        var middleware = new RateLimitingMiddleware(
            _ => Task.CompletedTask,
            service.Object,
            Mock.Of<ILogger<RateLimitingMiddleware>>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var bodyRetryAfter = document.RootElement.GetProperty("retryAfter").GetInt32();
        var headerRetryAfter = int.Parse(
            context.Response.Headers.RetryAfter.ToString(),
            CultureInfo.InvariantCulture);
        bodyRetryAfter.Should().Be(headerRetryAfter);
        bodyRetryAfter.Should().BeGreaterThan(0);
    }
}
