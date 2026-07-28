namespace Ecommerce.Infrastructure.Services;

public static class MicrosoftOAuthEndpointBuilder
{
    public static string BuildV2Endpoint(string? authority, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(authority) ||
            !Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri) ||
            !string.IsNullOrEmpty(authorityUri.UserInfo) ||
            !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OAuth Authority must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(endpoint) ||
            endpoint.Contains('/') ||
            endpoint.Contains('\\'))
        {
            throw new ArgumentException("OAuth endpoint name is invalid.", nameof(endpoint));
        }

        var authorityPath = authorityUri.AbsolutePath.TrimEnd('/');
        const string versionSuffix = "/v2.0";
        if (authorityPath.EndsWith(versionSuffix, StringComparison.OrdinalIgnoreCase))
        {
            authorityPath = authorityPath[..^versionSuffix.Length];
        }

        var builder = new UriBuilder(authorityUri)
        {
            Path = $"{authorityPath}/oauth2/v2.0/{endpoint}",
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.AbsoluteUri;
    }
}
