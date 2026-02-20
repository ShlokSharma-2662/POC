using Ecommerce.Application.Features.Metrics.Commands;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Features.Metrics.Handlers
{
    public class LogMetricHandler : IRequestHandler<LogMetricCommand>
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public LogMetricHandler(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(LogMetricCommand request, CancellationToken cancellationToken)
        {
            var isHighLatency = request.ResponseTimeMs > 1000; // Set your threshold

            var metric = new SystemMetric
            {
                Endpoint = request.Endpoint,
                ResponseTimeMs = (long)request.ResponseTimeMs,
                Timestamp = DateTime.Now,
                IsThresholdExceeded = isHighLatency
            };

            _context.SystemMetrics.Add(metric);
            await _context.SaveChangesAsync(cancellationToken);

            if (isHighLatency)
            {
                // Trigger email or notification
                await _emailService.NotifyAdminAsync($"High latency on {request.Endpoint}: {request.ResponseTimeMs} ms");
            }

            return Unit.Value;
        }
    }

}
