using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Handlers;
using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ecommerce.Tests.Application.Features.Admin
{
    public class GetAllOrdersForAdminQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetAllOrdersForAdminQueryHandler _handler;

        public GetAllOrdersForAdminQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup configuration to return cache disabled by default
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetAllOrdersForAdminQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithNoFilters_ShouldReturnAllOrders()
        {
            // Arrange
            var product1 = new Product
            {
                ProductId = 1,
                Name = "Test Product 1",
                Description = "Description 1",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            var product2 = new Product
            {
                ProductId = 2,
                Name = "Test Product 2",
                Description = "Description 2",
                Price = 200.00m,
                Stock = 5,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.AddRange(product1, product2);

            var order1 = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = "Pending",
                UserId = 1
            };

            var order2 = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Jane Smith",
                Phone = "0987654321",
                ShippingAddress = "456 Oak Ave",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Status = "Completed",
                UserId = 2
            };

            _context.Orders.AddRange(order1, order2);

            var orderItem1 = new OrderItem
            {
                Id = 1,
                ProductId = 1,
                Quantity = 2,
                OrderId = order1.Id,
                Product = product1
            };

            var orderItem2 = new OrderItem
            {
                Id = 2,
                ProductId = 2,
                Quantity = 1,
                OrderId = order2.Id,
                Product = product2
            };

            _context.OrderItems.AddRange(orderItem1, orderItem2);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().BeInDescendingOrder(x => x.CreatedAt);
            
            var firstOrder = result.Items.First();
            firstOrder.CustomerName.Should().Be("John Doe");
            firstOrder.Items.Should().HaveCount(1);
            firstOrder.Items.First().ProductName.Should().Be("Test Product 1");
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnFilteredOrders()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Doe",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Status = "Pending",
                    UserId = 1
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Jane Smith",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Status = "Completed",
                    UserId = 2
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Bob Johnson",
                    Phone = "5555555555",
                    ShippingAddress = "789 Pine St",
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    Status = "Pending",
                    UserId = 3
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                Status = "Pending" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(x => x.Status == "Pending");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnMatchingOrders()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Doe",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Status = "Pending",
                    UserId = 1
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Jane Smith",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Status = "Completed",
                    UserId = 2
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = "john" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().CustomerName.Should().Be("John Doe");
        }

        [Fact]
        public async Task Handle_WithSearchTermInPhone_ShouldReturnMatchingOrders()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Doe",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Status = "Pending",
                    UserId = 1
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Jane Smith",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Status = "Completed",
                    UserId = 2
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = "123456" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Phone.Should().Be("1234567890");
        }

        [Fact]
        public async Task Handle_WithSearchTermInOrderId_ShouldReturnMatchingOrders()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerName = "John Doe",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = "Pending",
                UserId = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = orderId.ToString().Substring(0, 8) // Search by partial GUID
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Id.Should().Be(orderId);
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orders = Enumerable.Range(1, 25).Select(i => new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = $"Customer {i}",
                Phone = $"123456789{i:D1}",
                ShippingAddress = $"{i} Main St",
                CreatedAt = DateTime.UtcNow.AddHours(-i),
                Status = "Pending",
                UserId = i
            }).ToList();

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery { PageNumber = 2, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
            result.Items.First().CustomerName.Should().Be("Customer 11"); // Second page should start from item 11
        }

        [Fact]
        public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyResult()
        {
            // Arrange
            var query = new GetAllOrdersForAdminQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }


        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "Test Customer",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetAllOrdersForAdminQuery { PageNumber = 1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_WithCombinedFilters_ShouldReturnCorrectResults()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.Add(product);

            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Doe",
                    Phone = "1234567890",
                    ShippingAddress = "123 Main St",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Status = "Pending",
                    UserId = 1
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "Jane Smith",
                    Phone = "0987654321",
                    ShippingAddress = "456 Oak Ave",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Status = "Completed",
                    UserId = 2
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Johnson",
                    Phone = "5555555555",
                    ShippingAddress = "789 Pine St",
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    Status = "Pending",
                    UserId = 3
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                Status = "Pending",
                SearchTerm = "john"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(x => x.Status == "Pending");
            result.Items.Should().OnlyContain(x => x.CustomerName.ToLower().Contains("john"));
        }

        [Fact]
        public async Task Handle_WithOrderItems_ShouldIncludeProductDetails()
        {
            // Arrange
            var product1 = new Product
            {
                ProductId = 1,
                Name = "Product 1",
                Description = "Description 1",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            var product2 = new Product
            {
                ProductId = 2,
                Name = "Product 2",
                Description = "Description 2",
                Price = 200.00m,
                Stock = 5,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1
            };

            _context.Products.AddRange(product1, product2);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1
            };

            _context.Orders.Add(order);

            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    Id = 1,
                    ProductId = 1,
                    Quantity = 2,
                    OrderId = order.Id,
                    Product = product1
                },
                new OrderItem
                {
                    Id = 2,
                    ProductId = 2,
                    Quantity = 1,
                    OrderId = order.Id,
                    Product = product2
                }
            };

            _context.OrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            
            var orderDto = result.Items.First();
            orderDto.Items.Should().HaveCount(2);
            
            var firstItem = orderDto.Items.First();
            firstItem.ProductName.Should().Be("Product 1");
            firstItem.Description.Should().Be("Description 1");
            firstItem.Price.Should().Be(100.00m);
            firstItem.Quantity.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithNullProduct_ShouldHandleGracefully()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = "John Doe",
                Phone = "1234567890",
                ShippingAddress = "123 Main St",
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                UserId = 1
            };

            _context.Orders.Add(order);

            var orderItem = new OrderItem
            {
                Id = 1,
                ProductId = 999, // Non-existent product
                Quantity = 2,
                OrderId = order.Id,
                Product = null // Explicitly null
            };

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            var query = new GetAllOrdersForAdminQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            
            var orderDto = result.Items.First();
            orderDto.Items.Should().HaveCount(1);
            
            var item = orderDto.Items.First();
            item.ProductName.Should().Be("Unknown");
            item.Description.Should().Be("");
            item.Price.Should().Be(0);
            item.Quantity.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
