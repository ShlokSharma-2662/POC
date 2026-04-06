using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products;
using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Serilog;
using System.IO;
using System.Linq;

namespace Ecommerce.ProductService.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;
        private readonly IEventGridPublisherService _eventGridPublisher;
 
        public ProductsController(IMediator mediator, IWebHostEnvironment env, IEventGridPublisherService eventGridPublisher)
        {
            _mediator = mediator;
            _env = env;
            _eventGridPublisher = eventGridPublisher;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductViewModel>>> GetProducts([FromQuery] GetProductsQuery query)
        {
            try
            {
                if (query.PageNumber <= 0)
                {
                    return ValidationErrorResponse("Page number must be greater than 0.");
                }

                if (query.PageSize <= 0)
                {
                    return ValidationErrorResponse("Page size must be greater than 0.");
                }

                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Products retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve products at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductViewModel>> GetProductById(int id)
        {
            try
            {
                var result = await _mediator.Send(new GetProductByIdQuery { ProductId = id });
                if (result == null)
                {
                    return NotFoundResponse<ProductViewModel>("Product not found");
                }

                return SuccessResponse(result, "Success", "Product retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<ProductViewModel>(ex, "Unable to retrieve the requested product. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("odata")]
        [Authorize(Policy = "CustomerOrAdmin")]
        [EnableQuery(PageSize = 100)]
        public IActionResult GetProductsOData([FromServices] AppDbContext dbContext)
        {
            var query = dbContext.Products
                .Where(product => !product.IsDeleted)
                .Select(ProductMappings.ToViewModelExpression);

            return Ok(query);
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
                return HandleException(ex, "Unable to retrieve admin products. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> Create([FromBody] CreateProductCommand command)
        {
            try
            {
                var id = await _mediator.Send(command);
                await _eventGridPublisher.PublishProductUpdatedAsync(id, command.Name, "Created");
                return SuccessResponse(new { productId = id }, "Success", "Product created successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product creation failed. Please check your product information and try again. If the issue persists, contact support.");
            }
        }

        [HttpPost("with-image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> CreateWithImage([FromForm] CreateProductWithImageRequest request)
        {
            try
            {
                var imageUrl = await SaveImageIfProvided(request.Image);
                var command = new CreateProductCommand
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    ImageUrl = imageUrl ?? string.Empty
                };
                var id = await _mediator.Send(command);
                await _eventGridPublisher.PublishProductUpdatedAsync(id, command.Name, "Created");
                return SuccessResponse(new { productId = id }, "Success", "Product created successfully with image");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product creation with image failed. Please ensure the image is valid and try again. If the issue persists, contact support.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> Update(int id, [FromBody] UpdateProductCommand command)
        {
            if (id != command.ProductId)
            {
                return ValidationErrorResponse("Mismatched product id");
            }

            try
            {
                var ok = await _mediator.Send(command);
                if (!ok)
                {
                    return NotFoundResponse("Product not found");
                }
 
                await _eventGridPublisher.PublishProductUpdatedAsync(command.ProductId, command.Name, "Updated");
                return SuccessResponse(new { success = true }, "Success", "Product updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product update failed. Please check your product information and try again. If the issue persists, contact support.");
            }
        }

        [HttpPut("{id}/with-image")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> UpdateWithImage(int id, [FromForm] UpdateProductWithImageRequest request)
        {
            if (id != request.ProductId)
            {
                return ValidationErrorResponse("Mismatched product id");
            }

            try
            {
                var imageUrl = await SaveImageIfProvided(request.Image);
                var command = new UpdateProductCommand
                {
                    ProductId = request.ProductId,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    CategoryId = request.CategoryId,
                    ImageUrl = imageUrl ?? request.ImageUrl ?? string.Empty
                };
                var ok = await _mediator.Send(command);
                if (!ok)
                {
                    return NotFoundResponse("Product not found");
                }
 
                await _eventGridPublisher.PublishProductUpdatedAsync(command.ProductId, command.Name, "Updated");
                return SuccessResponse(new { success = true }, "Success", "Product updated successfully with image");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product update with image failed. Please ensure the image is valid and try again. If the issue persists, contact support.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> SoftDelete(int id)
        {
            try
            {
                var ok = await _mediator.Send(new DeleteProductCommand { ProductId = id });
                if (!ok)
                {
                    return NotFoundResponse("Product not found");
                }
 
                await _eventGridPublisher.PublishProductUpdatedAsync(id, "DeletedProduct", "Deleted");
                return SuccessResponse(new { success = true }, "Success", "Product deleted successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product deletion failed. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> Restore(int id)
        {
            try
            {
                var ok = await _mediator.Send(new DeleteProductCommand { ProductId = id, Restore = true });
                if (!ok)
                {
                    return NotFoundResponse("Product not found");
                }

                return SuccessResponse(new { success = true }, "Success", "Product restored successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Product restoration failed. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("image/{fileName}")]
        [AllowAnonymous]
        public IActionResult GetImage(string fileName)
        {
            try
            {
                var uploadsDir = System.IO.Path.Combine(_env.ContentRootPath, "Uploads", "products");
                var filePath = System.IO.Path.Combine(uploadsDir, fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var contentType = GetContentType(filePath);
                var stream = System.IO.File.OpenRead(filePath);
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error serving product image");
                return StatusCode(500, "Unable to retrieve the requested image. Please try again or contact support if the issue persists.");
            }
        }

        private async Task<string?> SaveImageIfProvided(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = System.IO.Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                throw new InvalidOperationException("Unsupported image type");
            }

            var uploadsDir = System.IO.Path.Combine(_env.ContentRootPath, "Uploads", "products");
            System.IO.Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = System.IO.Path.Combine(uploadsDir, fileName);
            await using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                await image.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/api/products/image/{fileName}";
        }

        private static string GetContentType(string path)
        {
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
