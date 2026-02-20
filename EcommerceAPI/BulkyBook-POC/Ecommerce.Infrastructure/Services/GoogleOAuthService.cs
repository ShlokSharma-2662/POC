using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ecommerce.Infrastructure.Services
{
    public interface IGoogleOAuthService
    {
        Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri);
        Task<OAuthUserInfo> GetUserInfoAsync(string accessToken);
    }

    public class GoogleOAuthService : IGoogleOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleOAuthService> _logger;
        private readonly IResiliencePolicyService _resiliencePolicyService;

        public GoogleOAuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GoogleOAuthService> logger,
            IResiliencePolicyService resiliencePolicyService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _resiliencePolicyService = resiliencePolicyService;

            // Set timeout for external calls
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            // Input validation and sanitization
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Authorization code cannot be null or empty.", nameof(code));

            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new ArgumentException("Redirect URI cannot be null or empty.", nameof(redirectUri));

            // Sanitize inputs
            code = SanitizeInput(code);
            redirectUri = SanitizeInput(redirectUri);

            // Validate redirect URI format
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriParsed))
                throw new ArgumentException("Invalid redirect URI format.", nameof(redirectUri));

            try
            {
                var clientId = _configuration["GoogleOAuth:ClientId"];
                var clientSecret = _configuration["GoogleOAuth:ClientSecret"]
                    ?? throw new InvalidOperationException(
                        "Google OAuth ClientSecret is not configured. Please set it in User Secrets (development) or Azure Key Vault (production).");

                if (string.IsNullOrWhiteSpace(clientId))
                    throw new InvalidOperationException("Google OAuth configuration is incomplete.");

                // Google OAuth token endpoint
                var tokenEndpoint = "https://oauth2.googleapis.com/token";

                var parameters = new Dictionary<string, string>
                {
                    {"client_id", clientId},
                    {"client_secret", clientSecret},
                    {"code", code},
                    {"redirect_uri", redirectUri},
                    {"grant_type", "authorization_code"}
                };

                var content = new FormUrlEncodedContent(parameters);

                // Apply retry and circuit breaker policies
                var retryPolicy = _resiliencePolicyService.GetHttpRetryPolicy();
                var circuitBreakerPolicy = _resiliencePolicyService.GetHttpCircuitBreakerPolicy();
                var policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

                var response = await policy.ExecuteAsync(async () =>
                {
                    return await _httpClient.PostAsync(tokenEndpoint, content);
                });

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResult = JsonSerializer.Deserialize<OAuthTokenResult>(json);

                    if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
                    {
                        _logger.LogWarning("Token exchange succeeded but returned empty token result");
                        throw new TokenExchangeException("Token exchange returned invalid response.");
                    }

                    _logger.LogInformation("Successfully exchanged code for Google OAuth token");
                    return tokenResult;
                }

                // Handle specific error responses
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to exchange code for Google OAuth token. Status: {Status}, Response: {Response}, RedirectUri: {RedirectUri}, ClientId: {ClientId}",
                    response.StatusCode, errorContent, redirectUri, clientId);

                // Try to parse error details from response
                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorJson.TryGetProperty("error", out var errorProp))
                    {
                        var errorDescription = errorJson.TryGetProperty("error_description", out var descProp) 
                            ? descProp.GetString() 
                            : errorProp.GetString();
                        _logger.LogError("Google OAuth error details: {ErrorDescription}", errorDescription);
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidGrantException(
                        $"Google OAuth token exchange failed: {response.StatusCode}. Response: {errorContent}");
                }

                throw new TokenExchangeException(
                    $"Google OAuth token exchange failed with status {response.StatusCode}. Response: {errorContent}");
            }
            catch (OAuthException)
            {
                // Re-throw OAuth-specific exceptions
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open - Google OAuth service is unavailable");
                throw new OAuthException(
                    "Google OAuth service is temporarily unavailable. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Google OAuth token exchange timed out");
                throw new TokenExchangeException("Google OAuth token exchange timed out. Please try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during Google OAuth token exchange");
                throw new TokenExchangeException(
                    "Network error occurred during Google OAuth token exchange. Please check your connection and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google OAuth token exchange");
                throw new TokenExchangeException(
                    "An unexpected error occurred during Google OAuth token exchange.", ex);
            }
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remove any potentially dangerous characters but preserve valid OAuth code/URI characters
            return Regex.Replace(input, @"[^\w\-._~:/?#\[\]@!$&'()*+,;=]", "");
        }

        public async Task<OAuthUserInfo> GetUserInfoAsync(string accessToken)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            // Don't sanitize access token - it's already validated and contains valid JWT characters
            // Sanitizing might remove valid characters from the token

            try
            {
                // Google OAuth user info endpoint
                var userInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

                // Clear previous authorization header and set new one
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Apply retry and circuit breaker policies
                var retryPolicy = _resiliencePolicyService.GetHttpRetryPolicy();
                var circuitBreakerPolicy = _resiliencePolicyService.GetHttpCircuitBreakerPolicy();
                var policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

                var response = await policy.ExecuteAsync(async () =>
                {
                    return await _httpClient.GetAsync(userInfoEndpoint);
                });

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Google user info response: {Json}", json);
                    
                    var userInfo = JsonSerializer.Deserialize<OAuthUserInfo>(json);

                    if (userInfo == null)
                    {
                        _logger.LogError("Failed to deserialize user info. JSON: {Json}", json);
                        throw new UserInfoException($"Google user info endpoint returned invalid response. JSON: {json}");
                    }

                    // Google's userinfo endpoint may return "id" instead of "sub" - check both
                    if (string.IsNullOrEmpty(userInfo.Subject))
                    {
                        // Try to parse "id" field if "sub" is missing (Google sometimes uses "id")
                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(json);
                            if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
                            {
                                userInfo.Subject = idElement.GetString() ?? string.Empty;
                                _logger.LogInformation("Using 'id' field as Subject: {Subject}", userInfo.Subject);
                            }
                            // Also check if email can be used as fallback identifier
                            else if (!string.IsNullOrEmpty(userInfo.Email))
                            {
                                _logger.LogWarning("No 'sub' or 'id' field found, using email as Subject");
                                userInfo.Subject = userInfo.Email;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse additional fields from user info");
                        }

                        if (string.IsNullOrEmpty(userInfo.Subject))
                        {
                            _logger.LogError("User info missing Subject/id/email field. JSON: {Json}", json);
                            throw new UserInfoException($"Google user info endpoint returned invalid response. Missing 'sub', 'id', or 'email' field. JSON: {json}");
                        }
                    }

                    _logger.LogInformation("Successfully retrieved Google user info. Email: {Email}, Subject: {Subject}", 
                        userInfo.Email, userInfo.Subject);
                    return userInfo;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get Google user info. Status: {Status}, Response: {Response}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UserInfoException(
                        "Failed to retrieve Google user info: Invalid or expired access token.");
                }

                throw new UserInfoException(
                    $"Failed to retrieve Google user info: {response.StatusCode}.");
            }
            catch (OAuthException)
            {
                // Re-throw OAuth-specific exceptions
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open - Google OAuth service is unavailable");
                throw new UserInfoException(
                    "Google OAuth service is temporarily unavailable. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Google OAuth user info retrieval timed out");
                throw new UserInfoException("Google OAuth user info retrieval timed out. Please try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during Google OAuth user info retrieval");
                throw new UserInfoException(
                    "Network error occurred during Google OAuth user info retrieval. Please check your connection and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google OAuth user info retrieval");
                throw new UserInfoException(
                    "An unexpected error occurred during Google OAuth user info retrieval.", ex);
            }
            finally
            {
                // Clear authorization header after use
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
    }
}

