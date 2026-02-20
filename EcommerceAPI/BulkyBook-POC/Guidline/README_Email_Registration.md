# Email Notifications System

## Overview
This system provides automatic email notifications for various events in the e-commerce application, including user registration and new product announcements.

## Features

### 1. Registration Confirmation Email
Automatically sends a welcome email to users when they successfully register for an account.

### 2. Product Notification Email
Automatically sends notification emails to all users with "User" role when an admin adds a new product to the store.

## Implementation Details

### 1. Email Service Interface
- **File**: `Ecommerce.Domain/Interfaces/IEmailService.cs`
- **Methods**:
  - `SendRegistrationConfirmationEmailAsync(string userEmail, string firstName, string lastName)`
  - `SendProductNotificationToAllUsersAsync(string productName, string productDescription, decimal productPrice, string productImageUrl, string categoryName)`

### 2. Email Service Implementation
- **File**: `Ecommerce.Infrastructure/Services/EmailService.cs`
- **Features**:
  - Uses SendGrid for email delivery
  - Simple and short email templates
  - Both HTML and plain text versions
  - Graceful error handling (doesn't break processes if email fails)
  - Bulk email sending for product notifications

### 3. Registration Handler Update
- **File**: `Ecommerce.Application/Features/Auth/Handlers/RegisterUserHandler.cs`
- **Changes**:
  - Injected `IEmailService` dependency
  - Sends confirmation email after successful user creation
  - Error handling ensures registration process continues even if email fails

### 4. Product Creation Handler Update
- **File**: `Ecommerce.Application/Features/Products/Handlers/CreateProductHandler.cs`
- **Changes**:
  - Injected `IEmailService` dependency
  - Sends notification email to all users after successful product creation
  - Retrieves category information for the email
  - Error handling ensures product creation process continues even if email fails

### 5. Configuration
- **File**: `Ecommerce.API/appsettings.json`
- **SendGrid Settings**:
  ```json
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "sankalpkamdi143@gmail.com",
    "FromName": "E-Commerce Store"
  }
  ```

## Email Templates

### Registration Confirmation Email
- **HTML Version**: Modern, responsive design with gradient header
- **Plain Text Version**: Simple text format for compatibility
- **Content**: Welcome message, call-to-action button, professional footer

### Product Notification Email
- **HTML Version**: Product card layout with image, price, and details
- **Plain Text Version**: Simple text format with product information
- **Content**: New product alert, product details, call-to-action button
- **Recipients**: All users with "User" role and "Active" status

## Testing
- Unit tests added in `Ecommerce.Tests/EmailServiceTests.cs`
- Tests verify methods don't throw exceptions with invalid API keys
- Graceful error handling ensures processes are not affected

## Usage

### Registration Email
The email is automatically sent when a user successfully registers. No additional code is required.

### Product Notification Email
The email is automatically sent when an admin successfully creates a new product. All users with "User" role will receive the notification.

## Error Handling
- If SendGrid API is unavailable, errors are logged but don't prevent processes
- If email sending fails, users can still complete registration/product creation
- Console logging provides visibility into email delivery status
- Bulk email sending handles multiple recipients gracefully

## Dependencies
- SendGrid NuGet package (already included)
- MediatR for query handling
- Existing dependency injection setup
- No additional configuration required

## Performance Considerations
- Product notifications are sent asynchronously to avoid blocking the product creation process
- Bulk email sending is used for product notifications to improve efficiency
- Database queries are optimized to only retrieve necessary user information
