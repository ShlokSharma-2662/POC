using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;

namespace Ecommerce.Tests.Common
{
    public abstract class ControllerTestBase
    {
        protected Mock<HttpContext> MockHttpContext { get; private set; } = null!;
        protected Mock<ClaimsPrincipal> MockUser { get; private set; } = null!;
        protected Mock<HttpRequest> MockRequest { get; private set; } = null!;
        protected Mock<HttpResponse> MockResponse { get; private set; } = null!;
        protected Mock<ISession> MockSession { get; private set; } = null!;
        protected Dictionary<string, byte[]> SessionData { get; private set; } = new();
        protected Mock<ILogger<T>> CreateMockLogger<T>() => new Mock<ILogger<T>>();

        protected virtual void SetupHttpContext()
        {
            MockHttpContext = new Mock<HttpContext>();
            MockUser = new Mock<ClaimsPrincipal>();
            MockRequest = new Mock<HttpRequest>();
            MockResponse = new Mock<HttpResponse>();
            MockSession = new Mock<ISession>();

            // Setup session methods to work with extension methods
            MockSession.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                      .Callback<string, byte[]>((key, value) => SessionData[key] = value);
            
            MockSession.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                      .Returns((string key, out byte[] value) =>
                      {
                          if (SessionData.TryGetValue(key, out byte[] data))
                          {
                              value = data;
                              return true;
                          }
                          value = null!;
                          return false;
                      });

            MockHttpContext.Setup(x => x.User).Returns(MockUser.Object);
            MockHttpContext.Setup(x => x.Request).Returns(MockRequest.Object);
            MockHttpContext.Setup(x => x.Response).Returns(MockResponse.Object);
            MockHttpContext.Setup(x => x.Session).Returns(MockSession.Object);
        }

        protected void SetupAuthenticatedUser(long userId, string email = "test@example.com", string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            MockUser.Setup(x => x.Identity).Returns(identity);
            MockUser.Setup(x => x.FindFirst(It.IsAny<string>())).Returns<string>(claimType => 
                claims.FirstOrDefault(c => c.Type == claimType));
        }

        protected void SetupUnauthenticatedUser()
        {
            var identity = new ClaimsIdentity();
            MockUser.Setup(x => x.Identity).Returns(identity);
        }

        protected void SetupAdminUser(long userId = 1)
        {
            SetupAuthenticatedUser(userId, "admin@example.com", "Admin");
        }

        protected void SetupRequestScheme(string scheme = "https")
        {
            MockRequest.Setup(x => x.Scheme).Returns(scheme);
        }

        protected void SetupRequestHost(string host = "localhost:5001")
        {
            MockRequest.Setup(x => x.Host).Returns(new HostString(host));
        }

        protected void SetupControllerContext(ControllerBase controller)
        {
            var controllerContext = new ControllerContext
            {
                HttpContext = MockHttpContext.Object
            };
            controller.ControllerContext = controllerContext;
        }

        protected void SetSessionString(string key, string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            SessionData[key] = bytes;
        }

        protected string? GetSessionString(string key)
        {
            if (SessionData.TryGetValue(key, out byte[] bytes))
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            return null;
        }
    }
}
