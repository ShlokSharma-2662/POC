namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Base exception for OAuth-related errors
    /// </summary>
    public class OAuthException : Exception
    {
        public OAuthException(string message) : base(message) { }
        public OAuthException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when OAuth grant is invalid (e.g., expired code, invalid code)
    /// </summary>
    public class InvalidGrantException : OAuthException
    {
        public InvalidGrantException(string message) : base(message) { }
        public InvalidGrantException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when OAuth token exchange fails
    /// </summary>
    public class TokenExchangeException : OAuthException
    {
        public TokenExchangeException(string message) : base(message) { }
        public TokenExchangeException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when user info retrieval fails
    /// </summary>
    public class UserInfoException : OAuthException
    {
        public UserInfoException(string message) : base(message) { }
        public UserInfoException(string message, Exception innerException) : base(message, innerException) { }
    }
}

