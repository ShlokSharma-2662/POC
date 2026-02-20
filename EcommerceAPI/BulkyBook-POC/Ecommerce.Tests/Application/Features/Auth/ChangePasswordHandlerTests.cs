using Ecommerce.Application.Features.Auth.Commands;
using Ecommerce.Application.Features.Auth.Handlers;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Tests.Application.Features.Auth
{
    public class ChangePasswordHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ChangePasswordHandler _handler;

        public ChangePasswordHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _handler = new ChangePasswordHandler(_context);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldChangePassword()
        {
            // Arrange
            var oldPassword = "oldpassword123";
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = oldPassword,
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(1L);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash);
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            // Verify new password can be verified
            BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser.PasswordHash).Should().BeTrue();
            
            // Verify old password no longer works
            BCrypt.Net.BCrypt.Verify(oldPassword, updatedUser.PasswordHash).Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = 999,
                OldPassword = "oldpassword123",
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("User not found.");
        }

        [Fact]
        public async Task Handle_WithIncorrectOldPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = "wrongpassword",
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Current password is incorrect.");
        }

        [Fact]
        public async Task Handle_WithMismatchedPasswords_ShouldThrowArgumentException()
        {
            // Arrange
            var oldPassword = "oldpassword123";
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = oldPassword,
                NewPassword = "newpassword123",
                ConfirmPassword = "differentpassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("New password and confirm password do not match.");
        }

        [Fact]
        public async Task Handle_WithShortPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var oldPassword = "oldpassword123";
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = oldPassword,
                NewPassword = "short",
                ConfirmPassword = "short"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("at least 8 characters");
        }

        [Fact]
        public async Task Handle_WithSameOldAndNewPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var oldPassword = "oldpassword123";
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = oldPassword,
                NewPassword = oldPassword,
                ConfirmPassword = oldPassword
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("New password must be different from the old password.");
        }

        [Fact]
        public async Task Handle_WithNullOldPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = null!,
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("All password fields are required.");
        }

        [Fact]
        public async Task Handle_WithEmptyNewPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = "oldpassword123",
                NewPassword = "",
                ConfirmPassword = ""
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("All password fields are required.");
        }

        [Fact]
        public async Task Handle_WithWhitespaceConfirmPassword_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = "oldpassword123",
                NewPassword = "newpassword123",
                ConfirmPassword = "   "
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("All password fields are required.");
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var oldPassword = "oldpassword123";
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = oldPasswordHash,
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

            var command = new ChangePasswordCommand
            {
                UserId = 1,
                OldPassword = oldPassword,
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new ChangePasswordCommand
            {
                UserId = 0,
                OldPassword = "oldpassword123",
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("User not found.");
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldOnlyAffectTargetUser()
        {
            // Arrange
            var targetUserId = 1L;
            var otherUserId = 2L;
            
            var oldPassword = "oldpassword123";
            var targetOldPasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
            var otherOldPasswordHash = BCrypt.Net.BCrypt.HashPassword("otherpassword123");
            
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Username = "targetuser",
                Email = "target@example.com",
                PasswordHash = targetOldPasswordHash,
                FirstName = "Target",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            var otherUser = new ApplicationUser
            {
                Id = otherUserId,
                Username = "otheruser",
                Email = "other@example.com",
                PasswordHash = otherOldPasswordHash,
                FirstName = "Other",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.AddRange(targetUser, otherUser);
            await _context.SaveChangesAsync();

            var command = new ChangePasswordCommand
            {
                UserId = targetUserId,
                OldPassword = oldPassword,
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            
            var updatedTargetUser = await _context.Users.FindAsync(targetUserId);
            var unchangedOtherUser = await _context.Users.FindAsync(otherUserId);
            
            updatedTargetUser.Should().NotBeNull();
            BCrypt.Net.BCrypt.Verify("newpassword123", updatedTargetUser!.PasswordHash).Should().BeTrue();
            
            unchangedOtherUser.Should().NotBeNull();
            unchangedOtherUser!.PasswordHash.Should().Be(otherOldPasswordHash);
            BCrypt.Net.BCrypt.Verify("otherpassword123", unchangedOtherUser.PasswordHash).Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}







