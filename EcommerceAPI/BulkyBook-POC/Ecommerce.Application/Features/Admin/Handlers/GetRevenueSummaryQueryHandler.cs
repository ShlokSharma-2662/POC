using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class GetRevenueSummaryQueryHandler : IRequestHandler<GetRevenueSummaryQuery, RevenueSummaryDto>
    {
        private readonly AppDbContext _context;

        public GetRevenueSummaryQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueSummaryDto> Handle(GetRevenueSummaryQuery request, CancellationToken cancellationToken)
        {
            var (currentStart, currentEnd, previousStart, previousEnd) = CalculatePeriods(request.Period);
            
            // Get current period data
            var currentOrders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.CreatedAt >= currentStart && o.CreatedAt <= currentEnd)
                .ToListAsync(cancellationToken);

            // Get previous period data for comparison
            var previousOrders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.CreatedAt >= previousStart && o.CreatedAt <= previousEnd)
                .ToListAsync(cancellationToken);

            var currentRevenue = currentOrders.Sum(o => o.Items.Sum(i => i.Product.Price * i.Quantity));
            var currentOrderCount = currentOrders.Count;
            var currentAvgOrderValue = currentOrderCount > 0 ? currentRevenue / currentOrderCount : 0;

            var previousRevenue = previousOrders.Sum(o => o.Items.Sum(i => i.Product.Price * i.Quantity));
            var previousOrderCount = previousOrders.Count;

            var revenueGrowth = previousRevenue > 0 ? ((currentRevenue - previousRevenue) / previousRevenue) * 100 : 0;
            var orderGrowth = previousOrderCount > 0 ? ((currentOrderCount - previousOrderCount) / (double)previousOrderCount) * 100 : 0;

            return new RevenueSummaryDto
            {
                TotalRevenue = currentRevenue,
                TotalOrders = currentOrderCount,
                AverageOrderValue = currentAvgOrderValue,
                RevenueGrowth = Math.Round(revenueGrowth, 2),
                OrderGrowth = (int)Math.Round(orderGrowth, 0),
                LastUpdated = DateTime.UtcNow
            };
        }

        private (DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd) CalculatePeriods(string period)
        {
            var now = DateTime.UtcNow;
            
            return period switch
            {
                "7days" => (
                    now.AddDays(-7),
                    now,
                    now.AddDays(-14),
                    now.AddDays(-7)
                ),
                "30days" => (
                    now.AddDays(-30),
                    now,
                    now.AddDays(-60),
                    now.AddDays(-30)
                ),
                "90days" => (
                    now.AddDays(-90),
                    now,
                    now.AddDays(-180),
                    now.AddDays(-90)
                ),
                _ => (
                    now.AddDays(-30),
                    now,
                    now.AddDays(-60),
                    now.AddDays(-30)
                )
            };
        }
    }
}
