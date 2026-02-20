using Ecommerce.Application.Features.Admin.Handlers;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Tests.Application.Features.Admin
{
    public class GetRevenueSummaryQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly GetRevenueSummaryQueryHandler _handler;

        public GetRevenueSummaryQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new GetRevenueSummaryQueryHandler(_context);
        }

        [Fact]
        public async Task Handle_With7DaysPeriod_ShouldReturnRevenueSummary()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "7days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
            result.AverageOrderValue.Should().Be(200.00m);
            result.RevenueGrowth.Should().BeGreaterOrEqualTo(0);
            result.OrderGrowth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task Handle_With30DaysPeriod_ShouldReturnRevenueSummary()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 50.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 1",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    Status = "Completed",
                    UserId = 1,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                    }
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 2",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Status = "Completed",
                    UserId = 2,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 3, Product = product }
                    }
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(250.00m); // (50 * 2) + (50 * 3)
            result.TotalOrders.Should().Be(2);
            result.AverageOrderValue.Should().Be(125.00m);
        }

        [Fact]
        public async Task Handle_With90DaysPeriod_ShouldReturnRevenueSummary()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "90days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
            result.AverageOrderValue.Should().Be(200.00m);
        }

        [Fact]
        public async Task Handle_WithNoCurrentOrders_ShouldReturnZeroRevenue()
        {
            // Arrange
            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(0);
            result.TotalOrders.Should().Be(0);
            result.AverageOrderValue.Should().Be(0);
            result.RevenueGrowth.Should().Be(0);
            result.OrderGrowth.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithGrowth_ShouldCalculateGrowthCorrectly()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Previous period order (30-60 days ago)
            var previousOrder = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Previous Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                }
            };

            // Current period order (0-30 days ago)
            var currentOrder = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Current Customer",
                Phone = "0987654321",
                ShippingAddress = "456 Oak Ave",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Status = "Completed",
                UserId = 2,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.AddRange(previousOrder, currentOrder);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(200.00m); // Current: 2 * 100
            result.TotalOrders.Should().Be(1); // Only current period
            result.AverageOrderValue.Should().Be(200.00m);
            // Revenue growth: (200 - 100) / 100 * 100 = 100%
            result.RevenueGrowth.Should().Be(100.00m);
        }

        [Fact]
        public async Task Handle_WithDefaultPeriod_ShouldUse30Days()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "invalid" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldSetLastUpdated()
        {
            // Arrange
            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_WithMultipleProducts_ShouldCalculateTotalRevenue()
        {
            // Arrange
            var product1 = CreateProduct(1, "Product 1", 100.00m);
            var product2 = CreateProduct(2, "Product 2", 50.00m);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product1 },
                    new OrderItem { ProductId = 2, Quantity = 3, Product = product2 }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueSummaryQuery { Period = "30days" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(350.00m); // (100 * 2) + (50 * 3)
            result.TotalOrders.Should().Be(1);
            result.AverageOrderValue.Should().Be(350.00m);
        }

        private Product CreateProduct(int productId, string name, decimal price)
        {
            return new Product
            {
                ProductId = productId,
                Name = name,
                Description = $"Description for {name}",
                Price = price,
                Stock = 100,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

