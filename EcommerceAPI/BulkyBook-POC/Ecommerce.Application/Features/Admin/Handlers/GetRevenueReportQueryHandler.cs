using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, RevenueReportDto>
    {
        private readonly AppDbContext _context;

        public GetRevenueReportQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportDto> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
        {
            var (startDate, endDate) = CalculateDateRange(request);
            
            Console.WriteLine($"Revenue Report Query - ReportType: {request.ReportType}, StartDate: {startDate}, EndDate: {endDate}");
            
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            Console.WriteLine($"Found {orders.Count} orders in date range");

            var totalRevenue = orders.Sum(o => o.Items.Sum(i => i.Product.Price * i.Quantity));
            var totalOrders = orders.Count;

            var periods = GeneratePeriods(request.ReportType, startDate, endDate, orders);

            Console.WriteLine($"Generated {periods.Count} periods");

            return new RevenueReportDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                Periods = periods,
                ReportStartDate = startDate,
                ReportEndDate = endDate,
                ReportType = request.ReportType
            };
        }

        private (DateTime startDate, DateTime endDate) CalculateDateRange(GetRevenueReportQuery request)
        {
            var now = DateTime.UtcNow;
            
            return request.ReportType switch
            {
                "15day" => (now.AddDays(-15), now),
                "monthly" when request.Month.HasValue && request.Year.HasValue => 
                    (new DateTime(request.Year.Value, request.Month.Value, 1), 
                     new DateTime(request.Year.Value, request.Month.Value, 1).AddMonths(1).AddDays(-1)),
                "monthly" => (now.AddDays(-30), now),
                "6months" => (now.AddMonths(-6), now),
                "12months" => (now.AddMonths(-12), now),
                _ when request.StartDate.HasValue && request.EndDate.HasValue => 
                    (request.StartDate.Value, request.EndDate.Value),
                _ => (now.AddDays(-15), now)
            };
        }

        private List<RevenuePeriodDto> GeneratePeriods(string reportType, DateTime startDate, DateTime endDate, List<Domain.Entities.Order> orders)
        {
            var periods = new List<RevenuePeriodDto>();

            switch (reportType)
            {
                case "15day":
                    periods = Generate15DayPeriods(startDate, endDate, orders);
                    break;
                case "monthly":
                    periods = GenerateMonthlyPeriods(startDate, endDate, orders);
                    break;
                case "6months":
                    periods = GenerateMonthlyPeriods(startDate, endDate, orders);
                    break;
                case "12months":
                    periods = GenerateMonthlyPeriods(startDate, endDate, orders);
                    break;
                default:
                    periods = Generate15DayPeriods(startDate, endDate, orders);
                    break;
            }

            return periods;
        }

        private List<RevenuePeriodDto> Generate15DayPeriods(DateTime startDate, DateTime endDate, List<Domain.Entities.Order> orders)
        {
            var periods = new List<RevenuePeriodDto>();
            var current = startDate;

            Console.WriteLine($"Generating 15-day periods from {startDate} to {endDate}");

            while (current < endDate)
            {
                var periodEnd = current.AddDays(14);
                if (periodEnd > endDate) periodEnd = endDate;

                var periodOrders = orders.Where(o => o.CreatedAt >= current && o.CreatedAt <= periodEnd).ToList();
                var periodRevenue = periodOrders.Sum(o => o.Items.Sum(i => i.Product.Price * i.Quantity));
                var orderCount = periodOrders.Count;
                var avgOrderValue = orderCount > 0 ? periodRevenue / orderCount : 0;

                Console.WriteLine($"Period {current:MMM dd} - {periodEnd:MMM dd}: {orderCount} orders, ₹{periodRevenue:F2} revenue");

                periods.Add(new RevenuePeriodDto
                {
                    StartDate = current,
                    EndDate = periodEnd,
                    Revenue = periodRevenue,
                    OrderCount = orderCount,
                    AverageOrderValue = avgOrderValue,
                    PeriodLabel = $"{current:MMM dd} - {periodEnd:MMM dd, yyyy}"
                });

                current = periodEnd.AddDays(1);
            }

            return periods;
        }

        private List<RevenuePeriodDto> GenerateMonthlyPeriods(DateTime startDate, DateTime endDate, List<Domain.Entities.Order> orders)
        {
            var periods = new List<RevenuePeriodDto>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                var periodEnd = current.AddMonths(1).AddDays(-1);
                if (periodEnd > endDate) periodEnd = endDate;

                var periodOrders = orders.Where(o => o.CreatedAt >= current && o.CreatedAt <= periodEnd).ToList();
                var periodRevenue = periodOrders.Sum(o => o.Items.Sum(i => i.Product.Price * i.Quantity));
                var orderCount = periodOrders.Count;
                var avgOrderValue = orderCount > 0 ? periodRevenue / orderCount : 0;

                periods.Add(new RevenuePeriodDto
                {
                    StartDate = current,
                    EndDate = periodEnd,
                    Revenue = periodRevenue,
                    OrderCount = orderCount,
                    AverageOrderValue = avgOrderValue,
                    PeriodLabel = $"{current:MMMM yyyy}"
                });

                current = current.AddMonths(1);
            }

            return periods;
        }
    }
}
