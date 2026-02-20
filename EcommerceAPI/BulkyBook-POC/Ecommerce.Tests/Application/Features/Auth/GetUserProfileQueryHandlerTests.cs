using Ecommerce.Application.Features.Auth.Handlers;
using Ecommerce.Application.Features.Auth.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Ecommerce.Tests.Application.Features.Auth
{
    public class GetUserProfileQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetUserProfileQueryHandler _handler;

        public GetUserProfileQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetUserProfileQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_ShouldReturnUserProfile()
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

            var query = new GetUserProfileQuery { UserId = 1 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Email.Should().Be("test@example.com");
            result.FirstName.Should().Be("Test");
            result.LastName.Should().Be("User");
            result.Role.Should().Be("User");
            result.Status.Should().Be("Active");
            result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(user.UpdatedAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldThrowException()
        {
            // Arrange
            var query = new GetUserProfileQuery { UserId = 999 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(query, CancellationToken.None));
            
            exception.Message.Should().Be("User not found.");
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldThrowException()
        {
            // Arrange
            var query = new GetUserProfileQuery { UserId = 0 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(query, CancellationToken.None));
            
            exception.Message.Should().Be("User not found.");
        }

        [Fact]
        public async Task Handle_WithNegativeUserId_ShouldThrowException()
        {
            // Arrange
            var query = new GetUserProfileQuery { UserId = -1 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(query, CancellationToken.None));
            
            exception.Message.Should().Be("User not found.");
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

            var query = new GetUserProfileQuery { UserId = 1 };

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

            var query = new GetUserProfileQuery { UserId = 123 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(123);
            result.FirstName.Should().Be("Admin");
            result.LastName.Should().Be("User");
            result.Email.Should().Be("admin@example.com");
            result.Role.Should().Be("Admin");
            result.Status.Should().Be("Active");
            result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().BeCloseTo(user.UpdatedAt, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Handle_WithDifferentStatuses_ShouldReturnCorrectStatus()
        {
            // Arrange
            var statuses = new[] { "Active", "Inactive", "Pending", "Deactivated" };

            foreach (var status in statuses)
            {
                var user = new ApplicationUser
                {
                    Id = (long)statuses.ToList().IndexOf(status) + 1,
                    Username = $"user{status}",
                    Email = $"{status.ToLower()}@example.com",
                    PasswordHash = "hash",
                    FirstName = "Test",
                    LastName = "User",
                    Role = "User",
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }

            await _context.SaveChangesAsync();

            // Act & Assert
            foreach (var status in statuses)
            {
                var userId = (long)statuses.ToList().IndexOf(status) + 1;
                var query = new GetUserProfileQuery { UserId = userId };
                
                var result = await _handler.Handle(query, CancellationToken.None);
                
                result.Should().NotBeNull();
                result.Status.Should().Be(status);
            }
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

            var query = new GetUserProfileQuery { UserId = 2 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(2);
            result.Email.Should().Be("user2@example.com");
            result.Role.Should().Be("Admin");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

