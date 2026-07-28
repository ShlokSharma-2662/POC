using Ecommerce.API.Controllers;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Application.Features.Metrics.Commands;
using Ecommerce.Application.Features.Metrics.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using HotChocolate.Subscriptions;

namespace Ecommerce.Tests.Controllers
{
    public class AdminControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<ITopicEventSender> _mockTopicEventSender;
        private readonly Mock<IEventGridPublisherService> _mockEventGridPublisher;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockTopicEventSender = new Mock<ITopicEventSender>();
            _mockEventGridPublisher = new Mock<IEventGridPublisherService>();
            _controller = new AdminController(_mockMediator.Object, _mockEventGridPublisher.Object);
            SetupHttpContext();
        }

        private void AssertSuccessResponse<T>(ActionResult<T> result, int expectedStatusCode = 200)
        {
            result.Should().BeAssignableTo<ActionResult<T>>();
            var actionResult = result as ActionResult<T>;
            actionResult.Should().NotBeNull();
            
            if (actionResult!.Value != null)
            {
                actionResult.Value.Should().NotBeNull();
            }
            else if (actionResult.Result != null)
            {
                actionResult.Result.Should().BeOfType<OkObjectResult>();
                var okResult = actionResult.Result as OkObjectResult;
                okResult!.StatusCode.Should().Be(expectedStatusCode);
            }
            else
            {
                Assert.Fail("ActionResult should have either Value or Result set");
            }
        }

        [Fact]
        public async Task GetAllOrders_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetAllOrdersForAdminQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<AdminOrderDto>
            {
                Items = new List<AdminOrderDto>(),
                TotalCount = 0
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAllOrders(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<AdminOrderDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetAllOrders_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetAllOrdersForAdminQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllOrders(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<AdminOrderDto>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateStatus_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var orderId = Guid.NewGuid();
            var dto = new UpdateStatusDto
            {
                Status = "Shipped"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<UpdateOrderStatusCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStatus(orderId, dto, _mockTopicEventSender.Object);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task UpdateStatus_WithNullDto_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var orderId = Guid.NewGuid();

            // Act
            var result = await _controller.UpdateStatus(orderId, null, _mockTopicEventSender.Object);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateStatus_WithEmptyStatus_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var orderId = Guid.NewGuid();
            var dto = new UpdateStatusDto
            {
                Status = ""
            };

            // Act
            var result = await _controller.UpdateStatus(orderId, dto, _mockTopicEventSender.Object);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task UpdateStatus_WithOrderNotFound_ReturnsNotFoundResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var orderId = Guid.NewGuid();
            var dto = new UpdateStatusDto
            {
                Status = "Shipped"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<UpdateOrderStatusCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateStatus(orderId, dto, _mockTopicEventSender.Object);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetSystemMetrics_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetSystemMetricsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<SystemMetric>
            {
                Items = new List<SystemMetric>(),
                TotalCount = 0
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetSystemMetrics(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<SystemMetric>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task LogMetric_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new LogMetricCommand
            {
                Endpoint = "/api/test",
                ResponseTimeMs = 100
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.LogMetric(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public void LogMetric_ExposesLegacyAndMetricsRoutes()
        {
            var method = typeof(AdminController).GetMethod(nameof(AdminController.LogMetric));

            var routeTemplates = method!
                .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
                .Cast<HttpPostAttribute>()
                .Select(attribute => attribute.Template);

            routeTemplates.Should().Contain("log");
            routeTemplates.Should().Contain("metrics/log");
        }

        [Fact]
        public async Task LogMetric_WithNullCommand_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.LogMetric(null);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetErrorLogs_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetAllErrorLogsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<ErrorLog>
            {
                Items = new List<ErrorLog>(),
                TotalCount = 0
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetErrorLogs(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<ErrorLog>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUsers_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetAllUsersQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<AdminUserDto>
            {
                Items = new List<AdminUserDto>(),
                TotalCount = 0
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetUsers(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<AdminUserDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUserById_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var userId = 1L;
            var expectedResult = new AdminUserDto
            {
                Id = userId,
                Email = "test@example.com"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            result.Should().BeOfType<ActionResult<AdminUserDto>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUserById_WithUserNotFound_ReturnsNotFoundResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var userId = 1L;

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AdminUserDto)null);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            result.Should().BeOfType<ActionResult<AdminUserDto>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task AssignRole_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new AssignRoleDto
            {
                UserId = 1,
                Role = "Admin"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AssignUserRoleCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.AssignRole(dto);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task AssignRole_WithInvalidData_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new AssignRoleDto
            {
                UserId = 0,
                Role = ""
            };

            // Act
            var result = await _controller.AssignRole(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task AssignRole_WithUserNotFound_ReturnsNotFoundResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new AssignRoleDto
            {
                UserId = 1,
                Role = "Admin"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AssignUserRoleCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.AssignRole(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task ResetPassword_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new ResetPasswordDto
            {
                UserId = 1,
                NewPassword = "NewPassword123!"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<ResetUserPasswordCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.ResetPassword(dto);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidData_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new ResetPasswordDto
            {
                UserId = 0,
                NewPassword = ""
            };

            // Act
            var result = await _controller.ResetPassword(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Deactivate_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new DeactivateUserDto
            {
                UserId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<DeactivateUserCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.Deactivate(dto);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task Activate_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var dto = new ActivateUserDto
            {
                UserId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<ActivateUserCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.Activate(dto);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task GetRevenueReport_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetRevenueReportQuery
            {
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now
            };

            var expectedResult = new RevenueReportDto
            {
                TotalRevenue = 1000
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetRevenueReport(query);

            // Assert
            result.Should().BeOfType<ActionResult<RevenueReportDto>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetRevenueSummary_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetRevenueSummaryQuery();

            var expectedResult = new RevenueSummaryDto
            {
                TotalRevenue = 1000
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetRevenueSummary(query);

            // Assert
            result.Should().BeOfType<ActionResult<RevenueSummaryDto>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }
    }
}
