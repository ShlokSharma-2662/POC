using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Wishlist.Commands;
using Ecommerce.Application.Features.Wishlist.Models;
using Ecommerce.Application.Features.Wishlist.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : BaseController
    {
        private readonly IMediator _mediator;

        public WishlistController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<WishlistItemDto>>> GetUserWishlist()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<List<WishlistItemDto>>("Invalid user token");

                var query = new GetUserWishlistQuery { UserId = userId };
                var wishlistItems = await _mediator.Send(query);

                return SuccessResponse(wishlistItems, "Success", "Wishlist retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<List<WishlistItemDto>>(ex, "Unable to retrieve your wishlist at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost("add")]
        public async Task<ActionResult<bool>> AddToWishlist([FromBody] AddToWishlistRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<bool>("Invalid user token");

                var command = new AddToWishlistCommand
                {
                    ProductId = request.ProductId,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (result)
                    return SuccessResponse(true, "Success", "Product added to wishlist successfully");
                else
                    return ValidationErrorResponse<bool>("Failed to add product to wishlist");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to add product to wishlist. Please check the product availability and try again. If the issue persists, contact support.");
            }
        }

        [HttpDelete("remove")]
        public async Task<ActionResult<bool>> RemoveFromWishlist([FromBody] RemoveFromWishlistRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<bool>("Invalid user token");

                var command = new RemoveFromWishlistCommand
                {
                    ProductId = request.ProductId,
                    UserId = userId
                };

                var result = await _mediator.Send(command);

                if (result)
                    return SuccessResponse(true, "Success", "Product removed from wishlist successfully");
                else
                    return ValidationErrorResponse<bool>("Failed to remove product from wishlist");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to remove product from wishlist. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("check/{productId}")]
        public async Task<ActionResult<bool>> CheckWishlistStatus(int productId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<bool>("Invalid user token");

                var query = new CheckWishlistStatusQuery
                {
                    ProductId = productId,
                    UserId = userId
                };

                var isInWishlist = await _mediator.Send(query);
                return SuccessResponse(isInWishlist, "Success", "Wishlist status checked successfully");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to check wishlist status. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("count")]
        public async Task<ActionResult<int>> GetWishlistCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<int>("Invalid user token");

                var query = new GetUserWishlistQuery { UserId = userId };
                var wishlistItems = await _mediator.Send(query);

                return SuccessResponse(wishlistItems.Count, "Success", "Wishlist count retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "Unable to get wishlist count. Please try again or contact support if the issue persists.");
            }
        }

        [HttpDelete("clear")]
        public async Task<ActionResult<bool>> ClearWishlist()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<bool>("Invalid user token");

                var command = new ClearWishlistCommand { UserId = userId };
                var result = await _mediator.Send(command);

                if (result)
                    return SuccessResponse(true, "Success", "Wishlist cleared successfully");
                else
                    return ValidationErrorResponse<bool>("Failed to clear wishlist");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "Unable to clear wishlist. Please try again or contact support if the issue persists.");
            }
        }
    }
}
