using Ecommerce.Application.Features.Admin.Models;
using MediatR;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetRevenueReportQuery : IRequest<RevenueReportDto>
    {
        public string ReportType { get; set; } = "15day"; // "15day", "monthly", "6months", "12months"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Month { get; set; } // For monthly reports (1-12)
        public int? Year { get; set; } // For yearly reports
    }
}
