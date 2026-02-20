using Ecommerce.Application.Common.Services;
using Ecommerce.Application.Features.Orders.Handlers;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ecommerce.Tests.Application.Features.Orders
{
    public class GetMyOrdersQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _mockUserContextService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetMyOrdersQueryHandler _handler;

        public GetMyOrdersQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockUserContextService = new Mock<IUserContextService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetMyOrdersQueryHandler(
                _context, 
                _mockUserContextService.Object, 
                _mockCacheService.Object, 
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_ShouldReturnUserOrders()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Test Product", 100.00m);
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem 
                    { 
                        ProductId = 1, 
                        Quantity = 2, 
                        Product = product 
                    }
                }
            };

            _context.Products.Add(product);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(order.Id);
            result[0].CustomerName.Should().Be("Test Customer");
            result[0].Phone.Should().Be("1234567890");
            result[0].ShippingAddress.Should().Be("123 Main St");
            result[0].Status.Should().Be("Pending");
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].ProductId.Should().Be(1);
            result[0].Items[0].ProductName.Should().Be("Test Product");
            result[0].Items[0].Price.Should().Be(100.00m);
            result[0].Items[0].Quantity.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithNullUserId_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns((long?)null);

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNoOrders_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithMultipleOrders_ShouldReturnAllUserOrders()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            
            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Customer 1",
                    Phone = "111-111-1111",
                    ShippingAddress = "Address 1",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Status = "Pending",
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
                    Phone = "222-222-2222",
                    ShippingAddress = "Address 2",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Status = "Shipped",
                    UserId = 1,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, Quantity = 2, Product = product }
                    }
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldOnlyReturnCurrentUserOrders()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Product 1", 100.00m);
            _context.Products.Add(product);
            
            var user1Order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "User 1",
                Phone = "111-111-1111",
                ShippingAddress = "Address 1",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                }
            };

            var user2Order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "User 2",
                Phone = "222-222-2222",
                ShippingAddress = "Address 2",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 2,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                }
            };

            _context.Orders.AddRange(user1Order, user2Order);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].CustomerName.Should().Be("User 1");
        }

        [Fact]
        public async Task Handle_WithOrderContainingMultipleItems_ShouldMapAllItems()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product1 = CreateProduct(1, "Product 1", 100.00m, "/images/product1.jpg");
            var product2 = CreateProduct(2, "Product 2", 200.00m, "/images/product2.jpg");
            _context.Products.AddRange(product1, product2);
            
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 2, Product = product1 },
                    new OrderItem { ProductId = 2, Quantity = 3, Product = product2 }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Items.Should().HaveCount(2);
            result[0].Items.First(i => i.ProductId == 1).ProductName.Should().Be("Product 1");
            result[0].Items.First(i => i.ProductId == 1).Price.Should().Be(100.00m);
            result[0].Items.First(i => i.ProductId == 1).Quantity.Should().Be(2);
            result[0].Items.First(i => i.ProductId == 1).ImageUrl.Should().Be("/images/product1.jpg");
            
            result[0].Items.First(i => i.ProductId == 2).ProductName.Should().Be("Product 2");
            result[0].Items.First(i => i.ProductId == 2).Price.Should().Be(200.00m);
            result[0].Items.First(i => i.ProductId == 2).Quantity.Should().Be(3);
            result[0].Items.First(i => i.ProductId == 2).ImageUrl.Should().Be("/images/product2.jpg");
        }

        [Fact]
        public async Task Handle_WithNullStatus_ShouldDefaultToPending()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Test Product", 100.00m);
            // Note: Order entity has Status as required, so we test with empty string
            // which the handler treats as null/empty and defaults to "Pending"
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = string.Empty, // Empty string should default to "Pending" in handler
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                }
            };

            _context.Products.Add(product);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Status.Should().Be("Pending");
        }

        [Fact]
        public async Task Handle_WithDifferentStatuses_ShouldReturnCorrectStatus()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Test Product", 100.00m);
            _context.Products.Add(product);
            
            var statuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            var orders = statuses.Select((status, index) => new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = $"Customer {status}",
                Phone = $"123-456-789{index}",
                ShippingAddress = $"Address {index}",
                CreatedAt = DateTime.UtcNow.AddDays(-index),
                Status = status,
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 1, Product = product }
                }
            }).ToList();

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Select(o => o.Status).Should().Contain(statuses);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetMyOrdersQuery();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldMapAllOrderProperties()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = CreateProduct(1, "Test Product", 100.00m);
            var createdAt = DateTime.UtcNow.AddDays(-10);
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                Phone = "555-123-4567",
                ShippingAddress = "456 Oak Street",
                CreatedAt = createdAt,
                Status = "Shipped",
                UserId = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = 5, Product = product }
                }
            };

            _context.Products.Add(product);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetMyOrdersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var orderDto = result[0];
            orderDto.Id.Should().Be(order.Id);
            orderDto.CustomerName.Should().Be("John Doe");
            orderDto.Phone.Should().Be("555-123-4567");
            orderDto.ShippingAddress.Should().Be("456 Oak Street");
            orderDto.Status.Should().Be("Shipped");
            orderDto.CreatedAt.Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
        }       

        private Product CreateProduct(int productId, string name, decimal price, string imageUrl = "")
        {
            return new Product
            {
                ProductId = productId,
                Name = name,
                Description = $"Description for {name}",
                Price = price,
                Stock = 100,
                ImageUrl = imageUrl,
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

