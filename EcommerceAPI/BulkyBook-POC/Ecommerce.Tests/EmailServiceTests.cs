using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Text.Json;
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
            var mockClient = CreateSuccessfulClient();
            var emailService = new EmailService(
                "test-api-key",
                "test@example.com",
                "Test Store",
                sendGridClient: mockClient.Object);
            var userEmail = "test@example.com";
            var firstName = "John";
            var lastName = "Doe";

            // Act & Assert
            await emailService.SendRegistrationConfirmationEmailAsync(userEmail, firstName, lastName);
            mockClient.Verify(
                client => client.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
                Times.Once);
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

        [Fact]
        public async Task SendOrderConfirmation_WithTemplateId_ShouldSendExpectedDynamicTemplateData()
        {
            // Arrange
            SendGridMessage? capturedMessage = null;
            var mockClient = CreateSuccessfulClient();
            mockClient
                .Setup(client => client.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .Callback<SendGridMessage, CancellationToken>((message, _) => capturedMessage = message)
                .ReturnsAsync(CreateResponse(HttpStatusCode.Accepted));

            var templateId = "d-05dbcb5eb1c5402984f6ab4cf7c41d98";
            var orderId = Guid.Parse("7b682980-d2dd-4c4d-88f1-82fd0dc1c389");
            var emailService = new EmailService(
                "test-api-key",
                " sender@example.com\r\n",
                " Test Store\r\n",
                templateId,
                mockClient.Object,
                "http://localhost:4200",
                "usd");

            // Act
            await emailService.SendOrderConfirmationEmailAsync(
                "customer@example.com",
                "Test Customer",
                orderId.ToString(),
                new List<OrderItemEmailDto>
                {
                    new()
                    {
                        ProductName = "Test Product",
                        Quantity = 2,
                        Price = 9.99m,
                        TotalPrice = 19.98m,
                        ImageUrl = "https://example.com/product.jpg"
                    }
                },
                19.98m,
                "123 Main Street\r\nTest City",
                "+1 555 0100");

            // Assert
            Assert.NotNull(capturedMessage);
            Assert.Equal(templateId, capturedMessage.TemplateId);
            Assert.Equal("sender@example.com", capturedMessage.From.Email);
            Assert.Equal("Test Store", capturedMessage.From.Name);
            Assert.Equal("customer@example.com", capturedMessage.Personalizations.Single().Tos.Single().Email);
            Assert.True(capturedMessage.Contents == null || capturedMessage.Contents.Count == 0);

            var templateDataJson = JsonSerializer.Serialize(capturedMessage.Personalizations.Single().TemplateData);
            using var templateData = JsonDocument.Parse(templateDataJson);
            var root = templateData.RootElement;

            Assert.Equal("Test Customer", root.GetProperty("customerName").GetString());
            Assert.Equal("ORD-7B682980-D2DD-4C4D-88F1-82FD0DC1C389", root.GetProperty("orderNumber").GetString());
            Assert.Equal("$19.98", root.GetProperty("total").GetString());
            Assert.Equal("http://localhost:4200/my-orders", root.GetProperty("orderUrl").GetString());
            Assert.Equal(2, root.GetProperty("shippingAddressLines").GetArrayLength());

            var item = root.GetProperty("orderItems")[0];
            Assert.Equal("Test Product", item.GetProperty("productName").GetString());
            Assert.Equal(2, item.GetProperty("quantity").GetInt32());
            Assert.Equal("$9.99", item.GetProperty("unitPrice").GetString());
            Assert.Equal("https://example.com/product.jpg", item.GetProperty("imageUrl").GetString());
        }

        [Fact]
        public async Task SendOrderConfirmation_WithoutTemplateId_ShouldKeepInlineFallback()
        {
            // Arrange
            SendGridMessage? capturedMessage = null;
            var mockClient = CreateSuccessfulClient();
            mockClient
                .Setup(client => client.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .Callback<SendGridMessage, CancellationToken>((message, _) => capturedMessage = message)
                .ReturnsAsync(CreateResponse(HttpStatusCode.Accepted));
            var emailService = new EmailService(
                "test-api-key",
                "sender@example.com",
                "Test Store",
                sendGridClient: mockClient.Object);

            // Act
            await emailService.SendOrderConfirmationEmailAsync(
                "customer@example.com",
                "Test Customer",
                Guid.NewGuid().ToString(),
                new List<OrderItemEmailDto>(),
                0m,
                "123 Main Street",
                "+1 555 0100");

            // Assert
            Assert.NotNull(capturedMessage);
            Assert.True(string.IsNullOrEmpty(capturedMessage.TemplateId));
            Assert.Contains(capturedMessage.Contents, content => content.Type == "text/plain");
            Assert.Contains(capturedMessage.Contents, content => content.Type == "text/html");
        }

        [Fact]
        public void EmailService_Constructor_WithWhitespaceApiKey_ShouldThrowException()
        {
            Assert.Throws<InvalidOperationException>(() => new EmailService("   "));
        }

        private static Mock<ISendGridClient> CreateSuccessfulClient()
        {
            var mockClient = new Mock<ISendGridClient>();
            mockClient
                .Setup(client => client.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateResponse(HttpStatusCode.Accepted));
            return mockClient;
        }

        private static Response CreateResponse(HttpStatusCode statusCode)
        {
            var httpResponse = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty)
            };
            return new Response(statusCode, httpResponse.Content, httpResponse.Headers);
        }
    }
}
