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
    public class ClearCartCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly ClearCartCommandHandler _handler;

        public ClearCartCommandHandlerTests()
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
            
            _handler = new ClearCartCommandHandler(_context, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task Handle_WithCartContainingItems_ShouldClearAllItems()
        {
            // Arrange
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2 },
                    new CartItem { ProductId = 2, Quantity = 3 },
                    new CartItem { ProductId = 3, Quantity = 1 }
                }
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var clearedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            clearedCart.Should().NotBeNull();
            clearedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithEmptyCart_ShouldNotThrowException()
        {
            // Arrange
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>()
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var clearedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            clearedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNonExistentCart_ShouldNotThrowException()
        {
            // Arrange
            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithSingleItem_ShouldRemoveItem()
        {
            // Arrange
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

            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var clearedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            clearedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new ClearCartCommand();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldPreserveCart()
        {
            // Arrange
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

            var cartId = cart.CartId;
            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var clearedCart = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId);
            clearedCart.Should().NotBeNull(); // Cart should still exist
            clearedCart!.UserId.Should().Be("1");
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
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

            var command = new ClearCartCommand();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_WithMultipleCarts_ShouldOnlyClearUserCart()
        {
            // Arrange
            var user1Cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2 }
                }
            };
            
            var user2Cart = new CartEntity 
            { 
                UserId = "2",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 2, Quantity = 3 }
                }
            };
            
            _context.Carts.AddRange(user1Cart, user2Cart);
            await _context.SaveChangesAsync();

            var command = new ClearCartCommand();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var clearedCart1 = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            var clearedCart2 = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "2");
            
            clearedCart1!.Items.Should().BeEmpty();
            clearedCart2!.Items.Should().HaveCount(1);
            clearedCart2.Items.First().ProductId.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

