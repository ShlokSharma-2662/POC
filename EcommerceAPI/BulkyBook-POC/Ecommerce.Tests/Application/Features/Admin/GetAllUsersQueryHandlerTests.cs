using Ecommerce.Application.Common.Models;
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
    public class GetAllUsersQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetAllUsersQueryHandler _handler;

        public GetAllUsersQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetAllUsersQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithNoFilters_ShouldReturnAllUsers()
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
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    UpdatedAt = DateTime.UtcNow.AddHours(-1)
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
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().BeInDescendingOrder(x => x.CreatedAt);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnFilteredUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Username = "activeuser",
                    Email = "active@example.com",
                    PasswordHash = "hash1",
                    FirstName = "Active",
                    LastName = "User",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "inactiveuser",
                    Email = "inactive@example.com",
                    PasswordHash = "hash2",
                    FirstName = "Inactive",
                    LastName = "User",
                    Role = "User",
                    Status = "Inactive",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                Status = "Active" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Status.Should().Be("Active");
        }

        [Fact]
        public async Task Handle_WithRoleFilter_ShouldReturnFilteredUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Username = "adminuser",
                    Email = "admin@example.com",
                    PasswordHash = "hash1",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "regularuser",
                    Email = "user@example.com",
                    PasswordHash = "hash2",
                    FirstName = "Regular",
                    LastName = "User",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                Role = "Admin" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Role.Should().Be("Admin");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnMatchingUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Username = "john_doe",
                    Email = "john@example.com",
                    PasswordHash = "hash1",
                    FirstName = "John",
                    LastName = "Doe",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "jane_smith",
                    Email = "jane@example.com",
                    PasswordHash = "hash2",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery 
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
            result.Items.First().Email.Should().Be("john@example.com");
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var users = Enumerable.Range(1, 25).Select(i => new ApplicationUser
            {
                Id = i,
                Username = $"user{i}",
                Email = $"user{i}@example.com",
                PasswordHash = $"hash{i}",
                FirstName = $"User",
                LastName = $"{i}",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            }).ToList();

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery { PageNumber = 2, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
            result.Items.First().Id.Should().Be(11);
        }

        [Fact]
        public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyResult()
        {
            // Arrange
            var query = new GetAllUsersQuery { PageNumber = 1, PageSize = 10 };

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

            var query = new GetAllUsersQuery { PageNumber = 1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_WithCombinedFilters_ShouldReturnCorrectResults()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Username = "admin_user1",
                    Email = "admin1@example.com",
                    PasswordHash = "hash1",
                    FirstName = "Admin",
                    LastName = "One",
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 2,
                    Username = "admin_user2",
                    Email = "admin2@example.com",
                    PasswordHash = "hash2",
                    FirstName = "Admin",
                    LastName = "Two",
                    Role = "Admin",
                    Status = "Inactive",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationUser
                {
                    Id = 3,
                    Username = "regular_user",
                    Email = "regular@example.com",
                    PasswordHash = "hash3",
                    FirstName = "Regular",
                    LastName = "User",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                Role = "Admin",
                Status = "Active",
                SearchTerm = "admin"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Role.Should().Be("Admin");
            result.Items.First().Status.Should().Be("Active");
        }

        [Fact]
        public async Task Handle_WithNullSearchTerm_ShouldNotFilterBySearch()
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
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = null
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldMapToAdminUserDto()
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
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var query = new GetAllUsersQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            
            var dto = result.Items.First();
            dto.Id.Should().Be(1);
            dto.Username.Should().Be("testuser");
            dto.Email.Should().Be("test@example.com");
            dto.Role.Should().Be("Admin");
            dto.Status.Should().Be("Active");
            dto.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
            dto.UpdatedAt.Should().BeCloseTo(user.UpdatedAt, TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}







