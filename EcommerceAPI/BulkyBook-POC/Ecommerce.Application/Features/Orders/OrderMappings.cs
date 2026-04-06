using System.Linq.Expressions;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Domain.Entities;
using OrderItemDto = Ecommerce.Application.Features.Orders.Models.OrderItemDto;

namespace Ecommerce.Application.Features.Orders
{
    public static class OrderMappings
    {
        public static readonly Expression<Func<Order, OrderDto>> ToOrderDtoExpression = order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            CustomerName = order.CustomerName,
            ShippingAddress = order.ShippingAddress,
            Phone = order.Phone,
            CreatedAt = order.CreatedAt,
            Status = string.IsNullOrEmpty(order.Status) ? OrderStatuses.Pending : order.Status,
            Items = order.Items.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product != null ? item.Product.Name : "Product not found",
                Price = item.Product != null ? item.Product.Price : 0,
                Quantity = item.Quantity,
                ImageUrl = item.Product != null ? item.Product.ImageUrl : string.Empty
            }).ToList()
        };
    }
}
