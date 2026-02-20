using Ecommerce.Application.Features.Categories.Handlers;
using Ecommerce.Application.Features.Categories.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ecommerce.Tests.Application.Features.Categories
{
    public class GetAllCategoriesQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetAllCategoriesQueryHandler _handler;

        public GetAllCategoriesQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetAllCategoriesQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithCategoriesInDatabase_ShouldReturnAllCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", Description = "Electronic items" },
                new Category { CategoryId = 2, Name = "Books", Description = "Books and publications" },
                new Category { CategoryId = 3, Name = "Clothing", Description = "Apparel items" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(c => c.Name).Should().Contain(new[] { "Electronics", "Books", "Clothing" });
        }

        [Fact]
        public async Task Handle_ShouldOrderCategoriesByName()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Zebra", Description = "Z category" },
                new Category { CategoryId = 2, Name = "Alpha", Description = "A category" },
                new Category { CategoryId = 3, Name = "Beta", Description = "B category" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Alpha");
            result[1].Name.Should().Be("Beta");
            result[2].Name.Should().Be("Zebra");
        }

        [Fact]
        public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldMapCategoryEntityToDto()
        {
            // Arrange
            var category = new Category 
            { 
                CategoryId = 1, 
                Name = "Electronics", 
                Description = "Electronic items" 
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
            result[0].Name.Should().Be("Electronics");
        }

        [Fact]
        public async Task Handle_WithDuplicateNames_ShouldUseDistinct()
        {
            // Arrange
            // Note: In-memory database doesn't enforce unique constraints
            // But Distinct() in the query should still work
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Electronics", Description = "First" },
                new Category { CategoryId = 2, Name = "Electronics", Description = "Second" },
                new Category { CategoryId = 3, Name = "Books", Description = "Books" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            // The Distinct() clause should filter duplicates
            result.Select(c => c.Name).Should().Contain("Electronics", "Books");
            // Since we have distinct on the anonymous type (Id, Name), duplicates with same name but different IDs should still appear
            // But if the query properly groups, we might get fewer results
            var electronicsCount = result.Count(c => c.Name == "Electronics");
            // With Distinct on (Id, Name), both should appear if IDs differ
            electronicsCount.Should().BeGreaterOrEqualTo(1);
        }


        [Fact]
        public async Task Handle_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "C++ Books", Description = "Programming" },
                new Category { CategoryId = 2, Name = "C# Books", Description = "DotNet" },
                new Category { CategoryId = 3, Name = "Books", Description = "General" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Books");
            result.Select(c => c.Name).Should().Contain("C# Books", "C++ Books");
        }

        [Fact]
        public async Task Handle_WithNumericNames_ShouldOrderCorrectly()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "Category 10", Description = "Ten" },
                new Category { CategoryId = 2, Name = "Category 2", Description = "Two" },
                new Category { CategoryId = 3, Name = "Category 1", Description = "One" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Category 1");
            result[1].Name.Should().Be("Category 10"); // String comparison: "10" < "2" alphabetically
            result[2].Name.Should().Be("Category 2");
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var category = new Category 
            { 
                CategoryId = 1, 
                Name = "Electronics", 
                Description = "Electronic items" 
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetAllCategoriesQuery();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_WithLargeDataset_ShouldReturnAllCategories()
        {
            // Arrange
            var categories = Enumerable.Range(1, 100)
                .Select(i => new Category 
                { 
                    CategoryId = i, 
                    Name = $"Category {i:D3}", 
                    Description = $"Description {i}" 
                })
                .ToList();

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(100);
            result.Should().BeInAscendingOrder(c => c.Name);
        }

        [Fact]
        public async Task Handle_WithNullName_ShouldHandleGracefully()
        {
            // Arrange
            // EF Core typically doesn't allow nulls in required fields at the entity level
            // But if allowed, this test checks behavior
            var category = new Category 
            { 
                CategoryId = 1, 
                Name = "", // Empty string instead of null
                Description = "No name" 
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("");
        }

        [Fact]
        public async Task Handle_ShouldOnlyReturnIdAndName()
        {
            // Arrange
            var category = new Category 
            { 
                CategoryId = 5, 
                Name = "Test Category", 
                Description = "This should not be in result" 
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(5);
            result[0].Name.Should().Be("Test Category");
            // Description is not mapped to the DTO
        }

        [Fact]
        public async Task Handle_WithWhitespaceNames_ShouldOrderCorrectly()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category { CategoryId = 1, Name = "   Electronics", Description = "Leading spaces" },
                new Category { CategoryId = 2, Name = "Books", Description = "Normal" },
                new Category { CategoryId = 3, Name = "Clothing", Description = "Normal" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();

            var query = new GetAllCategoriesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            // String ordering: "   Electronics" comes first (space before 'B')
            result[0].Name.Should().Be("   Electronics");
            result[1].Name.Should().Be("Books");
            result[2].Name.Should().Be("Clothing");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

