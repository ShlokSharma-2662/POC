using Ecommerce.API.Controllers;
using Ecommerce.Application.Common.Models;
using Ecommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;

namespace Ecommerce.Tests.Controllers
{
    public class BaseControllerTests : ControllerTestBase
    {
        private readonly TestController _controller;

        public BaseControllerTests()
        {
            _controller = new TestController();
            SetupHttpContext();
            SetupControllerContext(_controller);
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
        public void SuccessResponse_WithData_ReturnsOkResult()
        {
            // Arrange
            var data = new { message = "test" };
            var status = "Success";
            var statusReason = "Operation completed";

            // Act
            var result = _controller.TestSuccessResponse(data, status, statusReason);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public void SuccessResponse_WithoutData_ReturnsOkResult()
        {
            // Arrange
            var status = "Success";
            var statusReason = "Operation completed";

            // Act
            var result = _controller.TestSuccessResponse(status, statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var response = okResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeTrue();
            response.Status.Should().Be(status);
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ErrorResponse_WithData_ReturnsBadRequestResult()
        {
            // Arrange
            var status = "Error";
            var statusReason = "An error occurred";

            // Act
            var result = _controller.TestErrorResponse<object>(status, statusReason);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be(status);
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ErrorResponse_WithoutData_ReturnsBadRequestResult()
        {
            // Arrange
            var status = "Error";
            var statusReason = "An error occurred";

            // Act
            var result = _controller.TestErrorResponse(status, statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be(status);
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ExceptionResponse_WithData_ReturnsInternalServerErrorResult()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var statusReason = "Custom error message";

            // Act
            var result = _controller.TestExceptionResponse<object>(exception, statusReason);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);

            var response = statusResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Exception");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ExceptionResponse_WithoutData_ReturnsInternalServerErrorResult()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var statusReason = "Custom error message";

            // Act
            var result = _controller.TestExceptionResponse(exception, statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);

            var response = statusResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Exception");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ExceptionResponse_WithNullStatusReason_UsesExceptionMessage()
        {
            // Arrange
            var exception = new Exception("Test exception message");

            // Act
            var result = _controller.TestExceptionResponse<object>(exception, null);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);

            var response = statusResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Exception");
            response.StatusReason.Should().Be("Test exception message");
        }

        [Fact]
        public void ValidationErrorResponse_WithData_ReturnsBadRequestResult()
        {
            // Arrange
            var statusReason = "Validation failed";

            // Act
            var result = _controller.TestValidationErrorResponse<object>(statusReason);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("ValidationError");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void ValidationErrorResponse_WithoutData_ReturnsBadRequestResult()
        {
            // Arrange
            var statusReason = "Validation failed";

            // Act
            var result = _controller.TestValidationErrorResponse(statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("ValidationError");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void NotFoundResponse_WithData_ReturnsNotFoundResult()
        {
            // Arrange
            var statusReason = "Resource not found";

            // Act
            var result = _controller.TestNotFoundResponse<object>(statusReason);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("NotFound");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void NotFoundResponse_WithoutData_ReturnsNotFoundResult()
        {
            // Arrange
            var statusReason = "Resource not found";

            // Act
            var result = _controller.TestNotFoundResponse(statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);

            var response = notFoundResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("NotFound");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void UnauthorizedResponse_WithData_ReturnsUnauthorizedResult()
        {
            // Arrange
            var statusReason = "Access denied";

            // Act
            var result = _controller.TestUnauthorizedResponse<object>(statusReason);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);

            var response = unauthorizedResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Unauthorized");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void UnauthorizedResponse_WithoutData_ReturnsUnauthorizedResult()
        {
            // Arrange
            var statusReason = "Access denied";

            // Act
            var result = _controller.TestUnauthorizedResponse(statusReason);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);

            var response = unauthorizedResult.Value as ApiResponse;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Unauthorized");
            response.StatusReason.Should().Be(statusReason);
        }

        [Fact]
        public void HandleException_WithArgumentException_ReturnsValidationErrorResponse()
        {
            // Arrange
            var exception = new ArgumentException("Invalid argument");

            // Act
            var result = _controller.TestHandleException<object>(exception, "Custom message");

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("ValidationError");
            response.StatusReason.Should().Be("Custom message");
        }

        [Fact]
        public void HandleException_WithInvalidOperationException_ReturnsConflictResult()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid operation");

            // Act
            var result = _controller.TestHandleException<object>(exception, "Custom message");

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(409);

            var response = statusResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("ValidationError");
            response.StatusReason.Should().Be("Custom message");
        }

        [Fact]
        public void HandleException_WithGenericException_ReturnsExceptionResponse()
        {
            // Arrange
            var exception = new Exception("Generic exception");

            // Act
            var result = _controller.TestHandleException<object>(exception, "Custom message");

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);

            var response = statusResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("Exception");
            response.StatusReason.Should().Be("Custom message");
        }

        [Fact]
        public void HandleException_WithNullCustomMessage_UsesExceptionMessage()
        {
            // Arrange
            var exception = new ArgumentException("Exception message");

            // Act
            var result = _controller.TestHandleException<object>(exception, null);

            // Assert
            result.Should().BeAssignableTo<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);

            var response = badRequestResult.Value as ApiResponse<object>;
            response.Should().NotBeNull();
            response!.IsSuccessful.Should().BeFalse();
            response.Status.Should().Be("ValidationError");
            response.StatusReason.Should().Be("Exception message");
        }

        // Test controller class to expose protected methods
        public class TestController : BaseController
        {
            public ActionResult<T> TestSuccessResponse<T>(T data, string status, string statusReason)
            {
                return SuccessResponse(data, status, statusReason);
            }

            public ActionResult TestSuccessResponse(string status, string statusReason)
            {
                return SuccessResponse(status, statusReason);
            }

            public ActionResult<T> TestErrorResponse<T>(string status, string statusReason)
            {
                return ErrorResponse<T>(status, statusReason);
            }

            public ActionResult TestErrorResponse(string status, string statusReason)
            {
                return ErrorResponse(status, statusReason);
            }

            public ActionResult<T> TestExceptionResponse<T>(Exception ex, string statusReason)
            {
                return ExceptionResponse<T>(ex, statusReason);
            }

            public ActionResult TestExceptionResponse(Exception ex, string statusReason)
            {
                return ExceptionResponse(ex, statusReason);
            }

            public ActionResult<T> TestValidationErrorResponse<T>(string statusReason)
            {
                return ValidationErrorResponse<T>(statusReason);
            }

            public ActionResult TestValidationErrorResponse(string statusReason)
            {
                return ValidationErrorResponse(statusReason);
            }

            public ActionResult<T> TestNotFoundResponse<T>(string statusReason)
            {
                return NotFoundResponse<T>(statusReason);
            }

            public ActionResult TestNotFoundResponse(string statusReason)
            {
                return NotFoundResponse(statusReason);
            }

            public ActionResult<T> TestUnauthorizedResponse<T>(string statusReason)
            {
                return UnauthorizedResponse<T>(statusReason);
            }

            public ActionResult TestUnauthorizedResponse(string statusReason)
            {
                return UnauthorizedResponse(statusReason);
            }

            public ActionResult<T> TestHandleException<T>(Exception ex, string customMessage)
            {
                return HandleException<T>(ex, customMessage);
            }

            public ActionResult TestHandleException(Exception ex, string customMessage)
            {
                return HandleException(ex, customMessage);
            }
        }
    }
}

