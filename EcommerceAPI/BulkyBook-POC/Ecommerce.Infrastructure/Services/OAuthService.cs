using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ecommerce.Infrastructure.Services
{
    public interface IOAuthService
    {
        Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri);
        Task<OAuthUserInfo> GetUserInfoAsync(string accessToken);
    }

    public class OAuthService : IOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthService> _logger;
        private readonly IResiliencePolicyService _resiliencePolicyService;

        public OAuthService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<OAuthService> logger,
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

            // Sanitize inputs - remove any potentially dangerous characters
            code = SanitizeInput(code);
            redirectUri = SanitizeInput(redirectUri);

            // Validate redirect URI format
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriParsed))
                throw new ArgumentException("Invalid redirect URI format.", nameof(redirectUri));

            try
            {
                var authority = _configuration["OAuth:Authority"];
                var clientId = _configuration["OAuth:ClientId"];
                var clientSecret = _configuration["OAuth:ClientSecret"] 
                    ?? throw new InvalidOperationException(
                        "OAuth ClientSecret is not configured. Please set it in User Secrets (development) or Azure Key Vault (production). " +
                        "See SECRETS_MANAGEMENT_GUIDE.md for instructions.");

                if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(clientId))
                    throw new InvalidOperationException("OAuth configuration is incomplete.");

                var tokenEndpoint = $"{authority}/oauth2/v2.0/token";
                
                var parameters = new Dictionary<string, string>
                {
                    {"client_id", clientId},
                    {"client_secret", clientSecret},
                    {"code", code},
                    {"redirect_uri", redirectUri},
                    {"grant_type", "authorization_code"},
                    {"scope", _configuration["OAuth:Scope"] ?? "openid profile email"}
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
                    
                    _logger.LogInformation("Successfully exchanged code for token");
                    return tokenResult;
                }
                
                // Handle specific error responses
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to exchange code for token. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidGrantException(
                        $"OAuth token exchange failed: {response.StatusCode}. The authorization code may be invalid or expired.");
                }

                throw new TokenExchangeException(
                    $"OAuth token exchange failed with status {response.StatusCode}.");
            }
            catch (OAuthException)
            {
                // Re-throw OAuth-specific exceptions
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open - OAuth service is unavailable");
                throw new OAuthException(
                    "OAuth service is temporarily unavailable. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OAuth token exchange timed out");
                throw new TokenExchangeException("OAuth token exchange timed out. Please try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during OAuth token exchange");
                throw new TokenExchangeException(
                    "Network error occurred during OAuth token exchange. Please check your connection and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during OAuth token exchange");
                throw new TokenExchangeException(
                    "An unexpected error occurred during OAuth token exchange.", ex);
            }
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remove any potentially dangerous characters but preserve valid OAuth code/URI characters
            // OAuth codes are typically alphanumeric with some special characters
            return Regex.Replace(input, @"[^\w\-._~:/?#\[\]@!$&'()*+,;=]", "");
        }

        public async Task<OAuthUserInfo> GetUserInfoAsync(string accessToken)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Access token cannot be null or empty.", nameof(accessToken));

            // Sanitize token
            accessToken = SanitizeInput(accessToken);

            try
            {
                var authority = _configuration["OAuth:Authority"];
                if (string.IsNullOrWhiteSpace(authority))
                    throw new InvalidOperationException("OAuth Authority is not configured.");

                var userInfoEndpoint = $"{authority}/openid/userinfo";
                
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
                    var userInfo = JsonSerializer.Deserialize<OAuthUserInfo>(json);
                    
                    if (userInfo == null || string.IsNullOrEmpty(userInfo.Subject))
                    {
                        _logger.LogWarning("User info retrieval succeeded but returned empty result");
                        throw new UserInfoException("User info endpoint returned invalid response.");
                    }
                    
                    _logger.LogInformation("Successfully retrieved user info");
                    return userInfo;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get user info. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UserInfoException(
                        "Failed to retrieve user info: Invalid or expired access token.");
                }

                throw new UserInfoException(
                    $"Failed to retrieve user info: {response.StatusCode}.");
            }
            catch (OAuthException)
            {
                // Re-throw OAuth-specific exceptions
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open - OAuth service is unavailable");
                throw new UserInfoException(
                    "OAuth service is temporarily unavailable. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OAuth user info retrieval timed out");
                throw new UserInfoException("OAuth user info retrieval timed out. Please try again.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during OAuth user info retrieval");
                throw new UserInfoException(
                    "Network error occurred during OAuth user info retrieval. Please check your connection and try again.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during OAuth user info retrieval");
                throw new UserInfoException(
                    "An unexpected error occurred during OAuth user info retrieval.", ex);
            }
            finally
            {
                // Clear authorization header after use
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

    }

    public class OAuthTokenResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    public class OAuthUserInfo
    {
        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("given_name")]
        public string GivenName { get; set; } = string.Empty;
        
        [JsonPropertyName("family_name")]
        public string FamilyName { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }
        
        [JsonPropertyName("picture")]
        public string Picture { get; set; } = string.Empty;
    }
}

