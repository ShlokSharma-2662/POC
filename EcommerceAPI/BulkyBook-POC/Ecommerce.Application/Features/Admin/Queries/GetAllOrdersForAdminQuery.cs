using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Models;
using MediatR;
using System.Collections.Generic;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetAllOrdersForAdminQuery : IRequest<PagedResult<AdminOrderDto>>
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}