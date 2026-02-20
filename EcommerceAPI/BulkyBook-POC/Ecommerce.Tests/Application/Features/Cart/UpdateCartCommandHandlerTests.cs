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
    public class UpdateCartCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly UpdateCartCommandHandler _handler;

        public UpdateCartCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            
            _handler = new UpdateCartCommandHandler(_context, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task Handle_WithValidUpdate_ShouldUpdateQuantity()
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

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(1);
            updatedCart.Items.First().Quantity.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithZeroQuantity_ShouldRemoveItem()
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

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 0 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNegativeQuantity_ShouldRemoveItem()
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

            var command = new UpdateCartCommand { ProductId = 1, Quantity = -5 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNonExistentCart_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Cart not found");
        }

        [Fact]
        public async Task Handle_WithNonExistentItem_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var cart = new CartEntity { UserId = "1" };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new UpdateCartCommand { ProductId = 999, Quantity = 5 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Item not found");
        }

        [Fact]
        public async Task Handle_WithInsufficientStock_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 5);
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

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 10 }; // Stock is only 5

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("Only 5 units available");
        }

        [Fact]
        public async Task Handle_WithExactStock_ShouldUpdateSuccessfully()
        {
            // Arrange
            var product = CreateProduct(1, "Test Product", 100.00m, 5);
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

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 }; // Exact stock

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.First().Quantity.Should().Be(5);
        }

        [Fact]
        public async Task Handle_WithNonExistentProduct_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 999, Quantity = 2 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new UpdateCartCommand { ProductId = 999, Quantity = 5 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Product not found");
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithMultipleItems_ShouldOnlyUpdateTargetItem()
        {
            // Arrange
            var product1 = CreateProduct(1, "Product 1", 100.00m, 10);
            var product2 = CreateProduct(2, "Product 2", 200.00m, 10);
            _context.Products.AddRange(product1, product2);
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2 },
                    new CartItem { ProductId = 2, Quantity = 3 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(2);
            updatedCart.Items.First(i => i.ProductId == 1).Quantity.Should().Be(5);
            updatedCart.Items.First(i => i.ProductId == 2).Quantity.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
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

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new UpdateCartCommand { ProductId = 1, Quantity = 5 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
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

