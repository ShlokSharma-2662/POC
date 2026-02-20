using Ecommerce.Application.Common.Models;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.API.Controllers
{
    [Route("api/oauth")]
    [ApiController]
    public class OAuthController : BaseController
    {
        private readonly IOAuthService _oauthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthController> _logger;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public OAuthController(IOAuthService oauthService, IConfiguration configuration, ILogger<OAuthController> logger, IJwtTokenGenerator jwtTokenGenerator)
        {
            _oauthService = oauthService;
            _configuration = configuration;
            _logger = logger;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        [HttpGet("login")]
        public IActionResult Login([FromQuery] string? returnUrl = null)
        {
            try
            {
                var authority = _configuration["OAuth:Authority"];
                var clientId = _configuration["OAuth:ClientId"];
                var scope = _configuration["OAuth:Scope"] ?? "openid profile email";
                var callbackPath = _configuration["OAuth:CallbackPath"] ?? "/signin-oidc";
                var responseType = _configuration["OAuth:ResponseType"] ?? "code";

                var redirectUri = $"{Request.Scheme}://{Request.Host}{callbackPath}";
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    redirectUri += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
                }

                var state = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("oauth_state", state);

                var authUrl = $"{authority}/oauth2/v2.0/authorize?" +
                    $"client_id={clientId}&" +
                    $"response_type={responseType}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"scope={Uri.EscapeDataString(scope)}&" +
                    $"state={state}&" +
                    $"response_mode=query";

                _logger.LogInformation("Redirecting to OAuth provider: {AuthUrl}", authUrl);
                
                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating OAuth login");
                return HandleException(ex, "Unable to initiate OAuth login. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? returnUrl = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("OAuth callback error: {Error}", error);
                    return BadRequest(new { error = "OAuth authentication failed", details = error });
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("OAuth callback missing authorization code");
                    return BadRequest(new { error = "Authorization code not provided" });
                }

                var storedState = HttpContext.Session.GetString("oauth_state");
                if (string.IsNullOrEmpty(storedState) || storedState != state)
                {
                    _logger.LogWarning("OAuth state validation failed");
                    return BadRequest(new { error = "Invalid state parameter" });
                }

                var callbackPath = _configuration["OAuth:CallbackPath"] ?? "/signin-oidc";
                var redirectUri = $"{Request.Scheme}://{Request.Host}{callbackPath}";
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    redirectUri += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
                }

                var tokenResult = await _oauthService.ExchangeCodeForTokenAsync(code, redirectUri);
                
                if (string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _logger.LogError("Failed to exchange authorization code for tokens");
                    return BadRequest(new { error = "Failed to obtain access token" });
                }

                var userInfo = await _oauthService.GetUserInfoAsync(tokenResult.AccessToken);
                
                if (string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogError("Failed to retrieve user information");
                    return BadRequest(new { error = "Failed to retrieve user information" });
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userInfo.Subject),
                    new(ClaimTypes.Email, userInfo.Email),
                    new(ClaimTypes.Name, userInfo.Name),
                    new(ClaimTypes.GivenName, userInfo.GivenName),
                    new(ClaimTypes.Surname, userInfo.FamilyName),
                    new("picture", userInfo.Picture),
                    new("access_token", tokenResult.AccessToken),
                    new("id_token", tokenResult.IdToken)
                };

                if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
                {
                    claims.Add(new Claim("refresh_token", tokenResult.RefreshToken));
                }

                var identity = new ClaimsIdentity(claims, "OAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookie", principal);

                // Generate JWT token for API access
                var jwtClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userInfo.Subject),
                    new Claim(ClaimTypes.Email, userInfo.Email),
                    new Claim(ClaimTypes.Name, userInfo.Name),
                    new Claim(ClaimTypes.GivenName, userInfo.GivenName),
                    new Claim(ClaimTypes.Surname, userInfo.FamilyName),
                    new Claim(ClaimTypes.Role, "User") // Default role for OAuth users
                };

                var jwtIdentity = new ClaimsIdentity(jwtClaims, "JWT");
                var jwtPrincipal = new ClaimsPrincipal(jwtIdentity);
                // Generate a temporary ID for OAuth users since Subject might not be a valid long
                var tempId = Math.Abs(userInfo.Subject.GetHashCode());
                var jwtToken = _jwtTokenGenerator.GenerateToken(new Ecommerce.Domain.Entities.ApplicationUser
                {
                    Id = tempId,
                    Email = userInfo.Email,
                    FirstName = userInfo.GivenName,
                    LastName = userInfo.FamilyName,
                    Role = "User"
                });

                _logger.LogInformation("Successfully authenticated user: {Email}", userInfo.Email);

                var redirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";
                // Add JWT token as query parameter for frontend to extract
                var separator = redirectUrl.Contains("?") ? "&" : "?";
                redirectUrl += $"{separator}jwt_token={Uri.EscapeDataString(jwtToken)}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuth callback");
                return HandleException(ex, "OAuth authentication failed. Please try logging in again or contact support if the issue persists.");
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("Cookie");
                
                var authority = _configuration["OAuth:Authority"];
                var clientId = _configuration["OAuth:ClientId"];
                var postLogoutRedirectUri = $"{Request.Scheme}://{Request.Host}/";
                
                var logoutUrl = $"{authority}/oauth2/v2.0/logout?" +
                    $"client_id={clientId}&" +
                    $"post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

                _logger.LogInformation("User logged out successfully");
                
                return Ok(new { logoutUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return HandleException(ex, "Logout failed. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("userinfo")]
        [Authorize]
        public ActionResult<object> GetUserInfo()
        {
            try
            {
                var user = HttpContext.User;
                var userInfo = new
                {
                    id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    email = user.FindFirst(ClaimTypes.Email)?.Value,
                    name = user.FindFirst(ClaimTypes.Name)?.Value,
                    givenName = user.FindFirst(ClaimTypes.GivenName)?.Value,
                    familyName = user.FindFirst(ClaimTypes.Surname)?.Value,
                    picture = user.FindFirst("picture")?.Value,
                    isAuthenticated = user.Identity?.IsAuthenticated ?? false
                };

                return SuccessResponse(userInfo, "Success", "User information retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return HandleException(ex, "Unable to retrieve user information. Please try again or contact support if the issue persists.");
            }
        }
    }
}
