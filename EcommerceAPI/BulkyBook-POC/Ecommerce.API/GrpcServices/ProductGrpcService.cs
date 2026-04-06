using Grpc.Core;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Contracts.Products;

namespace Ecommerce.API.GrpcServices;

public class ProductGrpcService : ProductService.ProductServiceBase
{
    private readonly AppDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<ProductGrpcService> _logger;

    public ProductGrpcService(
        AppDbContext context,
        IMediator mediator,
        ILogger<ProductGrpcService> logger)
    {
        _context = context;
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetProduct called for ID: {ProductId}", request.ProductId);

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && !p.IsDeleted);

        if (product == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.ProductId} not found"));
        }

        return MapToResponse(product);
    }

    public override async Task<StockCheckResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC CheckStock called for ProductId: {ProductId}, Quantity: {Quantity}",
            request.ProductId, request.Quantity);

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && !p.IsDeleted);

        if (product == null)
        {
            return new StockCheckResponse
            {
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = "Product not found"
            };
        }

        var isAvailable = product.Stock >= request.Quantity;

        return new StockCheckResponse
        {
            IsAvailable = isAvailable,
            AvailableQuantity = product.Stock,
            Message = isAvailable ? "Stock available" : "Insufficient stock"
        };
    }

    public override async Task<StockUpdateResponse> UpdateStock(UpdateStockRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC UpdateStock called for ProductId: {ProductId}, Change: {Quantity}",
            request.ProductId, request.QuantityChange);

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && !p.IsDeleted);

        if (product == null)
        {
            return new StockUpdateResponse
            {
                Success = false,
                NewQuantity = 0,
                Message = "Product not found"
            };
        }

        if (request.IsIncrease)
        {
            product.Stock += request.QuantityChange;
        }
        else
        {
            if (product.Stock < request.QuantityChange)
            {
                return new StockUpdateResponse
                {
                    Success = false,
                    NewQuantity = product.Stock,
                    Message = "Insufficient stock"
                };
            }
            product.Stock -= request.QuantityChange;
        }

        await _context.SaveChangesAsync();

        return new StockUpdateResponse
        {
            Success = true,
            NewQuantity = product.Stock,
            Message = "Stock updated successfully"
        };
    }

    public override async Task<ProductsResponse> GetProductsByIds(GetProductsByIdsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetProductsByIds called for {Count} products", request.ProductIds.Count);

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => request.ProductIds.Contains(p.ProductId) && !p.IsDeleted)
            .ToListAsync();

        var response = new ProductsResponse();
        response.Products.AddRange(products.Select(MapToResponse));

        return response;
    }

    private static ProductResponse MapToResponse(Ecommerce.Domain.Entities.Product product)
    {
        return new ProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description ?? string.Empty,
            Price = (double)product.Price,
            StockQuantity = product.Stock,
            StockStatus = product.Stock > 0 ? "InStock" : "OutOfStock",
            CategoryName = product.Category?.Name ?? string.Empty,
            ImageUrl = product.ImageUrl ?? string.Empty,
            IsAvailable = product.Stock > 0
        };
    }
}
