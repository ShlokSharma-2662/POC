using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.API.Security;

/// <summary>
/// Builds OAuth callback and frontend redirect URLs without trusting caller-controlled hosts.
/// </summary>
public static class OAuthRedirectUrlHelper
{
    private const string DefaultFrontendOrigin = "http://localhost:4200";
    private const string DefaultFrontendCallbackPath = "/oauth/callback";

    public static string BuildCallbackUri(
        HttpRequest request,
        IConfiguration configuration,
        string configurationSection,
        string defaultCallbackPath)
    {
        var callbackPath = configuration[$"{configurationSection}:CallbackPath"] ?? defaultCallbackPath;
        if (!IsSafeLocalPath(callbackPath))
        {
            throw new InvalidOperationException(
                $"{configurationSection}:CallbackPath must be an application-local absolute path.");
        }

        var configuredBaseUrl = configuration[$"{configurationSection}:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            if (!TryCreateHttpUri(configuredBaseUrl, out var configuredBaseUri))
            {
                throw new InvalidOperationException(
                    $"{configurationSection}:BaseUrl must be an absolute HTTP or HTTPS URL.");
            }

            return new Uri(new Uri(GetOrigin(configuredBaseUri)), callbackPath).AbsoluteUri;
        }

        if (!request.Host.HasValue ||
            !string.Equals(request.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unable to determine the public callback URL for {configurationSection}.");
        }

        return $"{request.Scheme}://{request.Host}{callbackPath}";
    }

    public static bool TryResolveFrontendReturnUrl(
        IConfiguration configuration,
        string? returnUrl,
        out string resolvedReturnUrl)
    {
        var frontendBaseUri = GetFrontendBaseUri(configuration);
        resolvedReturnUrl = new Uri(frontendBaseUri, DefaultFrontendCallbackPath).AbsoluteUri;

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return true;
        }

        returnUrl = returnUrl.Trim();
        if (IsSafeLocalPath(returnUrl))
        {
            resolvedReturnUrl = new Uri(frontendBaseUri, returnUrl).AbsoluteUri;
            return true;
        }

        if (!TryCreateHttpUri(returnUrl, out var absoluteReturnUri))
        {
            return false;
        }

        var allowedOrigins = GetAllowedFrontendOrigins(configuration);
        if (!allowedOrigins.Contains(GetOrigin(absoluteReturnUri), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        resolvedReturnUrl = absoluteReturnUri.AbsoluteUri;
        return true;
    }

    public static bool TryResolveFrontendReturnPath(
        string? returnTo,
        out string? resolvedReturnTo)
    {
        resolvedReturnTo = null;
        if (string.IsNullOrWhiteSpace(returnTo))
        {
            return true;
        }

        returnTo = returnTo.Trim();
        if (!IsSafeLocalPath(returnTo))
        {
            return false;
        }

        resolvedReturnTo = returnTo;
        return true;
    }

    public static string AppendAuthorizationCode(
        string redirectUrl,
        string authorizationCode,
        string provider)
    {
        if (!TryCreateHttpUri(redirectUrl, out var redirectUri))
        {
            throw new InvalidOperationException("The OAuth completion redirect URL is invalid.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var builder = new UriBuilder(redirectUri);
        var existingQuery = builder.Query.TrimStart('?');
        var completionParameters =
            $"code={Uri.EscapeDataString(authorizationCode)}&" +
            $"provider={Uri.EscapeDataString(provider.Trim().ToLowerInvariant())}";
        builder.Query = string.IsNullOrEmpty(existingQuery)
            ? completionParameters
            : $"{existingQuery}&{completionParameters}";

        return builder.Uri.AbsoluteUri;
    }

    private static Uri GetFrontendBaseUri(IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["Frontend:BaseUrl"];
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return new Uri(DefaultFrontendOrigin);
        }

        if (!TryCreateHttpUri(configuredBaseUrl, out var configuredBaseUri))
        {
            throw new InvalidOperationException(
                "Frontend:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        return new Uri(GetOrigin(configuredBaseUri));
    }

    private static HashSet<string> GetAllowedFrontendOrigins(IConfiguration configuration)
    {
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            GetOrigin(GetFrontendBaseUri(configuration))
        };

        var allowedOriginsSection = configuration.GetSection("Frontend:AllowedOrigins");
        if (allowedOriginsSection is not null)
        {
            AddConfiguredOrigins(origins, allowedOriginsSection.Value);
            foreach (var child in allowedOriginsSection.GetChildren())
            {
                AddConfiguredOrigins(origins, child.Value);
            }
        }

        return origins;
    }

    private static void AddConfiguredOrigins(ISet<string> origins, string? configuredOrigins)
    {
        if (string.IsNullOrWhiteSpace(configuredOrigins))
        {
            return;
        }

        foreach (var configuredOrigin in configuredOrigins.Split(
                     ',',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryCreateHttpUri(configuredOrigin, out var originUri))
            {
                origins.Add(GetOrigin(originUri));
            }
        }
    }

    private static bool IsSafeLocalPath(string value)
    {
        return value.StartsWith("/", StringComparison.Ordinal) &&
               !value.StartsWith("//", StringComparison.Ordinal) &&
               !value.Contains('\\') &&
               Uri.TryCreate(value, UriKind.Relative, out _);
    }

    private static bool TryCreateHttpUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsedUri) &&
            (string.Equals(parsedUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrEmpty(parsedUri.UserInfo))
        {
            uri = parsedUri;
            return true;
        }

        uri = null!;
        return false;
    }

    private static string GetOrigin(Uri uri) => uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
}
