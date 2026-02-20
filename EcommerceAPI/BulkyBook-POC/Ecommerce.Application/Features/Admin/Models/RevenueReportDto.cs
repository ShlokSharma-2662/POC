namespace Ecommerce.Application.Features.Admin.Models
{
    public class RevenueReportDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public List<RevenuePeriodDto> Periods { get; set; } = new();
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        public string ReportType { get; set; } = string.Empty; // "15day", "monthly", "6months", "12months"
    }

    public class RevenuePeriodDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string PeriodLabel { get; set; } = string.Empty; // e.g., "Jan 1-15, 2024"
    }

    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RevenueGrowth { get; set; } // Percentage change from previous period
        public int OrderGrowth { get; set; } // Percentage change from previous period
        public DateTime LastUpdated { get; set; }
    }
}
