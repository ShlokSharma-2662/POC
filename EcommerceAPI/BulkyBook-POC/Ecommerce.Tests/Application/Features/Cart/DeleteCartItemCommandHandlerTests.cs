using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Application.Features.Cart.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using CartEntity = Ecommerce.Domain.Entities.Cart;

namespace Ecommerce.Tests.Application.Features.Cart
{
    public class DeleteCartItemCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly DeleteCartItemCommandHandler _handler;

        public DeleteCartItemCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            
            _handler = new DeleteCartItemCommandHandler(
                _context,
                _mockHttpContextAccessor.Object,
                _mockCacheInvalidationService.Object);
        }

        [Fact]
        public async Task Handle_WithExistingItem_ShouldRemoveItem()
        {
            // Arrange
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

            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(1);
            updatedCart.Items.First().ProductId.Should().Be(2);
            _mockCacheInvalidationService.Verify(
                service => service.InvalidateCartCacheAsync(1L),
                Times.Once);
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

            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNonExistentCart_ShouldReturnUnitValue()
        {
            // Arrange
            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithNonExistentItem_ShouldReturnUnitValue()
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

            var command = new DeleteCartItemCommand { ProductId = 999 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(1); // Item should still exist
        }

        [Fact]
        public async Task Handle_WithEmptyCart_ShouldReturnUnitValue()
        {
            // Arrange
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>()
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithMultipleItems_ShouldOnlyRemoveTargetItem()
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

            var command = new DeleteCartItemCommand { ProductId = 2 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            updatedCart!.Items.Should().HaveCount(2);
            updatedCart.Items.Any(i => i.ProductId == 2).Should().BeFalse();
            updatedCart.Items.Any(i => i.ProductId == 1).Should().BeTrue();
            updatedCart.Items.Any(i => i.ProductId == 3).Should().BeTrue();
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
            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId);
            updatedCart.Should().NotBeNull(); // Cart should still exist
            updatedCart!.UserId.Should().Be("1");
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

            var command = new DeleteCartItemCommand { ProductId = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_WithMultipleCarts_ShouldOnlyAffectUserCart()
        {
            // Arrange
            var user1Cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2 },
                    new CartItem { ProductId = 2, Quantity = 3 }
                }
            };
            
            var user2Cart = new CartEntity 
            { 
                UserId = "2",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 5 },
                    new CartItem { ProductId = 3, Quantity = 1 }
                }
            };
            
            _context.Carts.AddRange(user1Cart, user2Cart);
            await _context.SaveChangesAsync();

            var command = new DeleteCartItemCommand { ProductId = 2 };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var updatedCart1 = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "1");
            var updatedCart2 = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == "2");
            
            updatedCart1!.Items.Should().HaveCount(1);
            updatedCart1.Items.First().ProductId.Should().Be(1);
            updatedCart2!.Items.Should().HaveCount(2); // User 2's cart unchanged
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
