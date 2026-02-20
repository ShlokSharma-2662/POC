using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Domain.Entities
{
    public class SystemMetric
    {
        public long Id { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
        public long StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsThresholdExceeded { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
    }
}
