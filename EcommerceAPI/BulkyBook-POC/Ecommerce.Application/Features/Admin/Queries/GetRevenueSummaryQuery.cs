using Ecommerce.Application.Features.Admin.Models;
using MediatR;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetRevenueSummaryQuery : IRequest<RevenueSummaryDto>
    {
        public string Period { get; set; } = "30days"; // "7days", "30days", "90days"
    }
}
