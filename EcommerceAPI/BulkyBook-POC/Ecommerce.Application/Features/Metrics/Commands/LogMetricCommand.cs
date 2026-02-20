using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Features.Metrics.Commands
{
    public class LogMetricCommand : IRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public double ResponseTimeMs { get; set; }
    }
}
