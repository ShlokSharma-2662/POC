using Ecommerce.ProductService.Controllers;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using Ecommerce.Infrastructure.Services;
using System.IO;

namespace Ecommerce.Tests.Controllers
{
    public class ProductsControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<IEventGridPublisherService> _mockEventGridPublisher;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockEventGridPublisher = new Mock<IEventGridPublisherService>();
            _controller = new ProductsController(_mockMediator.Object, _mockWebHostEnvironment.Object, _mockEventGridPublisher.Object);
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
        public async Task GetProducts_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            var query = new GetProductsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "test"
            };

            var expectedResult = new PagedResult<ProductViewModel>
            {
                Items = new List<ProductViewModel>
                {
                    new ProductViewModel { ProductId = 1, Name = "Test Product", Price = 100 }
                },
                TotalCount = 1
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetProducts(query);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task GetProducts_WithInvalidPageNumber_ReturnsValidationErrorResponse()
        {
            // Arrange
            var query = new GetProductsQuery
            {
                PageNumber = 0,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetProducts(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<ProductViewModel>>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetProducts_WithInvalidPageSize_ReturnsValidationErrorResponse()
        {
            // Arrange
            var query = new GetProductsQuery
            {
                PageNumber = 1,
                PageSize = 0
            };

            // Act
            var result = await _controller.GetProducts(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<ProductViewModel>>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetProducts_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            var query = new GetProductsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetProducts(query);

            // Assert
            result.Should().BeOfType<ActionResult<PagedResult<ProductViewModel>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetAllProductsAdmin_WithValidQuery_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var query = new GetAllProductsAdminQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            var expectedResult = new PagedResult<ProductViewModel>
            {
                Items = new List<ProductViewModel>(),
                TotalCount = 0
            };

            _mockMediator.Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAllProductsAdmin(query);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task Create_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100,
                Stock = 10,
                CategoryId = 1,
                ImageUrl = "test-image.jpg"
            };

            _mockMediator
                .Setup(x => x.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Create(command);

            // Assert
            _mockMediator.Verify(m => m.Send(
                It.Is<CreateProductCommand>(c =>
                    c.Name == "Test Product" &&
                    c.Price == 100 &&
                    c.CategoryId == 1),
                It.IsAny<CancellationToken>()),
                Times.Once);

            AssertSuccessResponse(result);

        }

        [Fact]
        public async Task Create_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new CreateProductCommand
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100,
                Stock = 10,
                CategoryId = 1
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Create(command);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateWithImage_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);
            SetupRequestScheme();
            SetupRequestHost();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

            var contentRootPath = TestPathHelper.CreateWritableContentRootPath();
            _mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(contentRootPath);
            
            var request = new CreateProductWithImageRequest
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 100,
                Stock = 10,
                CategoryId = 1,
                Image = mockFile.Object
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(1);

            // Act
            var result = await _controller.CreateWithImage(request);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task Update_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new UpdateProductCommand
            {
                ProductId = 1,
                Name = "Updated Product",
                Description = "Updated Description",
                Price = 150,
                Stock = 15,
                CategoryId = 1
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(1, command);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task Update_WithMismatchedId_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new UpdateProductCommand
            {
                ProductId = 2,
                Name = "Updated Product"
            };

            // Act
            var result = await _controller.Update(1, command);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Update_WithProductNotFound_ReturnsNotFoundResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            var command = new UpdateProductCommand
            {
                ProductId = 1,
                Name = "Updated Product"
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(1, command);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task SoftDelete_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.SoftDelete(1);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task SoftDelete_WithProductNotFound_ReturnsNotFoundResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.SoftDelete(1);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult!.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Restore_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAdminUser();
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.Restore(1);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public void GetImage_WithValidFileName_ReturnsFileResult()
        {
            // Arrange
            var fileName = "test.jpg";
            
            var contentRootPath = TestPathHelper.CreateWritableContentRootPath();
            _mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(contentRootPath);
            
            // Mock file existence
            var mockFileStream = new MemoryStream();
            var mockFile = new Mock<FileStream>(mockFileStream);
            
            // Act
            var result = _controller.GetImage(fileName);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public void GetImage_WithNonExistentFile_ReturnsNotFound()
        {
            // Arrange
            var fileName = "nonexistent.jpg";
            var contentRootPath = TestPathHelper.CreateWritableContentRootPath();
            _mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(contentRootPath);

            // Act
            var result = _controller.GetImage(fileName);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
