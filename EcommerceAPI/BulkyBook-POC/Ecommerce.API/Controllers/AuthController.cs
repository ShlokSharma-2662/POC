using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Commands;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Application.Features.Auth.Queries;
using Ecommerce.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IMessagePublisherService _messagePublisher;

        public AuthController(IMediator mediator, IMessagePublisherService messagePublisher)
        {
            _mediator = mediator;
            _messagePublisher = messagePublisher;
        }


        [HttpPost("register")]
        public async Task<ActionResult<AuthResult>> Register(RegisterUserCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                
                // Publish user registered event to RabbitMQ
                try
                {
                    await _messagePublisher.PublishUserRegisteredAsync(
                        result.UserId.ToString(),
                        command.Email,
                        command.FirstName,
                        command.LastName,
                        DateTime.UtcNow,
                        "web");
                    
                    Console.WriteLine($"Published UserRegistered event for {command.Email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to publish UserRegistered event: {ex.Message}");
                    // Don't fail the registration if message publishing fails
                }
                
                return SuccessResponse(result, "Success", "User registered successfully");
            }
            catch (Exception ex)
            {
                return HandleException<AuthResult>(ex, "Registration failed. Please check your information and try again. If the problem persists, contact support.");
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<AuthResult>> Login(LoginUserCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                return SuccessResponse(result, "Success", "User logged in successfully");
            }
            catch (Exception ex)
            {
                return HandleException<AuthResult>(ex, "Login failed. Please verify your email and password, then try again. If you've forgotten your password, please use the password reset feature.");
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                  ?? User.FindFirst("sub")?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<UserProfileDto>("Invalid user token");

                var query = new GetUserProfileQuery { UserId = userId };
                var profile = await _mediator.Send(query);
                return SuccessResponse(profile, "Success", "User profile retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<UserProfileDto>(ex, "Unable to retrieve your profile information. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<object>> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.OldPassword) || 
                    string.IsNullOrWhiteSpace(dto.NewPassword) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                    return ValidationErrorResponse("All password fields are required.");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                  ?? User.FindFirst("sub")?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
                    return UnauthorizedResponse<object>("Invalid user token");

                var command = new ChangePasswordCommand
                {
                    UserId = userId,
                    OldPassword = dto.OldPassword,
                    NewPassword = dto.NewPassword,
                    ConfirmPassword = dto.ConfirmPassword
                };

                var result = await _mediator.Send(command);
                if (!result) return SuccessResponse<object>("Failed to change password. Please try again.");
                
                return SuccessResponse(new { success = true }, "Success", "Password changed successfully");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "Password change failed. Please ensure your current password is correct and the new password meets the requirements, then try again.");
            }
        }
    }
}
