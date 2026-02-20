using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Admin.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Ecommerce.Tests.Application.Features.Admin
{
    public class AssignUserRoleCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly AssignUserRoleCommandHandler _handler;

        public AssignUserRoleCommandHandlerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new AppDbContext(options);
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _handler = new AssignUserRoleCommandHandler(_context, _mockCacheInvalidationService.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndAdminRole_ShouldAssignRoleAndReturnTrue()
        {
            // Arrange
            var userId = 1L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Role.Should().Be("Admin");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndUserRole_ShouldAssignRoleAndReturnTrue()
        {
            // Arrange
            var userId = 2L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "adminuser",
                Email = "admin@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "User" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Role.Should().Be("User");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndSameRole_ShouldUpdateTimestampAndReturnTrue()
        {
            // Arrange
            var userId = 3L;
            var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Existing",
                LastName = "User",
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = originalUpdatedAt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Role.Should().Be("Admin");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            updatedUser.UpdatedAt.Should().NotBe(originalUpdatedAt);
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentUserId = 999L;
            var command = new AssignUserRoleCommand 
            { 
                UserId = nonExistentUserId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeFalse();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidRole_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 4L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "InvalidRole" 
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, cancellationToken));
            
            exception.Message.Should().Be("Invalid role.");
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithEmptyRole_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 5L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "" 
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, cancellationToken));
            
            exception.Message.Should().Be("Invalid role.");
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithNullRole_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 6L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = null! 
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, cancellationToken));
            
            exception.Message.Should().Be("Invalid role.");
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new AssignUserRoleCommand 
            { 
                UserId = 0, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeFalse();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithNegativeUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new AssignUserRoleCommand 
            { 
                UserId = -1, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeFalse();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var userId = 7L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "canceltestuser",
                Email = "cancel@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Cancel",
                LastName = "Test",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "Admin" 
            };
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldOnlyAffectTargetUser()
        {
            // Arrange
            var targetUserId = 8L;
            var otherUserId = 9L;

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Username = "targetuser",
                Email = "target@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Target",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var otherUserOriginalUpdatedAt = DateTime.UtcNow.AddDays(-1);
            var otherUser = new ApplicationUser
            {
                Id = otherUserId,
                Username = "otheruser",
                Email = "other@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Other",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = otherUserOriginalUpdatedAt
            };

            _context.Users.AddRange(targetUser, otherUser);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = targetUserId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedTargetUser = await _context.Users.FindAsync(targetUserId);
            var unchangedOtherUser = await _context.Users.FindAsync(otherUserId);
            
            updatedTargetUser.Should().NotBeNull();
            updatedTargetUser!.Role.Should().Be("Admin");
            updatedTargetUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            unchangedOtherUser.Should().NotBeNull();
            unchangedOtherUser!.Role.Should().Be("User");
            unchangedOtherUser.UpdatedAt.Should().BeCloseTo(otherUserOriginalUpdatedAt, TimeSpan.FromSeconds(2));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithCaseSensitiveRole_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 10L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "admin" // lowercase
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, cancellationToken));
            
            exception.Message.Should().Be("Invalid role.");
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithWhitespaceRole_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = 11L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "  Admin  " // with whitespace
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, cancellationToken));
            
            exception.Message.Should().Be("Invalid role.");
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithInactiveUser_ShouldAssignRoleSuccessfully()
        {
            // Arrange
            var userId = 12L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "inactiveuser",
                Email = "inactive@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Inactive",
                LastName = "User",
                Role = "User",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Role.Should().Be("Admin");
            updatedUser.Status.Should().Be("Inactive"); // Status should remain unchanged
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithCacheInvalidationFailure_ShouldThrowException()
        {
            // Arrange
            var userId = 13L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "cachefailuser",
                Email = "cachefail@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Cache",
                LastName = "Fail",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Setup cache service to throw exception
            _mockCacheInvalidationService
                .Setup(x => x.InvalidateUserCacheAsync())
                .ThrowsAsync(new Exception("Cache service unavailable"));

            var command = new AssignUserRoleCommand 
            { 
                UserId = userId, 
                Role = "Admin" 
            };
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            // The handler should throw the exception since it doesn't handle cache failures
            await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, cancellationToken));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
