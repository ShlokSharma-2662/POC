using Ecommerce.Application.Features.Admin.Handlers;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Tests.Application.Features.Admin
{
    public class GetRevenueReportQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly GetRevenueReportQueryHandler _handler;

        public GetRevenueReportQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new GetRevenueReportQueryHandler(_context);
        }

        [Fact]
        public async Task Handle_With15DayReport_ShouldReturnRevenueReport()
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
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "15day" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ReportType.Should().Be("15day");
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
            result.Periods.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_WithMonthlyReport_ShouldReturnRevenueReport()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 50.00m);
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
                    new OrderItem { ProductId = 1, Quantity = 3, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "monthly" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ReportType.Should().Be("monthly");
            result.TotalRevenue.Should().Be(150.00m);
            result.TotalOrders.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithCustomDateRange_ShouldReturnRevenueReport()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 75.00m);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var startDate = DateTime.UtcNow.AddDays(-20);
            var endDate = DateTime.UtcNow.AddDays(-10);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 4, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery 
            { 
                ReportType = "custom",
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ReportStartDate.Should().BeCloseTo(startDate, TimeSpan.FromSeconds(1));
            result.ReportEndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1));
            result.TotalRevenue.Should().Be(300.00m);
            result.TotalOrders.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithNoOrders_ShouldReturnZeroRevenue()
        {
            // Arrange
            var query = new GetRevenueReportQuery { ReportType = "15day" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(0);
            result.TotalOrders.Should().Be(0);
            result.Periods.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_WithMultipleOrders_ShouldCalculateTotalRevenue()
        {
            // Arrange
            var product1 = CreateProduct(1, "Product 1", 100.00m);
            var product2 = CreateProduct(2, "Product 2", 50.00m);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 1",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Status = "Completed",
                    UserId = 1,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 2, Product = product1 }
                    }
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 2",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    Status = "Completed",
                    UserId = 2,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 2, Quantity = 3, Product = product2 }
                    }
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "15day" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(350.00m); // (100 * 2) + (50 * 3)
            result.TotalOrders.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithOrdersOutsideDateRange_ShouldExcludeThem()
        {
            // Arrange
            var product = CreateProduct(1, "Product 1", 100.00m);
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
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Status = "Completed",
                    UserId = 1,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                    }
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 2",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddDays(-20), // Outside 15 day range
                    Status = "Completed",
                    UserId = 2,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                    }
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "15day" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalRevenue.Should().Be(100.00m); // Only first order
            result.TotalOrders.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetRevenueReportQuery { ReportType = "15day" };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_With6MonthsReport_ShouldReturnRevenueReport()
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
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "6months" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ReportType.Should().Be("6months");
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
            result.Periods.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_With12MonthsReport_ShouldReturnRevenueReport()
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
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                Status = "Completed",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetRevenueReportQuery { ReportType = "12months" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.ReportType.Should().Be("12months");
            result.TotalRevenue.Should().Be(200.00m);
            result.TotalOrders.Should().Be(1);
            result.Periods.Should().NotBeEmpty();
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







