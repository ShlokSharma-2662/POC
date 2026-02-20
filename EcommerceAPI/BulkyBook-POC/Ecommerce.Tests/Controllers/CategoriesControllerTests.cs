using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Categories.Models;
using Ecommerce.Application.Features.Categories.Queries;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Ecommerce.Tests.Controllers
{
    public class CategoriesControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new CategoriesController(_mockMediator.Object);
            SetupHttpContext();
        }

        [Fact]
        public async Task GetCategories_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResult = new List<Category>
            {
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Clothing" },
                new Category { Id = 3, Name = "Books" }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeOfType<ActionResult<List<Category>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetCategories_WithEmptyResult_ReturnsSuccessResponse()
        {
            // Arrange
            var expectedResult = new List<Category>();

            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeOfType<ActionResult<List<Category>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetCategories_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeOfType<ActionResult<List<Category>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetCategories_WithNullResult_ReturnsSuccessResponse()
        {
            // Arrange
            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((List<Category>)null);

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeOfType<ActionResult<List<Category>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetCategories_WithTimeoutException_ReturnsExceptionResponse()
        {
            // Arrange
            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new TimeoutException("Request timed out"));

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeOfType<ActionResult<List<Category>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetCategories_WithInvalidOperationException_ReturnsExceptionResponse()
        {
            // Arrange
            _mockMediator.Setup(x => x.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.GetCategories();

            // Assert
            result.Should().BeAssignableTo<ActionResult<List<Category>>>();
            if (result.Result != null)
            {
                result.Result.Should().BeOfType<ObjectResult>();
                var statusResult = result.Result as ObjectResult;
                statusResult!.StatusCode.Should().Be(409);
            }
            else if (result.Value != null)
            {
                // If it's stored in Value, we need different handling
                Assert.Fail("Expected exception response in Result, but got value in Value");
            }
        }
    }
}

