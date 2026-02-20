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
    public class DeactivateUserCommandHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheInvalidationService> _mockCacheInvalidationService;
        private readonly DeactivateUserCommandHandler _handler;

        public DeactivateUserCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheInvalidationService = new Mock<ICacheInvalidationService>();
            _handler = new DeactivateUserCommandHandler(_context, _mockCacheInvalidationService.Object);
        }

        [Fact]
        public async Task Handle_WithActiveUser_ShouldDeactivateAndReturnTrue()
        {
            var userId = 1L;
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
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new DeactivateUserCommand { UserId = userId };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            var updated = await _context.Users.FindAsync(userId);
            updated!.Status.Should().Be("Deactivated");
            updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAlreadyDeactivatedUser_ShouldReturnTrueWithoutInvalidation()
        {
            var userId = 2L;
            var prevUpdated = DateTime.UtcNow.AddDays(-2);
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "deactivateduser",
                Email = "d@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Dec",
                LastName = "User",
                Role = "User",
                Status = "Deactivated",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = prevUpdated
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var command = new DeactivateUserCommand { UserId = userId };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().BeTrue();
            var unchanged = await _context.Users.FindAsync(userId);
            unchanged!.Status.Should().Be("Deactivated");
            unchanged.UpdatedAt.Should().Be(prevUpdated);
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldReturnFalse()
        {
            var command = new DeactivateUserCommand { UserId = 999 };
            var result = await _handler.Handle(command, CancellationToken.None);
            result.Should().BeFalse();
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithPendingUser_ShouldDeactivateAndReturnTrue()
        {
            var userId = 3L;
            var user = new ApplicationUser
            {
                Id = userId,
                Username = "pendinguser",
                Email = "p@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Pen",
                LastName = "User",
                Role = "User",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new DeactivateUserCommand { UserId = userId }, CancellationToken.None);

            result.Should().BeTrue();
            var updated = await _context.Users.FindAsync(userId);
            updated!.Status.Should().Be("Deactivated");
            updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithZeroUserId_ShouldReturnFalse()
        {
            var result = await _handler.Handle(new DeactivateUserCommand { UserId = 0 }, CancellationToken.None);
            result.Should().BeFalse();
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNegativeUserId_ShouldReturnFalse()
        {
            var result = await _handler.Handle(new DeactivateUserCommand { UserId = -1 }, CancellationToken.None);
            result.Should().BeFalse();
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithCancellation_ShouldThrowOperationCanceled()
        {
            var userId = 4L;
            _context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Username = "canceluser",
                Email = "c@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Can",
                LastName = "Cel",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(new DeactivateUserCommand { UserId = userId }, cts.Token));
        }

        [Fact]
        public async Task Handle_WithMultipleUsers_ShouldOnlyAffectTarget()
        {
            var targetId = 5L;
            var otherId = 6L;
            var nowMinus = DateTime.UtcNow.AddDays(-1);

            _context.Users.AddRange(
                new ApplicationUser
                {
                    Id = targetId,
                    Username = "target",
                    Email = "t@example.com",
                    PasswordHash = "hashedpassword",
                    FirstName = "Tar",
                    LastName = "Get",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = nowMinus,
                    UpdatedAt = nowMinus
                },
                new ApplicationUser
                {
                    Id = otherId,
                    Username = "other",
                    Email = "o@example.com",
                    PasswordHash = "hashedpassword",
                    FirstName = "Oth",
                    LastName = "Er",
                    Role = "User",
                    Status = "Active",
                    CreatedAt = nowMinus,
                    UpdatedAt = nowMinus
                }
            );
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new DeactivateUserCommand { UserId = targetId }, CancellationToken.None);

            result.Should().BeTrue();
            var target = await _context.Users.FindAsync(targetId);
            var other = await _context.Users.FindAsync(otherId);
            target!.Status.Should().Be("Deactivated");
            target.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            other!.Status.Should().Be("Active");
            other.UpdatedAt.Should().BeCloseTo(nowMinus, TimeSpan.FromSeconds(1));
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAdminUser_ShouldDeactivateSuccessfully()
        {
            var userId = 7L;
            _context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Username = "admin",
                Email = "a@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Ad",
                LastName = "Min",
                Role = "Admin",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await _context.SaveChangesAsync();

            var result = await _handler.Handle(new DeactivateUserCommand { UserId = userId }, CancellationToken.None);

            result.Should().BeTrue();
            var updated = await _context.Users.FindAsync(userId);
            updated!.Status.Should().Be("Deactivated");
            updated.Role.Should().Be("Admin");
            _mockCacheInvalidationService.Verify(x => x.InvalidateUserCacheAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenCacheInvalidationFails_ShouldThrowException()
        {
            var userId = 8L;
            _context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Username = "cachefail",
                Email = "cf@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Cache",
                LastName = "Fail",
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await _context.SaveChangesAsync();

            _mockCacheInvalidationService
                .Setup(x => x.InvalidateUserCacheAsync())
                .ThrowsAsync(new Exception("Cache service unavailable"));

            await Assert.ThrowsAsync<Exception>(
                () => _handler.Handle(new DeactivateUserCommand { UserId = userId }, CancellationToken.None));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}






