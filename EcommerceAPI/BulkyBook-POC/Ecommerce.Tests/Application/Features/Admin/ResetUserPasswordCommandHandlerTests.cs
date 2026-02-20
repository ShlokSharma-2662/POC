using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Admin.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ecommerce.Tests.Application.Features.Admin
{
    public class ResetUserPasswordCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly ResetUserPasswordCommandHandler _handler;

        public ResetUserPasswordCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _handler = new ResetUserPasswordCommandHandler(_context, _mockCacheInvalidationService.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserIdAndPassword_ShouldResetPassword()
        {
            // Arrange
            var originalPasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword");
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = originalPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "newpassword123" 
            };
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(1L);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordHash.Should().NotBe(originalPasswordHash);
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify new password can be verified
            BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser.PasswordHash).Should().BeTrue();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new ResetUserPasswordCommand 
            { 
                UserId = 999, 
                NewPassword = "newpassword123" 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new ResetUserPasswordCommand 
            { 
                UserId = 0, 
                NewPassword = "newpassword123" 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WithNegativeUserId_ShouldReturnFalse()
        {
            // Arrange
            var command = new ResetUserPasswordCommand 
            { 
                UserId = -1, 
                NewPassword = "newpassword123" 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WithShortPassword_ShouldThrowArgumentException()
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

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "short" 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("at least 8 characters");
        }

        [Fact]
        public async Task Handle_WithNullPassword_ShouldThrowArgumentException()
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

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = null! 
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithEmptyPassword_ShouldThrowArgumentException()
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

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "" 
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithWhitespacePassword_ShouldThrowArgumentException()
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

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "   " 
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
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

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "newpassword123" 
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldOnlyAffectTargetUser()
        {
            // Arrange
            var targetUserId = 1L;
            var otherUserId = 2L;
            
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Username = "targetuser",
                Email = "target@example.com",
                PasswordHash = "target_hash",
                FirstName = "Target",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var otherUserOriginalPasswordHash = "other_hash";
            var otherUser = new ApplicationUser
            {
                Id = otherUserId,
                Username = "otheruser",
                Email = "other@example.com",
                PasswordHash = otherUserOriginalPasswordHash,
                FirstName = "Other",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.AddRange(targetUser, otherUser);
            await _context.SaveChangesAsync();

            var command = new ResetUserPasswordCommand 
            { 
                UserId = targetUserId, 
                NewPassword = "newpassword123" 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            
            var updatedTargetUser = await _context.Users.FindAsync(targetUserId);
            var unchangedOtherUser = await _context.Users.FindAsync(otherUserId);
            
            updatedTargetUser.Should().NotBeNull();
            updatedTargetUser!.PasswordHash.Should().NotBe("target_hash");
            
            unchangedOtherUser.Should().NotBeNull();
            unchangedOtherUser!.PasswordHash.Should().Be(otherUserOriginalPasswordHash);
        }

        [Fact]
        public async Task Handle_WithCacheInvalidationFailure_ShouldPropagateException()
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

            _mockCacheInvalidationService
                .Setup(x => x.InvalidateUserCacheAsync())
                .ThrowsAsync(new Exception("Cache service unavailable"));

            var command = new ResetUserPasswordCommand 
            { 
                UserId = 1, 
                NewPassword = "newpassword123" 
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}







