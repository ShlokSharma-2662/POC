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
    [Route("api/oauth")]
    [ApiController]
    public class OAuthController : BaseController
    {
        private readonly IOAuthService _oauthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthController> _logger;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IExternalOAuthUserService _externalUserService;
        private readonly IOAuthAuthorizationCodeStore _authorizationCodeStore;

        public OAuthController(
            IOAuthService oauthService,
            IConfiguration configuration,
            ILogger<OAuthController> logger,
            IJwtTokenGenerator jwtTokenGenerator,
            IExternalOAuthUserService externalUserService,
            IOAuthAuthorizationCodeStore authorizationCodeStore)
        {
            _oauthService = oauthService;
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
                var authority = _configuration["OAuth:Authority"];
                var clientId = _configuration["OAuth:ClientId"];
                var scope = _configuration["OAuth:Scope"] ?? "openid profile email";
                var responseType = _configuration["OAuth:ResponseType"] ?? "code";

                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
                        _configuration,
                        returnUrl,
                        out var safeReturnUrl))
                {
                    _logger.LogWarning("Rejected untrusted OAuth return URL");
                    return ValidationErrorResponse("The return URL is not allowed.");
                }

                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnPath(
                        returnTo,
                        out var safeReturnTo))
                {
                    _logger.LogWarning("Rejected untrusted OAuth return destination");
                    return ValidationErrorResponse("The return destination is not allowed.");
                }

                var redirectUri = OAuthRedirectUrlHelper.BuildCallbackUri(
                    Request,
                    _configuration,
                    "OAuth",
                    "/api/oauth/callback");

                var state = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("oauth_state", state);
                HttpContext.Session.SetString("oauth_return_url", safeReturnUrl);
                HttpContext.Session.SetString("oauth_redirect_uri", redirectUri);
                if (safeReturnTo is not null)
                {
                    HttpContext.Session.SetString("oauth_return_to", safeReturnTo);
                }

                var authorizeEndpoint = MicrosoftOAuthEndpointBuilder.BuildV2Endpoint(authority, "authorize");
                var authUrl = $"{authorizeEndpoint}?" +
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

                HttpContext.Session.Remove("oauth_state");

                var redirectUri = HttpContext.Session.GetString("oauth_redirect_uri") ??
                    OAuthRedirectUrlHelper.BuildCallbackUri(
                        Request,
                        _configuration,
                        "OAuth",
                        "/api/oauth/callback");
                HttpContext.Session.Remove("oauth_redirect_uri");

                var storedReturnUrl = HttpContext.Session.GetString("oauth_return_url");
                HttpContext.Session.Remove("oauth_return_url");
                var safeReturnTo = HttpContext.Session.GetString("oauth_return_to");
                HttpContext.Session.Remove("oauth_return_to");
                var requestedReturnUrl = storedReturnUrl ?? returnUrl;
                if (!OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
                        _configuration,
                        requestedReturnUrl,
                        out var safeReturnUrl))
                {
                    _logger.LogWarning("Rejected untrusted OAuth callback return URL");
                    return ValidationErrorResponse("The return URL is not allowed.");
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

                var dbUser = await _externalUserService.UpsertAsync(
                    "microsoft",
                    userInfo,
                    HttpContext.RequestAborted);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new(ClaimTypes.Email, dbUser.Email),
                    new(ClaimTypes.Name, userInfo.Name ?? string.Empty),
                    new(ClaimTypes.GivenName, dbUser.FirstName),
                    new(ClaimTypes.Surname, dbUser.LastName),
                    new("external_subject", userInfo.Subject),
                    new("picture", userInfo.Picture ?? string.Empty),
                    new("access_token", tokenResult.AccessToken),
                    new("id_token", tokenResult.IdToken ?? string.Empty)
                };

                if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
                {
                    claims.Add(new Claim("refresh_token", tokenResult.RefreshToken));
                }

                var identity = new ClaimsIdentity(claims, "OAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookie", principal);

                var jwtToken = _jwtTokenGenerator.GenerateToken(dbUser);

                _logger.LogInformation("Successfully authenticated user: {Email}", userInfo.Email);

                var authorizationCode = await _authorizationCodeStore.IssueAsync(
                    "microsoft",
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
                    "microsoft");
                Response.Headers.CacheControl = "no-store";
                Response.Headers["Referrer-Policy"] = "no-referrer";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuth callback");
                return HandleException(ex, "OAuth authentication failed. Please try logging in again or contact support if the issue persists.");
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
                "microsoft",
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
                
                var authority = _configuration["OAuth:Authority"];
                var clientId = _configuration["OAuth:ClientId"];
                OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
                    _configuration,
                    "/",
                    out var postLogoutRedirectUri);
                
                var logoutEndpoint = MicrosoftOAuthEndpointBuilder.BuildV2Endpoint(authority, "logout");
                var logoutUrl = $"{logoutEndpoint}?" +
                    $"client_id={clientId}&" +
                    $"post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

                _logger.LogInformation("User logged out successfully");
                
                return Ok(ApiResponse<OAuthLogoutResponse>.Success(
                    new OAuthLogoutResponse(logoutUrl),
                    "Success",
                    "Logged out successfully"));
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
