using Ecommerce.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Middleware;

public class RequestTimingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrows_PropagatesTheOriginalException()
    {
        var expected = new InvalidOperationException("downstream failed");
        var middleware = new RequestTimingMiddleware(
            _ => Task.FromException(expected),
            Mock.Of<ILogger<RequestTimingMiddleware>>(),
            Mock.Of<IServiceProvider>());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(new DefaultHttpContext()));

        thrown.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task InvokeAsync_WhenMetricPersistenceFails_PreservesSuccessfulResponse()
    {
        var logger = new Mock<ILogger<RequestTimingMiddleware>>();
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new RequestTimingMiddleware(
            async httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status201Created;
                await httpContext.Response.WriteAsync("created");
            },
            logger.Object,
            serviceProvider);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        (await reader.ReadToEndAsync()).Should().Be("created");
        logger.Verify(
            entry => entry.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString()!.Contains("Error logging request metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
