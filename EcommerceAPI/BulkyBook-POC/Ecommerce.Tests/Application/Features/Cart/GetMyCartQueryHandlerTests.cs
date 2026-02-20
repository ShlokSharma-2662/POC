using Ecommerce.Application.Common.Services;
using Ecommerce.Application.Features.Cart.Handlers;
using Ecommerce.Application.Features.Cart.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using CartEntity = Ecommerce.Domain.Entities.Cart;

namespace Ecommerce.Tests.Application.Features.Cart
{
    public class GetMyCartQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IUserContextService> _mockUserContextService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetMyCartQueryHandler _handler;

        public GetMyCartQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockUserContextService = new Mock<IUserContextService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetMyCartQueryHandler(
                _context, 
                _mockUserContextService.Object, 
                _mockCacheService.Object, 
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithValidCart_ShouldReturnCartItems()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var category = new Category { CategoryId = 1, Name = "Electronics" };
            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Test Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1,
                Category = category
            };
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2, Product = product }
                }
            };
            
            _context.Categories.Add(category);
            _context.Products.Add(product);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].ProductId.Should().Be(1);
            result[0].Quantity.Should().Be(2);
            result[0].Product.Should().NotBeNull();
            result[0].Product.Name.Should().Be("Test Product");
            result[0].Product.Price.Should().Be(100.00m);
            result[0].Product.CategoryName.Should().Be("Electronics");
        }

        [Fact]
        public async Task Handle_WithNonExistentCart_ShouldCreateEmptyCart()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == "1");
            cart.Should().NotBeNull(); // Cart should be created
        }

        [Fact]
        public async Task Handle_WithEmptyCart_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var cart = new CartEntity { UserId = "1", Items = new List<CartItem>() };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithNullUserId_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns((long?)null);

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WithMultipleItems_ShouldReturnAllItems()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var category = new Category { CategoryId = 1, Name = "Electronics" };
            var product1 = new Product
            {
                ProductId = 1,
                Name = "Product 1",
                Description = "Description 1",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1,
                Category = category
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
                CategoryId = 1,
                Category = category
            };
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 2, Product = product1 },
                    new CartItem { ProductId = 2, Quantity = 3, Product = product2 }
                }
            };
            
            _context.Categories.Add(category);
            _context.Products.AddRange(product1, product2);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Any(i => i.ProductId == 1 && i.Quantity == 2).Should().BeTrue();
            result.Any(i => i.ProductId == 2 && i.Quantity == 3).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithNullProduct_ShouldHandleGracefully()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 999, Quantity = 2, Product = null! }
                }
            };
            
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].ProductId.Should().Be(999);
            result[0].Product.Name.Should().BeEmpty(); // Null product handled gracefully
            result[0].Product.Price.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetMyCartQuery();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldMapProductDetailsCorrectly()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var category = new Category { CategoryId = 2, Name = "Books" };
            var product = new Product
            {
                ProductId = 5,
                Name = "Test Book",
                Description = "A great book",
                Price = 29.99m,
                Stock = 20,
                ImageUrl = "/images/book.jpg",
                IsActive = true,
                IsDeleted = false,
                CategoryId = 2,
                Category = category
            };
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 5, Quantity = 1, Product = product }
                }
            };
            
            _context.Categories.Add(category);
            _context.Products.Add(product);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            var item = result[0];
            item.Product.ProductId.Should().Be(5);
            item.Product.Name.Should().Be("Test Book");
            item.Product.Description.Should().Be("A great book");
            item.Product.Price.Should().Be(29.99m);
            item.Product.Stock.Should().Be(20);
            item.Product.ImageUrl.Should().Be("/images/book.jpg");
            item.Product.CategoryId.Should().Be(2);
            item.Product.CategoryName.Should().Be("Books");
        }

        [Fact]
        public async Task Handle_WithNullCategory_ShouldHandleGracefully()
        {
            // Arrange
            _mockUserContextService.Setup(x => x.GetCurrentUserId()).Returns(1L);

            var product = new Product
            {
                ProductId = 1,
                Name = "Test Product",
                Description = "Test Description",
                Price = 100.00m,
                Stock = 10,
                IsActive = true,
                IsDeleted = false,
                CategoryId = 1,
                Category = null!
            };
            
            var cart = new CartEntity 
            { 
                UserId = "1",
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 1, Product = product }
                }
            };
            
            _context.Products.Add(product);
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            var query = new GetMyCartQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result[0].Product.CategoryName.Should().BeEmpty();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

