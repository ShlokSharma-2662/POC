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
    public class GetAllErrorLogsQueryHandlerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly GetAllErrorLogsQueryHandler _handler;

        public GetAllErrorLogsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockCacheService = new Mock<ICacheService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup configuration to return cache disabled by default
            _mockConfiguration.Setup(x => x["Redis:IsCacheEnabled"]).Returns("false");
            
            _handler = new GetAllErrorLogsQueryHandler(_context, _mockCacheService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Handle_WithNoFilters_ShouldReturnAllErrorLogs()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Test error 1",
                    Severity = "Error",
                    Path = "/api/test",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    UserAgent = "Mozilla/5.0",
                    IpAddress = "192.168.1.1"
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Test error 2",
                    Severity = "Warning",
                    Path = "/api/test2",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    UserAgent = "Chrome/91.0",
                    IpAddress = "192.168.1.2"
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().BeInDescendingOrder(x => x.Timestamp);
        }

        [Fact]
        public async Task Handle_WithSeverityFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Error message",
                    Severity = "Error",
                    Path = "/api/test",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Warning message",
                    Severity = "Warning",
                    Path = "/api/test2",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                },
                new ErrorLog
                {
                    Id = 3,
                    Message = "Another error",
                    Severity = "Error",
                    Path = "/api/test3",
                    Timestamp = DateTime.UtcNow.AddHours(-3)
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SeverityFilter = "Error" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(x => x.Severity == "Error");
        }

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnMatchingResults()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Database connection failed",
                    Severity = "Error",
                    Path = "/api/users",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    UserAgent = "Mozilla/5.0",
                    IpAddress = "192.168.1.1"
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Authentication error",
                    Severity = "Error",
                    Path = "/api/auth",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    UserAgent = "Chrome/91.0",
                    IpAddress = "192.168.1.2"
                },
                new ErrorLog
                {
                    Id = 3,
                    Message = "Payment processing failed",
                    Severity = "Error",
                    Path = "/api/payment",
                    Timestamp = DateTime.UtcNow.AddHours(-3),
                    UserAgent = "Safari/14.0",
                    IpAddress = "192.168.1.3"
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = "database" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Message.Should().Contain("Database");
        }

        [Fact]
        public async Task Handle_WithSearchTermInPath_ShouldReturnMatchingResults()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Some error",
                    Severity = "Error",
                    Path = "/api/users/profile",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Another error",
                    Severity = "Error",
                    Path = "/api/products/list",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = "users" 
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.Items.First().Path.Should().Contain("users");
        }

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var errorLogs = Enumerable.Range(1, 25).Select(i => new ErrorLog
            {
                Id = i,
                Message = $"Error {i}",
                Severity = "Error",
                Path = $"/api/test{i}",
                Timestamp = DateTime.UtcNow.AddHours(-i)
            }).ToList();

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery { PageNumber = 2, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(25);
            result.Items.First().Id.Should().Be(11); // Second page should start from item 11
        }

        [Fact]
        public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyResult()
        {
            // Arrange
            var query = new GetAllErrorLogsQuery { PageNumber = 1, PageSize = 10 };

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
            var errorLog = new ErrorLog
            {
                Id = 1,
                Message = "Test error",
                Severity = "Error",
                Path = "/api/test",
                Timestamp = DateTime.UtcNow
            };

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var query = new GetAllErrorLogsQuery { PageNumber = 1, PageSize = 10 };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(query, cts.Token));
        }

        [Fact]
        public async Task Handle_WithCombinedFilters_ShouldReturnCorrectResults()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Database connection failed",
                    Severity = "Error",
                    Path = "/api/users",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    UserAgent = "Mozilla/5.0",
                    IpAddress = "192.168.1.1"
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Database timeout",
                    Severity = "Error",
                    Path = "/api/products",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    UserAgent = "Chrome/91.0",
                    IpAddress = "192.168.1.2"
                },
                new ErrorLog
                {
                    Id = 3,
                    Message = "Authentication failed",
                    Severity = "Warning",
                    Path = "/api/auth",
                    Timestamp = DateTime.UtcNow.AddHours(-3),
                    UserAgent = "Safari/14.0",
                    IpAddress = "192.168.1.3"
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SeverityFilter = "Error",
                SearchTerm = "database"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(x => x.Severity == "Error");
            result.Items.Should().OnlyContain(x => x.Message.ToLower().Contains("database"));
        }

        [Fact]
        public async Task Handle_WithNullSearchTerm_ShouldNotFilterBySearch()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Test error 1",
                    Severity = "Error",
                    Path = "/api/test1",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Test error 2",
                    Severity = "Warning",
                    Path = "/api/test2",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = null
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WithEmptySearchTerm_ShouldNotFilterBySearch()
        {
            // Arrange
            var errorLogs = new List<ErrorLog>
            {
                new ErrorLog
                {
                    Id = 1,
                    Message = "Test error 1",
                    Severity = "Error",
                    Path = "/api/test1",
                    Timestamp = DateTime.UtcNow.AddHours(-1)
                },
                new ErrorLog
                {
                    Id = 2,
                    Message = "Test error 2",
                    Severity = "Warning",
                    Path = "/api/test2",
                    Timestamp = DateTime.UtcNow.AddHours(-2)
                }
            };

            _context.ErrorLogs.AddRange(errorLogs);
            await _context.SaveChangesAsync();

            var query = new GetAllErrorLogsQuery 
            { 
                PageNumber = 1, 
                PageSize = 10, 
                SearchTerm = ""
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
