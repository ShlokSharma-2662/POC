using System.Collections.Generic;

namespace Ecommerce.Domain.Interfaces
{
    public interface IEmailService
    {
        Task NotifyAdminAsync(string message);
        Task SendOrderConfirmationEmailAsync(string userEmail, string userName, string orderId, List<OrderItemEmailDto> orderItems, decimal totalAmount, string shippingAddress, string phoneNumber);
        Task SendRegistrationConfirmationEmailAsync(string userEmail, string firstName, string lastName);
        Task SendProductNotificationToAllUsersAsync(string productName, string productDescription, decimal productPrice, string productImageUrl, string categoryName);
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent, string plainTextContent);
    }

    public class OrderItemEmailDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}
