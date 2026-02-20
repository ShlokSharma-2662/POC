using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Handlers;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ecommerce.Tests.Application.Features.Auth
{
    public class LoginUserHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly LoginUserHandler _handler;

        public LoginUserHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _handler = new LoginUserHandler(_context, _mockJwtTokenGenerator.Object);
        }

        [Fact]
        public async Task Handle_WithValidCredentials_ShouldReturnAuthResult()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = password 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(1);
            result.Email.Should().Be("test@example.com");
            result.FirstName.Should().Be("Test");
            result.LastName.Should().Be("User");
            result.Role.Should().Be("User");
            result.Token.Should().Be(expectedToken);
            
            _mockJwtTokenGenerator.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentEmail_ShouldThrowException()
        {
            // Arrange
            var command = new LoginUserCommand 
            { 
                Email = "nonexistent@example.com", 
                Password = "password123" 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WithIncorrectPassword_ShouldThrowException()
        {
            // Arrange
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword");
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = "wrongpassword" 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WithDeactivatedUser_ShouldThrowException()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Deactivated",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = password 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Account is deactivated. Please contact support.");
        }

        [Fact]
        public async Task Handle_WithEmptyEmail_ShouldThrowException()
        {
            // Arrange
            var command = new LoginUserCommand 
            { 
                Email = "", 
                Password = "password123" 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Contain("Invalid");
        }

        [Fact]
        public async Task Handle_WithInactiveUser_ShouldAllowLogin()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Inactive", // Not "Deactivated"
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = password 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(expectedToken);
        }

        [Fact]
        public async Task Handle_WithPendingUser_ShouldAllowLogin()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = password 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(expectedToken);
        }

        [Fact]
        public async Task Handle_WithAdminRole_ShouldReturnAuthResultWithAdminRole()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "adminuser",
                Email = "admin@example.com",
                PasswordHash = passwordHash,
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new LoginUserCommand 
            { 
                Email = "admin@example.com", 
                Password = password 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("Admin");
            result.Token.Should().Be(expectedToken);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = passwordHash,
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

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", 
                Password = password 
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cts.Token));
        }

        [Fact]
        public async Task Handle_WithCaseSensitiveEmail_ShouldMatchExactly()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "Test@Example.com", // Mixed case
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new LoginUserCommand 
            { 
                Email = "test@example.com", // Lowercase - should not match
                Password = password 
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task Handle_WithExactEmailMatch_ShouldSucceed()
        {
            // Arrange
            var password = "password123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                Email = "Test@Example.com", // Mixed case
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new LoginUserCommand 
            { 
                Email = "Test@Example.com", // Exact match
                Password = password 
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("Test@Example.com");
            result.Token.Should().Be(expectedToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}







