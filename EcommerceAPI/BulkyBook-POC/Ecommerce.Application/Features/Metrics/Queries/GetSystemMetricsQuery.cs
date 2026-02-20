using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using MediatR;

namespace Ecommerce.Application.Features.Metrics.Queries
{
    public class GetSystemMetricsQuery : IRequest<PagedResult<SystemMetric>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? EndpointFilter { get; set; }
        public int? StatusCodeFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
