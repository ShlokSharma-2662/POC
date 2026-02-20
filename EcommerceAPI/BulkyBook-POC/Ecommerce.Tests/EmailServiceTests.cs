using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ecommerce.Tests
{
    public class EmailServiceTests
    {
        [Fact]
        public void EmailService_Constructor_WithApiKey_ShouldInitializeCorrectly()
        {
            // Arrange
            var apiKey = "test-api-key";
            var fromEmail = "test@example.com";
            var fromName = "Test Store";

            // Act
            var emailService = new EmailService(apiKey, fromEmail, fromName);

            // Assert
            Assert.NotNull(emailService);
        }

        [Fact]
        public void EmailService_Constructor_WithConfiguration_MissingApiKey_ShouldThrowException()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x["SendGrid:ApiKey"]).Returns((string)null);

            // Act & Assert
            var mockLogger = new Mock<ILogger<EmailService>>();
            Assert.Throws<InvalidOperationException>(() => new EmailService(mockConfiguration.Object, null!, mockLogger.Object));
        }

        [Fact]
        public async Task EmailService_SendRegistrationConfirmationEmailAsync_ShouldNotThrowException()
        {
            // Arrange
            var emailService = new EmailService("test-api-key", "test@example.com", "Test Store");
            var userEmail = "test@example.com";
            var firstName = "John";
            var lastName = "Doe";

            // Act & Assert
            await emailService.SendRegistrationConfirmationEmailAsync(userEmail, firstName, lastName);
        }

        [Fact]
        public async Task EmailService_SendProductNotificationToAllUsersAsync_ShouldNotThrowException()
        {
            // Arrange
            var emailService = new EmailService("test-api-key", "test@example.com", "Test Store");
            var productName = "Test Product";
            var productDescription = "Test Description";
            var productPrice = 99.99m;
            var productImageUrl = "https://example.com/image.jpg";
            var categoryName = "Electronics";

            // Act & Assert
            await emailService.SendProductNotificationToAllUsersAsync(productName, productDescription, productPrice, productImageUrl, categoryName);
        }
    }
}
