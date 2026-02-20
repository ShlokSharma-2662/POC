using Ecommerce.Application.Features.Categories.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Features.Categories.Queries
{
    public record GetAllCategoriesQuery : IRequest<List<Category>>;
}
