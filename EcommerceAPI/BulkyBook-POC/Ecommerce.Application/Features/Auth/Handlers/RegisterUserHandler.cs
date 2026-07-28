using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Ecommerce.Application.Features.Auth.Handlers
{
    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, AuthResult>
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _jwt;
        private readonly IEmailService _emailService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<RegisterUserHandler> _logger;

        public RegisterUserHandler(AppDbContext context, IJwtTokenGenerator jwt, IEmailService emailService, ICacheInvalidationService cacheInvalidationService, ILogger<RegisterUserHandler> logger)
        {
            _context = context;
            _jwt = jwt;
            _emailService = emailService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        public async Task<AuthResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                throw new InvalidOperationException("User already exists.");

            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                // Public registration must never grant privileged roles. Admin
                // accounts are provisioned through authenticated admin tooling.
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var token = _jwt.GenerateToken(user);

            try
            {
                await _emailService.SendRegistrationConfirmationEmailAsync(user.Email, user.FirstName, user.LastName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration confirmation email to {Email}. User registration will continue.", user.Email);
            }
            await _cacheInvalidationService.InvalidateUserCacheAsync();

            return new AuthResult(user.Id, user.FirstName, user.LastName, user.Email, user.Role, token);
        }
    }
}
