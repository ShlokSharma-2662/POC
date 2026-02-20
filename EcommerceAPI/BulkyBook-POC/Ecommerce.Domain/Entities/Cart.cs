using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class Cart
    {
        public int CartId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<CartItem> Items { get; set; } = new();
    }
}

