using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Ecommerce.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly AppDbContext? _context;
        private readonly ILogger<EmailService>? _logger;
        private readonly IResiliencePolicyService? _resiliencePolicyService;
        private readonly IEmailFailureTracker? _failureTracker;

        public EmailService(string sendGridApiKey, string fromEmail = "noreply@ecommerce.com", string fromName = "E-Commerce Store")
        {
            _sendGridApiKey = sendGridApiKey;
            _fromEmail = fromEmail;
            _fromName = fromName;
            _context = null;
            _logger = null;
            _resiliencePolicyService = null;
            _failureTracker = null;
        }

        public EmailService(
            IConfiguration configuration, 
            AppDbContext context, 
            ILogger<EmailService> logger,
            IResiliencePolicyService? resiliencePolicyService = null,
            IEmailFailureTracker? failureTracker = null)
        {
            _sendGridApiKey = configuration["SendGrid:ApiKey"] 
                ?? throw new InvalidOperationException(
                    "SendGrid API key is not configured. Please set it in User Secrets (development) or Azure Key Vault (production). " +
                    "See SECRETS_MANAGEMENT_GUIDE.md for instructions.");
            _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@ecommerce.com";
            _fromName = configuration["SendGrid:FromName"] ?? "E-Commerce Store";
            _context = context;
            _logger = logger;
            _resiliencePolicyService = resiliencePolicyService;
            _failureTracker = failureTracker;
        }

        public async Task NotifyAdminAsync(string message)
        {
            _logger?.LogInformation("Admin Notification: {Message}", message);
            await Task.CompletedTask;
        }

        public async Task SendOrderConfirmationEmailAsync(string userEmail, string userName, string orderId, List<OrderItemEmailDto> orderItems, decimal totalAmount, string shippingAddress, string phoneNumber)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(userEmail))
                throw new ArgumentException("User email cannot be null or empty.", nameof(userEmail));
            
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID cannot be null or empty.", nameof(orderId));

            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(userEmail, userName);
                var subject = $"Order Confirmation - Order #{orderId}";
                var htmlContent = GenerateOrderConfirmationEmailHtml(userName, orderId, orderItems, totalAmount, shippingAddress, phoneNumber);
                var plainTextContent = GenerateOrderConfirmationEmailText(userName, orderId, orderItems, totalAmount, shippingAddress, phoneNumber);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                
                // Apply retry and circuit breaker policies if available
                if (_resiliencePolicyService != null)
                {
                    var retryPolicy = _resiliencePolicyService.GetRetryPolicy<SendGrid.Response>();
                    var circuitBreakerPolicy = _resiliencePolicyService.GetCircuitBreakerPolicy<SendGrid.Response>();
                    var policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
                    
                    var response = await policy.ExecuteAsync(async () =>
                    {
                        return await client.SendEmailAsync(msg);
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("Order confirmation email sent successfully to {UserEmail} for order {OrderId}", userEmail, orderId);
                        _failureTracker?.RecordSuccess("OrderConfirmation", userEmail);
                    }
                    else
                    {
                        var errorBody = await response.Body.ReadAsStringAsync();
                        _logger?.LogWarning("Failed to send order confirmation email to {UserEmail} for order {OrderId}. Status: {StatusCode}, Body: {Body}", 
                            userEmail, orderId, response.StatusCode, errorBody);
                        
                        _failureTracker?.RecordFailure("OrderConfirmation", userEmail);
                        
                        // For critical emails like order confirmations, throw exception to allow caller to handle
                        // This ensures order flow can be aware of email failures
                        throw new InvalidOperationException(
                            $"Failed to send order confirmation email. Status: {response.StatusCode}. " +
                            "The order was processed, but the confirmation email could not be sent. Please contact support.");
                    }
                }
                else
                {
                    // Fallback to direct call if resilience policies not available
                    var response = await client.SendEmailAsync(msg);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("Order confirmation email sent successfully to {UserEmail} for order {OrderId}", userEmail, orderId);
                        _failureTracker?.RecordSuccess("OrderConfirmation", userEmail);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to send order confirmation email to {UserEmail} for order {OrderId}. Status: {StatusCode}", 
                            userEmail, orderId, response.StatusCode);
                        
                        _failureTracker?.RecordFailure("OrderConfirmation", userEmail);
                        
                        // For critical emails, throw exception
                        throw new InvalidOperationException(
                            $"Failed to send order confirmation email. Status: {response.StatusCode}. " +
                            "The order was processed, but the confirmation email could not be sent.");
                    }
                }
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogError(ex, "Circuit breaker is open - SendGrid service is unavailable. Email to {UserEmail} for order {OrderId} could not be sent.", 
                    userEmail, orderId);
                _failureTracker?.RecordFailure("OrderConfirmation", userEmail);
                
                // For critical emails, throw to allow caller to handle (e.g., queue for retry)
                throw new InvalidOperationException(
                    "Email service is temporarily unavailable due to circuit breaker. " +
                    "The order was processed, but the confirmation email could not be sent. Please contact support.", ex);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger?.LogError(ex, "Error sending order confirmation email to {UserEmail} for order {OrderId}", userEmail, orderId);
                _failureTracker?.RecordFailure("OrderConfirmation", userEmail);
                
                // For critical emails, throw to allow caller to handle
                throw new InvalidOperationException(
                    "An error occurred while sending the order confirmation email. " +
                    "The order was processed, but the confirmation email could not be sent. Please contact support.", ex);
            }
        }

        public async Task SendRegistrationConfirmationEmailAsync(string userEmail, string firstName, string lastName)
        {
            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(userEmail, $"{firstName} {lastName}");
                var subject = "Welcome to Our E-Commerce Store! 🎉";
                var htmlContent = GenerateRegistrationConfirmationEmailHtml(firstName, lastName);
                var plainTextContent = GenerateRegistrationConfirmationEmailText(firstName, lastName);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("Registration confirmation email sent successfully to {UserEmail}", userEmail);
                }
                else
                {
                    _logger?.LogWarning("Failed to send registration confirmation email to {UserEmail}. Status: {StatusCode}", userEmail, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending registration confirmation email to {UserEmail}", userEmail);
            }
        }

        public async Task SendProductNotificationToAllUsersAsync(string productName, string productDescription, decimal productPrice, string productImageUrl, string categoryName)
        {
            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var subject = $"🆕 New Product Alert: {productName}";
                var htmlContent = GenerateProductNotificationEmailHtml(productName, productDescription, productPrice, productImageUrl, categoryName);
                var plainTextContent = GenerateProductNotificationEmailText(productName, productDescription, productPrice, categoryName);

                var userEmails = await GetUserEmailsAsync();

                if (userEmails.Any())
                {
                    var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, userEmails, subject, plainTextContent, htmlContent);
                    var response = await client.SendEmailAsync(msg);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger?.LogInformation("Product notification email sent successfully to {UserCount} users for product {ProductName}", userEmails.Count, productName);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to send product notification email for product {ProductName}. Status: {StatusCode}", productName, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending product notification email for product {ProductName}", productName);
            }
        }

        private async Task<List<EmailAddress>> GetUserEmailsAsync()
        {
            try
            {
                if (_context == null)
                {
                    _logger?.LogWarning("Database context is not available for getting user emails");
                    return new List<EmailAddress>();
                }

                var userEmails = await _context.Users
                    .Where(u => u.Role == "User" && u.Status == "Active" && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email!)
                    .ToListAsync();
                
                return userEmails.Select(email => new EmailAddress(email)).ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting user emails for product notification");
                return new List<EmailAddress>();
            }
        }

        private string GenerateProductNotificationEmailHtml(string productName, string productDescription, decimal productPrice, string productImageUrl, string categoryName)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>New Product Alert</title>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
                    .content {{ padding: 30px; }}
                    .product-card {{ background-color: #f8f9fa; border-radius: 10px; padding: 20px; margin: 20px 0; }}
                    .product-image {{ width: 100%; max-width: 200px; height: auto; border-radius: 8px; margin: 15px 0; }}
                    .price {{ font-size: 24px; color: #667eea; font-weight: bold; margin: 10px 0; }}
                    .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='margin: 0; font-size: 28px;'>🆕 New Product Alert!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Check out our latest addition</p>
                    </div>
                    
                    <div class='content'>
                        <p style='font-size: 18px; color: #333; margin-bottom: 20px;'>Hello there!</p>
                        
                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            We're excited to announce a new product has been added to our store. Don't miss out!
                        </p>

                        <div class='product-card'>
                            <h2 style='margin: 0 0 10px 0; color: #333;'>{productName}</h2>
                            <p style='margin: 5px 0; color: #666;'><strong>Category:</strong> {categoryName}</p>
                            <p style='margin: 5px 0; color: #666;'><strong>Description:</strong> {productDescription}</p>
                            <div class='price'>₹{productPrice:F2}</div>
                            {(string.IsNullOrEmpty(productImageUrl) ? "" : $@"<img src='{productImageUrl}' alt='{productName}' class='product-image'>")}
                        </div>

                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='#' class='btn'>View Product Now</a>
                        </div>

                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            Be the first to check it out and grab it before it's gone!
                        </p>
                    </div>

                    <div class='footer'>
                        <p style='margin: 0;'>Happy shopping!<br>The E-Commerce Team</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateProductNotificationEmailText(string productName, string productDescription, decimal productPrice, string categoryName)
        {
            return $@"
New Product Alert! 🆕

Hello there!

We're excited to announce a new product has been added to our store. Don't miss out!

Product: {productName}
Category: {categoryName}
Description: {productDescription}
Price: ₹{productPrice:F2}

Be the first to check it out and grab it before it's gone!

Happy shopping!
The E-Commerce Team";
        }

        private string GenerateOrderConfirmationEmailHtml(string userName, string orderId, List<OrderItemEmailDto> orderItems, decimal totalAmount, string shippingAddress, string phoneNumber)
        {
            var itemsHtml = string.Join("", orderItems.Select(item => $@"
                <tr>
                    <td style='padding: 15px; border-bottom: 1px solid #eee;'>
                        <div style='display: flex; align-items: center;'>
                            {(string.IsNullOrEmpty(item.ImageUrl) ? "" : $@"<img src='{item.ImageUrl}' alt='{item.ProductName}' style='width: 60px; height: 60px; object-fit: cover; border-radius: 8px; margin-right: 15px;'>")}
                            <div>
                                <h4 style='margin: 0 0 5px 0; color: #333; font-size: 16px;'>{item.ProductName}</h4>
                                <p style='margin: 0; color: #666; font-size: 14px;'>Quantity: {item.Quantity}</p>
                            </div>
                        </div>
                    </td>
                    <td style='padding: 15px; border-bottom: 1px solid #eee; text-align: right; color: #333; font-weight: 600;'>₹{item.Price:F2}</td>
                    <td style='padding: 15px; border-bottom: 1px solid #eee; text-align: right; color: #333; font-weight: 600;'>₹{item.TotalPrice:F2}</td>
                </tr>"));

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Order Confirmation</title>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
                    .content {{ padding: 30px; }}
                    .order-details {{ background-color: #f8f9fa; border-radius: 10px; padding: 20px; margin: 20px 0; }}
                    .order-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                    .order-table th {{ background-color: #667eea; color: white; padding: 15px; text-align: left; }}
                    .order-table td {{ padding: 15px; border-bottom: 1px solid #eee; }}
                    .total-row {{ background-color: #e3f2fd; font-weight: bold; }}
                    .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                    .status-badge {{ display: inline-block; padding: 8px 16px; background-color: #4caf50; color: white; border-radius: 20px; font-size: 14px; font-weight: bold; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='margin: 0; font-size: 28px;'>🎉 Order Confirmed!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Thank you for your purchase</p>
                    </div>
                    
                    <div class='content'>
                        <p style='font-size: 18px; color: #333; margin-bottom: 20px;'>Hi {userName},</p>
                        
                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            Your order has been successfully placed and is being processed. We're excited to get your items ready for shipping!
                        </p>

                        <div class='order-details'>
                            <h3 style='margin: 0 0 15px 0; color: #333;'>📋 Order Details</h3>
                            <p style='margin: 5px 0; color: #666;'><strong>Order ID:</strong> #{orderId}</p>
                            <p style='margin: 5px 0; color: #666;'><strong>Order Date:</strong> {DateTime.Now.ToString("MMMM dd, yyyy")}</p>
                            <p style='margin: 5px 0; color: #666;'><strong>Status:</strong> <span class='status-badge'>Confirmed</span></p>
                        </div>

                        <div class='order-details'>
                            <h3 style='margin: 0 0 15px 0; color: #333;'>📍 Shipping Information</h3>
                            <p style='margin: 5px 0; color: #666;'><strong>Address:</strong> {shippingAddress}</p>
                            <p style='margin: 5px 0; color: #666;'><strong>Phone:</strong> {phoneNumber}</p>
                        </div>

                        <h3 style='margin: 30px 0 15px 0; color: #333;'>🛒 Order Items</h3>
                        <table class='order-table'>
                            <thead>
                                <tr>
                                    <th style='width: 50%;'>Product</th>
                                    <th style='text-align: right;'>Unit Price</th>
                                    <th style='text-align: right;'>Total</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemsHtml}
                                <tr class='total-row'>
                                    <td colspan='2' style='text-align: right; padding: 20px 15px; font-size: 18px;'><strong>Total Amount:</strong></td>
                                    <td style='text-align: right; padding: 20px 15px; font-size: 18px; color: #667eea;'><strong>₹{totalAmount:F2}</strong></td>
                                </tr>
                            </tbody>
                        </table>

                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='#' class='btn' style='background-color: #667eea; color: white; text-decoration: none; padding: 12px 24px; border-radius: 5px; display: inline-block;'>Track Your Order</a>
                        </div>

                        <div style='background-color: #e8f5e8; border-left: 4px solid #4caf50; padding: 15px; margin: 20px 0; border-radius: 0 5px 5px 0;'>
                            <h4 style='margin: 0 0 10px 0; color: #2e7d32;'>📦 What's Next?</h4>
                            <ul style='margin: 0; padding-left: 20px; color: #2e7d32;'>
                                <li>We'll process your order within 24 hours</li>
                                <li>You'll receive a shipping confirmation email</li>
                                <li>Your order will be delivered within 3-5 business days</li>
                            </ul>
                        </div>

                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            If you have any questions about your order, please don't hesitate to contact our customer support team.
                        </p>
                    </div>

                    <div class='footer'>
                        <p style='margin: 0 0 10px 0;'>Thank you for choosing our store!</p>
                        <p style='margin: 0; font-size: 14px;'>This email was sent from noreply@ecommerce.com</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateRegistrationConfirmationEmailHtml(string firstName, string lastName)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Welcome to Our Store</title>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
                    .content {{ padding: 30px; }}
                    .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background-color: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 style='margin: 0; font-size: 28px;'>🎉 Welcome!</h1>
                        <p style='margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;'>Your account has been created successfully</p>
                    </div>
                    
                    <div class='content'>
                        <p style='font-size: 18px; color: #333; margin-bottom: 20px;'>Hi {firstName},</p>
                        
                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            Welcome to our e-commerce store! Your account has been successfully created and you're now ready to start shopping.
                        </p>

                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='#' class='btn'>Start Shopping Now</a>
                        </div>

                        <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                            Thank you for choosing us. Happy shopping!
                        </p>
                    </div>

                    <div class='footer'>
                        <p style='margin: 0;'>Best regards,<br>The E-Commerce Team</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private string GenerateRegistrationConfirmationEmailText(string firstName, string lastName)
        {
            return $@"
Welcome to Our E-Commerce Store! 🎉

Hi {firstName},

Welcome to our e-commerce store! Your account has been successfully created and you're now ready to start shopping.

Thank you for choosing us. Happy shopping!

Best regards,
The E-Commerce Team";
        }

        private string GenerateOrderConfirmationEmailText(string userName, string orderId, List<OrderItemEmailDto> orderItems, decimal totalAmount, string shippingAddress, string phoneNumber)
        {
            var itemsText = string.Join("\n", orderItems.Select(item => $"- {item.ProductName} (Qty: {item.Quantity}) - ₹{item.TotalPrice:F2}"));

            return $@"
Order Confirmation - Order #{orderId}

Hi {userName},

Your order has been successfully placed and is being processed. We're excited to get your items ready for shipping!

ORDER DETAILS:
Order ID: #{orderId}
Order Date: {DateTime.Now.ToString("MMMM dd, yyyy")}
Status: Confirmed

SHIPPING INFORMATION:
Address: {shippingAddress}
Phone: {phoneNumber}

ORDER ITEMS:
{itemsText}

Total Amount: ₹{totalAmount:F2}

What's Next?
- We'll process your order within 24 hours
- You'll receive a shipping confirmation email
- Your order will be delivered within 3-5 business days

If you have any questions about your order, please don't hesitate to contact our customer support team.

Thank you for choosing our store!

This email was sent from noreply@ecommerce.com";
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent, string plainTextContent)
        {
            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, toName);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
                }
                else
                {
                    _logger?.LogWarning("Failed to send email to {ToEmail} with subject '{Subject}'. Status: {StatusCode}", toEmail, subject, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending email to {ToEmail} with subject '{Subject}'", toEmail, subject);
                throw;
            }
        }
    }
}
