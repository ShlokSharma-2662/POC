using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;

namespace Ecommerce.Application.Features.Auth.Handlers
{
    public class LoginUserHandler : IRequestHandler<LoginUserCommand, AuthResult>
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _jwt;

        public LoginUserHandler(AppDbContext context, IJwtTokenGenerator jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<AuthResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync(cancellationToken);
                
                // Check if any users exist with similar emails
                var emailPrefix = request.Email.Split('@')[0];
                var similarUsers = await _context.Users
                    .Where(u => u.Email.Contains(emailPrefix))
                    .Select(u => new { u.Email, u.Id })
                    .ToListAsync(cancellationToken);
                foreach (var u in similarUsers)
                {
                    Console.WriteLine($"  - {u.Email} (ID: {u.Id})");
                }

                // Try to find the user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

                if (user == null)
                {
                    var allUsers = await _context.Users.Select(u => u.Email).ToListAsync(cancellationToken);
                    foreach (var email in allUsers)
                    {
                        Console.WriteLine($"  - {email}");
                    }
                    throw new Exception("Invalid credentials.");
                }
                // Test password verification
                bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!passwordValid)
                {
                    Console.WriteLine("❌ Password verification failed");
                    throw new Exception("Invalid credentials.");
                }

                if (user.Status == "Deactivated")
                {
                    Console.WriteLine("❌ Account is deactivated");
                    throw new Exception("Account is deactivated. Please contact support.");
                }
                var token = _jwt.GenerateToken(user);
                return new AuthResult(user.Id, user.FirstName, user.LastName, user.Email, user.Role, token);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
