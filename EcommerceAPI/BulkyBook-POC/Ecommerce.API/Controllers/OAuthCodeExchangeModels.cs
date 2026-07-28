using System.Text.Json.Serialization;

namespace Ecommerce.API.Controllers;

public sealed record OAuthCodeExchangeRequest(
    [property: JsonPropertyName("code")] string Code);

public sealed record OAuthCodeExchangeResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("userId")] long UserId,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("picture")] string? Picture,
    [property: JsonPropertyName("returnTo")] string? ReturnTo);
