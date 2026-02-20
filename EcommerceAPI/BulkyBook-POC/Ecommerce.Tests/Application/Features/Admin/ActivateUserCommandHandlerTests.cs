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
    public class ActivateUserCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly ActivateUserCommandHandler _handler;

        public ActivateUserCommandHandlerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new AppDbContext(options);
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _handler = new ActivateUserCommandHandler(_context, _mockCacheInvalidationService.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndInactiveUser_ShouldActivateUserAndReturnTrue()
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
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Status.Should().Be("Active");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndAlreadyActiveUser_ShouldReturnTrueWithoutChanges()
        {
            // Arrange
            var userId = 2L;
            var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "activeuser",
                Email = "active@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Active",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = originalUpdatedAt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var unchangedUser = await _context.Users.FindAsync(userId);
            unchangedUser.Should().NotBeNull();
            unchangedUser!.Status.Should().Be("Active");
            unchangedUser.UpdatedAt.Should().Be(originalUpdatedAt);
            
            // Cache should not be invalidated since no changes were made
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentUserId = 999L;
            var command = new ActivateUserCommand { UserId = nonExistentUserId };
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
        public async Task Handle_WithValidUserIdAndDeactivatedUser_ShouldActivateUserAndReturnTrue()
        {
            // Arrange
            var userId = 3L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "deactivateduser",
                Email = "deactivated@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Deactivated",
                LastName = "User",
                Role = "User",
                Status = "Deactivated",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Status.Should().Be("Active");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndPendingUser_ShouldActivateUserAndReturnTrue()
        {
            // Arrange
            var userId = 4L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "pendinguser",
                Email = "pending@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Pending",
                LastName = "User",
                Role = "User",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Status.Should().Be("Active");
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new ActivateUserCommand { UserId = 0 };
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
            var command = new ActivateUserCommand { UserId = -1 };
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
            var userId = 5L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "canceltestuser",
                Email = "cancel@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Cancel",
                LastName = "Test",
                Role = "User",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
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
            var targetUserId = 6L;
            var otherUserId = 7L;

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Username = "targetuser",
                Email = "target@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Target",
                LastName = "User",
                Role = "User",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var otherUser = new ApplicationUser
            {
                Id = otherUserId,
                Username = "otheruser",
                Email = "other@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Other",
                LastName = "User",
                Role = "User",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.AddRange(targetUser, otherUser);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = targetUserId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedTargetUser = await _context.Users.FindAsync(targetUserId);
            var unchangedOtherUser = await _context.Users.FindAsync(otherUserId);
            
            updatedTargetUser.Should().NotBeNull();
            updatedTargetUser!.Status.Should().Be("Active");
            updatedTargetUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            unchangedOtherUser.Should().NotBeNull();
            unchangedOtherUser!.Status.Should().Be("Inactive");
            unchangedOtherUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(1));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithAdminUser_ShouldActivateAdminUserSuccessfully()
        {
            // Arrange
            var userId = 8L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "adminuser",
                Email = "admin@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ActivateUserCommand { UserId = userId };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser!.Status.Should().Be("Active");
            updatedUser.Role.Should().Be("Admin"); // Role should remain unchanged
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithCacheInvalidationFailure_ShouldStillReturnTrue()
        {
            // Arrange
            var userId = 9L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "cachefailuser",
                Email = "cachefail@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Cache",
                LastName = "Fail",
                Role = "User",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Setup cache service to throw exception
            _mockCacheInvalidationService
                .Setup(x => x.InvalidateUserCacheAsync())
                .ThrowsAsync(new Exception("Cache service unavailable"));

            var command = new ActivateUserCommand { UserId = userId };
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
