using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Application.Features.Orders.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Ecommerce.Tests.Application.Features.Orders
{
    public class CheckoutOrderHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly Mock<ILogger<CheckoutOrderHandler>> _mockLogger;
        private readonly CheckoutOrderHandler _handler;

        public CheckoutOrderHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _mockLogger = new Mock<ILogger<CheckoutOrderHandler>>();
            
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            
            _handler = new CheckoutOrderHandler(
                _context,
                _mockHttpContextAccessor.Object,
                _mockEmailService.Object,
                _mockCacheInvalidationService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldCreateOrder()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10, "/images/product.jpg");
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == result);
            
            order.Should().NotBeNull();
            order!.CustomerName.Should().Be("John Doe");
            order.ShippingAddress.Should().Be("123 Main St");
            order.Phone.Should().Be("1234567890");
            order.UserId.Should().Be(1);
            order.Items.Should().HaveCount(1);
            order.Items[0].ProductId.Should().Be(1);
            order.Items[0].Quantity.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldDecreaseProductStock()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 3 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedProduct = await _context.Products.FindAsync(1);
            updatedProduct.Should().NotBeNull();
            updatedProduct!.Stock.Should().Be(7); // 10 - 3 = 7
        }

        [Fact]
        public async Task Handle_WithMultipleProducts_ShouldDecreaseAllStocks()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product1 = CreateProduct(1, "Product 1", 100.00m, 10);
            var product2 = CreateProduct(2, "Product 2", 200.00m, 5);
            
            _context.Users.Add(user);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 },
                    new OrderItemDto { ProductId = 2, Quantity = 3 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedProduct1 = await _context.Products.FindAsync(1);
            var updatedProduct2 = await _context.Products.FindAsync(2);
            
            updatedProduct1!.Stock.Should().Be(8); // 10 - 2 = 8
            updatedProduct2!.Stock.Should().Be(2); // 5 - 3 = 2
        }

        [Fact]
        public async Task Handle_WithNonExistentProduct_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 999, Quantity = 1 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("Product with ID 999 not found");
        }

        [Fact]
        public async Task Handle_WithInsufficientStock_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 5); // Only 5 in stock
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 10 } // Request 10, only 5 available
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("Insufficient stock");
            exception.Message.Should().Contain("Available: 5");
            exception.Message.Should().Contain("Requested: 10");
        }

        [Fact]
        public async Task Handle_ShouldInvalidateProductCache()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateProductCacheAsync(1),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldInvalidateAllOrdersCache()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateAllOrdersCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSendOrderConfirmationEmail()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10, "/images/product.jpg");
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockEmailService.Verify(
                x => x.SendOrderConfirmationEmailAsync(
                    "user@example.com",
                    "John Doe",
                    It.Is<string>(s => !string.IsNullOrEmpty(s)), // OrderId as string
                    It.IsAny<List<OrderItemEmailDto>>(),
                    200.00m, // 100 * 2
                    "123 Main St",
                    "1234567890"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmailServiceFailure_ShouldStillCompleteOrder()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _mockEmailService
                .Setup(x => x.SendOrderConfirmationEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<OrderItemEmailDto>>(),
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == result);
            order.Should().NotBeNull();
            order!.CustomerName.Should().Be("John Doe");
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldNotSendEmailButStillCreateOrder()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == result);
            order.Should().NotBeNull();
            
            // Email should not be sent if user not found
            _mockEmailService.Verify(
                x => x.SendOrderConfirmationEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<OrderItemEmailDto>>(),
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithMultipleProducts_ShouldCalculateTotalCorrectly()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product1 = CreateProduct(1, "Product 1", 100.00m, 10);
            var product2 = CreateProduct(2, "Product 2", 200.00m, 5);
            
            _context.Users.Add(user);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }, // 100 * 2 = 200
                    new OrderItemDto { ProductId = 2, Quantity = 3 }  // 200 * 3 = 600
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockEmailService.Verify(
                x => x.SendOrderConfirmationEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<OrderItemEmailDto>>(),
                    800.00m, // 200 + 600
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNullUserId_ShouldThrowException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 1 }
                }
            };

            // Act & Assert
            // Handler now checks for null userId and throws UnauthorizedAccessException
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("User ID not found");
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 1 }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldSetOrderCreatedAt()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            
            _context.Users.Add(user);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 1 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == result);
            order.Should().NotBeNull();
            order!.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Handle_ShouldInvalidateCacheForAllProductsInOrder()
        {
            // Arrange
            var user = CreateUser(1, "user@example.com", "John", "Doe");
            var product1 = CreateProduct(1, "Product 1", 100.00m, 10);
            var product2 = CreateProduct(2, "Product 2", 200.00m, 5);
            
            _context.Users.Add(user);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "1234567890",
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 },
                    new OrderItemDto { ProductId = 2, Quantity = 1 }
                }
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateProductCacheAsync(1),
                Times.Once);
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateProductCacheAsync(2),
                Times.Once);
        }

        private ApplicationUser CreateUser(long id, string email, string firstName, string lastName)
        {
            return new ApplicationUser
            {
                Id = id,
                Username = email,
                Email = email,
                PasswordHash = "hash",
                FirstName = firstName,
                LastName = lastName,
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
        }

        private Product CreateProduct(int productId, string name, decimal price, int stock, string imageUrl = "")
        {
            return new Product
            {
                ProductId = productId,
                Name = name,
                Description = $"Description for {name}",
                Price = price,
                Stock = stock,
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

