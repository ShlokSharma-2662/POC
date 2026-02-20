using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Application.Features.Cart.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using CartEntity = Ecommerce.Domain.Entities.Cart;

namespace Ecommerce.Tests.Application.Features.Cart
{
    public class AddToCartCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly AddToCartCommandHandler _handler;

        public AddToCartCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            // Setup default HTTP context with user claims
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            
            _handler = new AddToCartCommandHandler(_context, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task Handle_WithNewCartAndValidProduct_ShouldAddItem()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 2 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Item added to cart successfully");
            result.RequestedQuantity.Should().Be(2);
            result.CurrentCartQuantity.Should().Be(2);
            result.CurrentStock.Should().Be(10);

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            cart.Should().NotBeNull();
            cart!.Items.Should().HaveCount(1);
            cart.Items.First().ProductId.Should().Be(1);
            cart.Items.First().Quantity.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithExistingCart_ShouldAddToExistingCart()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            
            var cart = new CartEntity { UserId = "1" };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 3 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.CurrentCartQuantity.Should().Be(3);

            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(1);
            updatedCart.Items.First().Quantity.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WithExistingItem_ShouldUpdateQuantity()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 3 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.CurrentCartQuantity.Should().Be(5); // 2 + 3

            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(1);
            updatedCart.Items.First().Quantity.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithZeroQuantity_ShouldDefaultTo1()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 0 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.RequestedQuantity.Should().Be(1); // Should default to 1
            result.CurrentCartQuantity.Should().Be(1);

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            cart!.Items.First().Quantity.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithNegativeQuantity_ShouldDefaultTo1()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = -5 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.RequestedQuantity.Should().Be(1);
            result.CurrentCartQuantity.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithNonExistentProduct_ShouldReturnFailure()
        {
            // Arrange
            var command = new AddToCartCommand { ProductId = 999, Quantity = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Product not found");
        }

        [Fact]
        public async Task Handle_WithInsufficientStock_ShouldReturnFailure()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 5);
            _context.Products.Add(product);
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 3 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 5 }; // Total would be 8, but stock is 5

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Only 2 units available");
            result.MaxAddableQuantity.Should().Be(2); // 5 - 3 = 2
            result.CurrentStock.Should().Be(5);
            result.CurrentCartQuantity.Should().Be(3);
            result.RequestedQuantity.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithOutOfStock_ShouldReturnFailure()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 5);
            _context.Products.Add(product);
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 5 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("This item is out of stock or you've reached the available stock in your cart.");
            result.MaxAddableQuantity.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new AddToCartCommand { ProductId = 1, Quantity = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithMultipleProducts_ShouldAddSeparately()
        {
            // Arrange
            var product1 = CreateProduct(1, "Product 1", 100.00m, 10);
            var product2 = CreateProduct(2, "Product 2", 200.00m, 5);
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();

            var command1 = new AddToCartCommand { ProductId = 1, Quantity = 2 };
            var command2 = new AddToCartCommand { ProductId = 2, Quantity = 3 };

            // Act
            var result1 = await _handler.Handle(command1, CancellationToken.None);
            var result2 = await _handler.Handle(command2, CancellationToken.None);

            // Assert
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();

            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            cart!.Items.Should().HaveCount(2);
            cart.Items.First(i => i.ProductId == 1).Quantity.Should().Be(2);
            cart.Items.First(i => i.ProductId == 2).Quantity.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldCreateCartIfNotExists()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == "1");
            cart.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithSubClaim_ShouldUseSubClaim()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("sub", "2")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var product = CreateProduct(1, "Test Product", 100.00m, 10);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var command = new AddToCartCommand { ProductId = 1, Quantity = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == "2");
            cart.Should().NotBeNull();
        }

        private Product CreateProduct(int productId, string name, decimal price, int stock)
        {
            return new Product
            {
                ProductId = productId,
                Name = name,
                Description = $"Description for {name}",
                Price = price,
                Stock = stock,
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

