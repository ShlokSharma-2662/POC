using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Domain.Entities
{
    public class ApiAlert
    {
        public long Id { get; set; }
        public string Path { get; set; } = null!;
        public long ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
