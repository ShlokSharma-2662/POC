using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Categories.Models;
using Ecommerce.Application.Features.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : BaseController
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<Category>>> GetCategories()
        {
            try
            {
                var result = await _mediator.Send(new GetAllCategoriesQuery());
                return SuccessResponse(result, "Success", "Categories retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<List<Category>>(ex, "Unable to retrieve categories at this time. Please try again or contact support if the issue persists.");
            }
        }
    }
}
