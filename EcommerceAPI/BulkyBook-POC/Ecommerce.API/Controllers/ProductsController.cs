using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;

        public ProductsController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductViewModel>>> GetProducts([FromQuery] GetProductsQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Products retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<PagedResult<ProductViewModel>>(ex, "Unable to retrieve products at this time.");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductViewModel>> GetProductById(int id)
        {
            try
            {
                var result = await _mediator.Send(new GetProductByIdQuery { ProductId = id });
                if (result == null)
                    return NotFoundResponse<ProductViewModel>("Product not found");
                return SuccessResponse(result, "Success", "Product retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<ProductViewModel>(ex, "Unable to retrieve product.");
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<ProductViewModel>>> GetAllProductsAdmin([FromQuery] GetAllProductsAdminQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Admin products retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<PagedResult<ProductViewModel>>(ex, "Unable to retrieve admin products.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> CreateProduct([FromBody] CreateProductCommand command)
        {
            try
            {
                var productId = await _mediator.Send(command);
                return SuccessResponse(productId, "Success", "Product created successfully");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "Unable to create product.");
            }
        }

        [HttpPost("with-image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> CreateProductWithImage([FromForm] CreateProductWithImageRequest request)
        {
            try
            {
                string? imageUrl = null;
                if (request.Image != null && request.Image.Length > 0)
                {
                    imageUrl = await SaveImageAsync(request.Image);
                }

                var command = new CreateProductCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    ImageUrl = imageUrl ?? string.Empty
                };

                var productId = await _mediator.Send(command);
                return SuccessResponse(productId, "Success", "Product created successfully");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "Unable to create product with image.");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> UpdateProduct(int id, [FromBody] UpdateProductCommand command)
        {
            try
            {
                command.ProductId = id;
                var result = await _mediator.Send(command);
                if (!result)
                    return NotFoundResponse<bool>("Product not found");
                return SuccessResponse(result, "Success", "Product updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to update product.");
            }
        }

        [HttpPut("{id:int}/with-image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> UpdateProductWithImage(int id, [FromForm] UpdateProductWithImageRequest request)
        {
            try
            {
                string? imageUrl = request.ImageUrl;
                if (request.Image != null && request.Image.Length > 0)
                {
                    imageUrl = await SaveImageAsync(request.Image);
                }

                var command = new UpdateProductCommand
                {
                    ProductId = id,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    ImageUrl = imageUrl ?? string.Empty
                };

                var result = await _mediator.Send(command);
                if (!result)
                    return NotFoundResponse<bool>("Product not found");
                return SuccessResponse(result, "Success", "Product updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to update product with image.");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> SoftDeleteProduct(int id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteProductCommand { ProductId = id });
                if (!result)
                    return NotFoundResponse<bool>("Product not found");
                return SuccessResponse(result, "Success", "Product deleted successfully");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to delete product.");
            }
        }

        [HttpPost("{id:int}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<bool>> RestoreProduct(int id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteProductCommand { ProductId = id, Restore = true });
                if (!result)
                    return NotFoundResponse<bool>("Product not found");
                return SuccessResponse(result, "Success", "Product restored successfully");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to restore product.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = System.IO.Path.Combine(_env.WebRootPath ?? System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "products");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{Guid.NewGuid()}{System.IO.Path.GetExtension(image.FileName)}";
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
            await using var stream = System.IO.File.Create(filePath);
            await image.CopyToAsync(stream);
            return $"/images/products/{uniqueFileName}";
        }
    }
}
