using Ecommerce.Application.Common.Models;
using Ecommerce.API.Security;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IExternalOAuthUserService _externalUserService;
        private readonly IOAuthAuthorizationCodeStore _authorizationCodeStore;

        public GoogleOAuthController(
            IGoogleOAuthService googleOAuthService,
            IConfiguration configuration,
            ILogger<GoogleOAuthController> logger,
            IJwtTokenGenerator jwtTokenGenerator,
            IExternalOAuthUserService externalUserService,
            IOAuthAuthorizationCodeStore authorizationCodeStore)
        {
            _googleOAuthService = googleOAuthService;
            _configuration = configuration;
            _logger = logger;
            _jwtTokenGenerator = jwtTokenGenerator;
            _externalUserService = externalUserService;
            _authorizationCodeStore = authorizationCodeStore;
        }

        [HttpGet("login")]
        public IActionResult Login(
            [FromQuery] string? returnUrl = null,
            [FromQuery] string? returnTo = null)
        {
            try
            {
                var clientId = _configuration["GoogleOAuth:ClientId"];
                var scope = _configuration["GoogleOAuth:Scope"] ?? "openid profile email";
                var responseType = _configuration["GoogleOAuth:ResponseType"] ?? "code";

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    _logger.LogError("Google OAuth ClientId is not configured");
                    return BadRequest(new { error = "Google OAuth is not properly configured" });
                }

                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
                        _configuration,
                        returnUrl,
                        out var safeReturnUrl))
                {
                    _logger.LogWarning("Rejected untrusted Google OAuth return URL");
                    return ValidationErrorResponse("The return URL is not allowed.");
                }

                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnPath(
                        returnTo,
                        out var safeReturnTo))
                {
                    _logger.LogWarning("Rejected untrusted Google OAuth return destination");
                    return ValidationErrorResponse("The return destination is not allowed.");
                }

                // Google requires an exact callback URI with no per-request query parameters.
                var redirectUri = OAuthRedirectUrlHelper.BuildCallbackUri(
                    Request,
                    _configuration,
                    "GoogleOAuth",
                    "/api/google-oauth/callback");
                
                // Store returnUrl and redirectUri in session to ensure exact match during callback
                // Google OAuth doesn't allow query parameters in redirect URI
                HttpContext.Session.SetString("google_oauth_return_url", safeReturnUrl);
                HttpContext.Session.SetString("google_oauth_redirect_uri", redirectUri);
                if (safeReturnTo is not null)
                {
                    HttpContext.Session.SetString("google_oauth_return_to", safeReturnTo);
                }

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

                HttpContext.Session.Remove("google_oauth_state");

                // Get redirectUri from session to ensure exact match with what was sent to Google
                var redirectUri = HttpContext.Session.GetString("google_oauth_redirect_uri");
                HttpContext.Session.Remove("google_oauth_redirect_uri");
                
                if (string.IsNullOrEmpty(redirectUri))
                {
                    // Fallback to constructing it if not in session (shouldn't happen normally)
                    redirectUri = OAuthRedirectUrlHelper.BuildCallbackUri(
                        Request,
                        _configuration,
                        "GoogleOAuth",
                        "/api/google-oauth/callback");
                    _logger.LogWarning("RedirectUri not found in session, constructed: {RedirectUri}", redirectUri);
                }
                else
                {
                    _logger.LogInformation("Using redirectUri from session: {RedirectUri}", redirectUri);
                }
                
                // Prefer the session value established during login. A callback query value is
                // accepted only for compatibility and is still validated below.
                var storedReturnUrl = HttpContext.Session.GetString("google_oauth_return_url");
                HttpContext.Session.Remove("google_oauth_return_url");
                var safeReturnTo = HttpContext.Session.GetString("google_oauth_return_to");
                HttpContext.Session.Remove("google_oauth_return_to");
                var requestedReturnUrl = storedReturnUrl ?? returnUrl;
                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
                        _configuration,
                        requestedReturnUrl,
                        out var safeReturnUrl))
                {
                    _logger.LogWarning("Rejected untrusted Google OAuth callback return URL");
                    return ValidationErrorResponse("The return URL is not allowed.");
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

                var dbUser = await _externalUserService.UpsertAsync(
                    "google",
                    userInfo,
                    HttpContext.RequestAborted);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new(ClaimTypes.Email, userInfo.Email),
                    new(ClaimTypes.Name, userInfo.Name ?? string.Empty),
                    new(ClaimTypes.GivenName, userInfo.GivenName ?? string.Empty),
                    new(ClaimTypes.Surname, userInfo.FamilyName ?? string.Empty),
                    new("external_subject", userInfo.Subject),
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

                var authorizationCode = await _authorizationCodeStore.IssueAsync(
                    "google",
                    new OAuthAuthorizationGrant(
                        jwtToken,
                        dbUser.Id,
                        dbUser.FirstName,
                        dbUser.LastName,
                        dbUser.Email,
                        dbUser.Role,
                        userInfo.Picture,
                        safeReturnTo,
                        default),
                    HttpContext.RequestAborted);
                var redirectUrl = OAuthRedirectUrlHelper.AppendAuthorizationCode(
                    safeReturnUrl,
                    authorizationCode,
                    "google");
                Response.Headers.CacheControl = "no-store";
                Response.Headers["Referrer-Policy"] = "no-referrer";
                
                _logger.LogInformation("Redirecting to the configured frontend OAuth callback");
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

        [HttpPost("exchange")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<OAuthCodeExchangeResponse>> Exchange(
            [FromBody] OAuthCodeExchangeRequest? request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
            {
                return ValidationErrorResponse<OAuthCodeExchangeResponse>(
                    "The OAuth authorization code is required.");
            }

            var grant = await _authorizationCodeStore.ConsumeAsync(
                "google",
                request.Code,
                cancellationToken);
            if (grant is null)
            {
                return ValidationErrorResponse<OAuthCodeExchangeResponse>(
                    "The OAuth authorization code is invalid, expired, or has already been used.");
            }

            Response.Headers.CacheControl = "no-store";
            return Ok(ApiResponse<OAuthCodeExchangeResponse>.Success(
                ToExchangeResponse(grant),
                "Success",
                "OAuth authentication completed successfully"));
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

                return Ok(ApiResponse<OAuthLogoutResponse>.Success(
                    new OAuthLogoutResponse(logoutUrl),
                    "Success",
                    "Logged out successfully"));
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

        private static OAuthCodeExchangeResponse ToExchangeResponse(
            OAuthAuthorizationGrant grant) =>
            new(
                grant.Token,
                grant.UserId,
                grant.FirstName,
                grant.LastName,
                grant.Email,
                grant.Role,
                $"{grant.FirstName} {grant.LastName}".Trim(),
                grant.Picture,
                grant.ReturnTo);
    }
}

