using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Features.Products.Handlers
{
    public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<CreateProductHandler> _logger;

        public CreateProductHandler(AppDbContext context, IEmailService emailService, ICacheInvalidationService cacheInvalidationService, ILogger<CreateProductHandler> logger)
        {
            _context = context;
            _emailService = emailService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                Stock = request.Stock,
                CategoryId = request.CategoryId,
                IsActive = true,
                IsDeleted = false
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            var category = await _context.Categories.FindAsync(request.CategoryId);
            var categoryName = category?.Name ?? "General";

            try
            {
                await _emailService.SendProductNotificationToAllUsersAsync(
                    product.Name,
                    product.Description,
                    product.Price,
                    product.ImageUrl,
                    categoryName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send product notification email for product {ProductName}. Product creation will continue.", product.Name);
            }
            await _cacheInvalidationService.InvalidateProductCacheAsync();
            await _cacheInvalidationService.InvalidateCategoryCacheAsync();

            return product.ProductId;
        }
    }
}
