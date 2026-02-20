using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Features.Auth.Models
{
    public class AuthResult
    {
        public long UserId { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public string Role { get; }
        public string Token { get; }

        public AuthResult(long userId, string firstName, string lastName, string email,string role, string token)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Token = token;
            Role = role;
        }
    }
}
