using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Application.Features.Cart.Models;
using Ecommerce.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize]
    public class CartController : BaseController
    {
        private readonly IMediator _mediator;

        public CartController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<CartItemDto>>> GetCart()
        {
            try
            {
                var result = await _mediator.Send(new GetMyCartQuery());
                return SuccessResponse(result, "Success", "Cart retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve your cart at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AddToCartResult>> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var result = await _mediator.Send(new AddToCartCommand { ProductId = request.ProductId, Quantity = request.Quantity });
                
                if (result.Success)
                {
                    return SuccessResponse(result, "Success", result.Message);
                }
                else
                {
                    return ErrorResponse<AddToCartResult>(result.Message, result.Message);
                }
            }
            catch (Exception ex)
            {
                return HandleException<AddToCartResult>(ex, "Unable to add item to cart. Please check the product availability and try again. If the issue persists, contact support.");
            }
        }

        [HttpPut]
        public async Task<ActionResult> UpdateCart([FromBody] UpdateCartRequest request)
        {
            try
            {
                await _mediator.Send(new UpdateCartCommand { ProductId = request.ProductId, Quantity = request.Quantity });
                return SuccessResponse("Success", "Cart updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to update cart. Please check your request and try again. If the issue persists, contact support.");
            }
        }

        [HttpDelete("{productId}")]
        public async Task<ActionResult> DeleteFromCart([FromRoute] int productId)
        {
            try
            {
                await _mediator.Send(new DeleteCartItemCommand { ProductId = productId });
                return SuccessResponse("Success", "Item removed from cart successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to remove item from cart. Please try again or contact support if the issue persists.");
            }
        }

        [HttpDelete("clear")]
        public async Task<ActionResult> ClearCart()
        {
            try
            {
                await _mediator.Send(new ClearCartCommand());
                return SuccessResponse("Success", "Cart cleared successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to clear cart. Please try again or contact support if the issue persists.");
            }
        }
    }
}
