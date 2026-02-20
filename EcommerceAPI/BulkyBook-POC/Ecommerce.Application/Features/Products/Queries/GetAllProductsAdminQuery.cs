using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Models;
using MediatR;

namespace Ecommerce.Application.Features.Products.Queries
{
    public class GetAllProductsAdminQuery : IRequest<PagedResult<ProductViewModel>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool? IsDeleted { get; set; } // null: all, true: deleted only, false: active only
        public int? CategoryId { get; set; }
        public string? Search { get; set; }
    }
}



