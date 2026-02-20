using Ecommerce.Application.Common.Models;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.API.Controllers
{
    [Route("api/google-oauth")]
    [ApiController]
    public class GoogleOAuthController : BaseController
    {
        private readonly IGoogleOAuthService _googleOAuthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleOAuthController> _logger;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly AppDbContext _context;

        public GoogleOAuthController(
            IGoogleOAuthService googleOAuthService,
            IConfiguration configuration,
            ILogger<GoogleOAuthController> logger,
            IJwtTokenGenerator jwtTokenGenerator,
            AppDbContext context)
        {
            _googleOAuthService = googleOAuthService;
            _configuration = configuration;
            _logger = logger;
            _jwtTokenGenerator = jwtTokenGenerator;
            _context = context;
        }

        [HttpGet("login")]
        public IActionResult Login([FromQuery] string? returnUrl = null)
        {
            try
            {
                var clientId = _configuration["GoogleOAuth:ClientId"];
                var scope = _configuration["GoogleOAuth:Scope"] ?? "openid profile email";
                var callbackPath = _configuration["GoogleOAuth:CallbackPath"] ?? "/api/google-oauth/callback";
                var responseType = _configuration["GoogleOAuth:ResponseType"] ?? "code";
                var baseUrl = _configuration["GoogleOAuth:BaseUrl"];

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    _logger.LogError("Google OAuth ClientId is not configured");
                    return BadRequest(new { error = "Google OAuth is not properly configured" });
                }

                // Use configured BaseUrl if available, otherwise construct from request
                // IMPORTANT: Google OAuth requires exact match - no query parameters in redirect URI
                string redirectUri;
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    redirectUri = $"{baseUrl.TrimEnd('/')}{callbackPath}";
                }
                else
                {
                    redirectUri = $"{Request.Scheme}://{Request.Host}{callbackPath}";
                }
                
                // Store returnUrl and redirectUri in session to ensure exact match during callback
                // Google OAuth doesn't allow query parameters in redirect URI
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    HttpContext.Session.SetString("google_oauth_return_url", returnUrl);
                }
                HttpContext.Session.SetString("google_oauth_redirect_uri", redirectUri);

                var state = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("google_oauth_state", state);
                
                _logger.LogInformation("Initiating Google OAuth with redirectUri: {RedirectUri}", redirectUri);

                // Google OAuth authorization URL
                var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={clientId}&" +
                    $"response_type={responseType}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"scope={Uri.EscapeDataString(scope)}&" +
                    $"state={state}&" +
                    $"access_type=offline&" +
                    $"prompt=consent";

                _logger.LogInformation("Redirecting to Google OAuth provider: {AuthUrl}", authUrl);

                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google OAuth login");
                return HandleException(ex, "Unable to initiate Google OAuth login. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery] string? returnUrl = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("Google OAuth callback error: {Error}", error);
                    return BadRequest(new { error = "Google OAuth authentication failed", details = error });
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("Google OAuth callback missing authorization code");
                    return BadRequest(new { error = "Authorization code not provided" });
                }

                var storedState = HttpContext.Session.GetString("google_oauth_state");
                if (string.IsNullOrEmpty(storedState) || storedState != state)
                {
                    _logger.LogWarning("Google OAuth state validation failed");
                    return BadRequest(new { error = "Invalid state parameter" });
                }

                // Get redirectUri from session to ensure exact match with what was sent to Google
                var redirectUri = HttpContext.Session.GetString("google_oauth_redirect_uri");
                
                if (string.IsNullOrEmpty(redirectUri))
                {
                    // Fallback to constructing it if not in session (shouldn't happen normally)
                    var callbackPath = _configuration["GoogleOAuth:CallbackPath"] ?? "/api/google-oauth/callback";
                    var baseUrl = _configuration["GoogleOAuth:BaseUrl"];
                    
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        redirectUri = $"{baseUrl.TrimEnd('/')}{callbackPath}";
                    }
                    else
                    {
                        redirectUri = $"{Request.Scheme}://{Request.Host}{callbackPath}";
                    }
                    _logger.LogWarning("RedirectUri not found in session, constructed: {RedirectUri}", redirectUri);
                }
                else
                {
                    _logger.LogInformation("Using redirectUri from session: {RedirectUri}", redirectUri);
                }
                
                // Get returnUrl from session if not in query string
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = HttpContext.Session.GetString("google_oauth_return_url");
                }

                _logger.LogInformation("Exchanging authorization code for token. RedirectUri: {RedirectUri}", redirectUri);
                var tokenResult = await _googleOAuthService.ExchangeCodeForTokenAsync(code, redirectUri);

                if (string.IsNullOrEmpty(tokenResult.AccessToken))
                {
                    _logger.LogError("Failed to exchange authorization code for tokens");
                    return BadRequest(new { error = "Failed to obtain access token" });
                }

                _logger.LogInformation("Successfully obtained access token. Retrieving user info...");
                var userInfo = await _googleOAuthService.GetUserInfoAsync(tokenResult.AccessToken);

                if (string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogError("Failed to retrieve user information. UserInfo: {UserInfo}", System.Text.Json.JsonSerializer.Serialize(userInfo));
                    return BadRequest(new { error = "Failed to retrieve user information" });
                }

                _logger.LogInformation("Successfully retrieved user info for email: {Email}", userInfo.Email);

                // Check if user exists in database, if not create them
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
                
                if (dbUser == null)
                {
                    // Create new user for OAuth login
                    _logger.LogInformation("Creating new user in database for OAuth email: {Email}", userInfo.Email);
                    dbUser = new ApplicationUser
                    {
                        Email = userInfo.Email,
                        FirstName = userInfo.GivenName ?? string.Empty,
                        LastName = userInfo.FamilyName ?? string.Empty,
                        Username = userInfo.Email, // Use email as username
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for OAuth users
                        Role = "User",
                        Status = "Active"
                    };

                    _context.Users.Add(dbUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User created successfully with ID: {UserId}", dbUser.Id);
                }
                else
                {
                    // Update user info if needed (in case name changed in Google)
                    if (dbUser.FirstName != userInfo.GivenName || dbUser.LastName != userInfo.FamilyName)
                    {
                        dbUser.FirstName = userInfo.GivenName ?? dbUser.FirstName;
                        dbUser.LastName = userInfo.FamilyName ?? dbUser.LastName;
                        dbUser.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("User info updated for ID: {UserId}", dbUser.Id);
                    }
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new(ClaimTypes.Email, userInfo.Email),
                    new(ClaimTypes.Name, userInfo.Name ?? string.Empty),
                    new(ClaimTypes.GivenName, userInfo.GivenName ?? string.Empty),
                    new(ClaimTypes.Surname, userInfo.FamilyName ?? string.Empty),
                    new("picture", userInfo.Picture ?? string.Empty),
                    new("access_token", tokenResult.AccessToken),
                    new("id_token", tokenResult.IdToken ?? string.Empty)
                };

                if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
                {
                    claims.Add(new Claim("refresh_token", tokenResult.RefreshToken));
                }

                var identity = new ClaimsIdentity(claims, "GoogleOAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookie", principal);

                // Generate JWT token using the actual database user
                var jwtToken = _jwtTokenGenerator.GenerateToken(dbUser);

                _logger.LogInformation("Successfully authenticated user via Google OAuth: {Email}", userInfo.Email);

                // Determine redirect URL - default to frontend OAuth callback
                string redirectUrl;
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    // If returnUrl is a relative path, assume it's a frontend route
                    if (!returnUrl.StartsWith("http://") && !returnUrl.StartsWith("https://"))
                    {
                        // Get frontend URL from configuration or default to localhost:4200
                        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://localhost:4200";
                        redirectUrl = $"{frontendUrl.TrimEnd('/')}{returnUrl}";
                        _logger.LogInformation("Converted relative returnUrl to full URL: {RedirectUrl}", redirectUrl);
                    }
                    else
                    {
                        redirectUrl = returnUrl;
                    }
                }
                else
                {
                    // Default to frontend OAuth callback
                    var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "https://localhost:4200";
                    redirectUrl = $"{frontendUrl}/oauth/callback";
                }
                
                // Add JWT token as query parameter for frontend to extract
                var separator = redirectUrl.Contains("?") ? "&" : "?";
                redirectUrl += $"{separator}jwt_token={Uri.EscapeDataString(jwtToken)}";
                
                _logger.LogInformation("Redirecting to: {RedirectUrl}", redirectUrl);
                return Redirect(redirectUrl);
            }
            catch (OAuthException oauthEx)
            {
                _logger.LogError(oauthEx, "OAuth error in Google OAuth callback. Type: {ExceptionType}, Message: {Message}", 
                    oauthEx.GetType().Name, oauthEx.Message);
                
                // Return OAuth-specific error with actual message
                return BadRequest(new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = oauthEx is InvalidGrantException ? "InvalidGrant" : 
                             oauthEx is TokenExchangeException ? "TokenExchangeFailed" :
                             oauthEx is UserInfoException ? "UserInfoFailed" : "OAuthError",
                    StatusReason = oauthEx.Message ?? "Google OAuth authentication failed",
                    Data = new { errorType = oauthEx.GetType().Name }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Google OAuth callback. Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                
                // Return detailed error for debugging
                var errorMessage = ex is InvalidOperationException || ex is ArgumentException 
                    ? ex.Message 
                    : $"Google OAuth authentication failed: {ex.Message}";
                
                return BadRequest(new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = "Exception",
                    StatusReason = errorMessage,
                    Data = new 
                    { 
                        errorType = ex.GetType().Name,
                        innerException = ex.InnerException?.Message
                    }
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync("Cookie");

                var logoutUrl = "https://accounts.google.com/logout";

                _logger.LogInformation("User logged out successfully from Google OAuth");

                return Ok(new { logoutUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth logout");
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
                _logger.LogError(ex, "Error getting Google OAuth user info");
                return HandleException(ex, "Unable to retrieve user information. Please try again or contact support if the issue persists.");
            }
        }
    }
}

