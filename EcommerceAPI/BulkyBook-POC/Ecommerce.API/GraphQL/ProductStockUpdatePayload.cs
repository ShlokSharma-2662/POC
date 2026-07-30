using System;

namespace Ecommerce.API.GraphQL
{
    public class ProductStockUpdatePayload
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
