using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using MediatR;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetAllErrorLogsQuery : IRequest<PagedResult<ErrorLog>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SeverityFilter { get; set; }
        public string? SearchTerm { get; set; }
    }
}
