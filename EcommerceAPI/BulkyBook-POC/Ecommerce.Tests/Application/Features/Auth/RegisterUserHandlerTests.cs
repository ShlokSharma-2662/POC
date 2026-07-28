using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Handlers;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Application.Features.Auth
{
    public class RegisterUserHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly Mock<ILogger<RegisterUserHandler>> _mockLogger;
        private readonly RegisterUserHandler _handler;

        public RegisterUserHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _mockEmailService = new Mock<IEmailService>();
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _mockLogger = new Mock<ILogger<RegisterUserHandler>>();
            
            _handler = new RegisterUserHandler(
                _context, 
                _mockJwtTokenGenerator.Object, 
                _mockEmailService.Object,
                _mockCacheInvalidationService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldRegisterUserAndReturnAuthResult()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "newuser@example.com",
                Password = "password123",
                FirstName = "New",
                LastName = "User",
                Role = "User"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("newuser@example.com");
            result.FirstName.Should().Be("New");
            result.LastName.Should().Be("User");
            result.Role.Should().Be("User");
            result.Token.Should().Be(expectedToken);
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
            user.Should().NotBeNull();
            user!.Email.Should().Be("newuser@example.com");
            user.FirstName.Should().Be("New");
            user.LastName.Should().Be("User");
            user.Role.Should().Be("User");
            user.Status.Should().Be("Active");
            
            // Verify password is hashed
            BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash).Should().BeTrue();
            
            _mockJwtTokenGenerator.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>()), Times.Once);
            _mockEmailService.Verify(
                x => x.SendRegistrationConfirmationEmailAsync("newuser@example.com", "New", "User"),
                Times.Once);
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmptyRole_ShouldDefaultToUser()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = ""
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("User");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@example.com");
            user.Should().NotBeNull();
            user!.Role.Should().Be("User");
        }

        [Fact]
        public async Task Handle_WithNullRole_ShouldDefaultToUser()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = null!
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("User");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@example.com");
            user.Should().NotBeNull();
            user!.Role.Should().Be("User");
        }

        [Fact]
        public async Task Handle_WithWhitespaceRole_ShouldDefaultToUser()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = "   "
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("User");
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var existingUser = new ApplicationUser
            {
                Id = 1,
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FirstName = "Existing",
                LastName = "User",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var command = new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "newpassword123",
                FirstName = "New",
                LastName = "User",
                Role = "User"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(command, CancellationToken.None));
            
            exception.Message.Should().Be("User already exists.");
        }

        [Fact]
        public async Task Handle_WithPrivilegedRole_ShouldForceUserRole()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "admin@example.com",
                Password = "password123",
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be("User");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
            user.Should().NotBeNull();
            user!.Role.Should().Be("User");
        }

        [Fact]
        public async Task Handle_WithEmailServiceFailure_ShouldStillRegisterUser()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            _mockEmailService
                .Setup(x => x.SendRegistrationConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be(expectedToken);
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@example.com");
            user.Should().NotBeNull();
            
            _mockCacheInvalidationService.Verify(
                x => x.InvalidateUserCacheAsync(),
                Times.Once);
        }


        [Fact]
        public async Task Handle_ShouldSetCreatedAtTimestamp()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@example.com");
            user.Should().NotBeNull();
            user!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_ShouldSetUsernameToEmpty()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var command = new RegisterUserCommand
            {
                Email = "user@example.com",
                Password = "password123",
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@example.com");
            user.Should().NotBeNull();
            user!.Username.Should().BeEmpty(); // Username is not set in RegisterUserHandler
        }

        [Fact]
        public async Task Handle_WithMultipleRegistrations_ShouldCreateMultipleUsers()
        {
            // Arrange
            var expectedToken = "mock-jwt-token";
            _mockJwtTokenGenerator
                .Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>()))
                .Returns(expectedToken);

            var commands = new List<RegisterUserCommand>
            {
                new RegisterUserCommand
                {
                    Email = "user1@example.com",
                    Password = "password123",
                    FirstName = "User",
                    LastName = "One",
                    Role = "User"
                },
                new RegisterUserCommand
                {
                    Email = "user2@example.com",
                    Password = "password456",
                    FirstName = "User",
                    LastName = "Two",
                    Role = "User"
                }
            };

            // Act
            var results = new List<AuthResult>();
            foreach (var command in commands)
            {
                var result = await _handler.Handle(command, CancellationToken.None);
                results.Add(result);
            }

            // Assert
            results.Should().HaveCount(2);
            results[0].Email.Should().Be("user1@example.com");
            results[1].Email.Should().Be("user2@example.com");
            
            var users = await _context.Users.Where(u => u.Email.Contains("@example.com")).ToListAsync();
            users.Should().HaveCount(2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
