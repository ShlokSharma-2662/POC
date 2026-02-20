using MediatR;
using System;

namespace Ecommerce.Application.Features.Admin.Commands
{
    public class UpdateOrderStatusCommand : IRequest<bool>
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = "Pending";
    }
}