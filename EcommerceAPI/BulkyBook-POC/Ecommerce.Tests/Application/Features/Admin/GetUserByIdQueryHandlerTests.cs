using Ecommerce.Application.Features.Admin.Handlers;
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
    public class GetUserByIdQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetUserByIdQueryHandler _handler;

        public GetUserByIdQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetUserByIdQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_ShouldReturnUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var query = new GetUserByIdQuery { UserId = 1 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Username.Should().Be("testuser");
            result.Email.Should().Be("test@example.com");
            result.Role.Should().Be("User");
            result.Status.Should().Be("Active");
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldReturnNull()
        {
            // Arrange
            var query = new GetUserByIdQuery { UserId = 999 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldReturnNull()
        {
            // Arrange
            var query = new GetUserByIdQuery { UserId = 0 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithNegativeUserId_ShouldReturnNull()
        {
            // Arrange
            var query = new GetUserByIdQuery { UserId = -1 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetUserByIdQuery { UserId = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_ShouldMapAllFieldsCorrectly()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = 123,
                Username = "adminuser",
                Email = "admin@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var query = new GetUserByIdQuery { UserId = 123 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(123);
            result.Username.Should().Be("adminuser");
            result.Email.Should().Be("admin@example.com");
            result.Role.Should().Be("Admin");
            result.Status.Should().Be("Active");
            result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(user.UpdatedAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldReturnCorrectUser()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Username = "user1",
                    Email = "user1@example.com",
                    PasswordHash = "hash1",
                    FirstName = "User",
                    LastName = "One",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "user2",
                    Email = "user2@example.com",
                    PasswordHash = "hash2",
                    FirstName = "User",
                    LastName = "Two",
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 3,
                    Username = "user3",
                    Email = "user3@example.com",
                    PasswordHash = "hash3",
                    FirstName = "User",
                    LastName = "Three",
                    Role = "User",
                    Status = "Inactive",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetUserByIdQuery { UserId = 2 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(2);
            result.Username.Should().Be("user2");
            result.Email.Should().Be("user2@example.com");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}







