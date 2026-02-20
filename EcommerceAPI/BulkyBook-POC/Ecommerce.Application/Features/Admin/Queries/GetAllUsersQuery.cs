using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Models;
using MediatR;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetAllUsersQuery : IRequest<PagedResult<AdminUserDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Role { get; set; }
    }
}


