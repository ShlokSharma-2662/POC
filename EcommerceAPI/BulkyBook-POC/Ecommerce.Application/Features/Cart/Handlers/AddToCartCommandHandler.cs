using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Application.Features.Cart.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CartEntity = Ecommerce.Domain.Entities.Cart;
using CartItemEntity = Ecommerce.Domain.Entities.CartItem;

namespace Ecommerce.Application.Features.Cart.Handlers
{
    public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, AddToCartResult>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddToCartCommandHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AddToCartResult> Handle(AddToCartCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                         ?? throw new UnauthorizedAccessException();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);
            if (product == null)
            {
                return new AddToCartResult
                {
                    Success = false,
                    Message = "Product not found"
                };
            }

            var cart = await _context.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (cart == null)
            {
                cart = new CartEntity { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Determine requested add quantity (at least 1)
            var addQuantity = Math.Max(1, request.Quantity);

            // Current quantity for this product in cart
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            var currentQty = existing?.Quantity ?? 0;

            // Validate against available stock
            if (currentQty + addQuantity > product.Stock)
            {
                var maxAddable = Math.Max(0, product.Stock - currentQty);
                return new AddToCartResult
                {
                    Success = false,
                    Message = maxAddable > 0
                        ? $"Only {maxAddable} units available to add right now."
                        : "This item is out of stock or you've reached the available stock in your cart.",
                    MaxAddableQuantity = maxAddable,
                    CurrentStock = product.Stock,
                    RequestedQuantity = addQuantity,
                    CurrentCartQuantity = currentQty
                };
            }

            if (existing != null)
            {
                existing.Quantity = currentQty + addQuantity;
            }
            else
            {
                cart.Items.Add(new CartItemEntity
                {
                    ProductId = request.ProductId,
                    Quantity = addQuantity
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new AddToCartResult
            {
                Success = true,
                Message = "Item added to cart successfully",
                CurrentStock = product.Stock,
                RequestedQuantity = addQuantity,
                CurrentCartQuantity = currentQty + addQuantity
            };
        }
    }
}

