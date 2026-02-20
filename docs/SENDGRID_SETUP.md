# SendGrid Email Setup for E-Commerce Application

## Overview
This application now includes SendGrid integration for sending order confirmation emails to customers after successful order placement.

## Setup Instructions

### 1. Create a SendGrid Account
1. Go to [SendGrid.com](https://sendgrid.com) and create a free account
2. Verify your email address
3. Complete the account setup process

### 2. Get Your SendGrid API Key
1. Log in to your SendGrid dashboard
2. Navigate to **Settings** → **API Keys**
3. Click **Create API Key**
4. Choose **Full Access** or **Restricted Access** (Mail Send permissions only)
5. Copy the generated API key

### 3. Configure the Application
1. Open `EcommerceAPI/Ecommerce.API/appsettings.json`
2. Replace `"YOUR_SENDGRID_API_KEY_HERE"` with your actual SendGrid API key:

```json
"SendGrid": {
  "ApiKey": "SG.your_actual_api_key_here"
}
```

### 4. Verify Sender Email
The application is configured to send emails from `sankalpkamdi143@gmail.com`. To use a different email:

1. **Option 1**: Update the EmailService constructor in `EcommerceAPI/Ecommerce.Infrastructure/Services/EmailService.cs`
2. **Option 2**: Add configuration to `appsettings.json`:

```json
"SendGrid": {
  "ApiKey": "SG.your_actual_api_key_here",
  "FromEmail": "your-email@yourdomain.com",
  "FromName": "Your Store Name"
}
```

### 5. Verify Your Sender Email in SendGrid
1. In SendGrid dashboard, go to **Settings** → **Sender Authentication**
2. Verify your sender email address
3. This is required for production use

## Email Template Features

The order confirmation email includes:

- **Professional Design**: Modern, responsive HTML template
- **Order Details**: Order ID, date, status
- **Shipping Information**: Address and phone number
- **Product List**: Product names, quantities, prices, and images
- **Total Amount**: Complete order summary
- **Next Steps**: Information about order processing and delivery
- **Contact Information**: Customer support details

## Testing

### 1. Test Order Placement
1. Start the API server
2. Place an order through the frontend
3. Check the console logs for email sending status
4. Verify the email is received

### 2. Check Logs
The application logs email sending status:
- Success: "Order confirmation email sent successfully to {email}"
- Failure: "Failed to send order confirmation email to {email}. Status: {statusCode}"

## Troubleshooting

### Common Issues

1. **"SendGrid API key is not configured"**
   - Ensure the API key is properly set in `appsettings.json`

2. **"Failed to send email"**
   - Verify your SendGrid API key is correct
   - Check if your sender email is verified in SendGrid
   - Ensure you have sufficient SendGrid credits

3. **Emails not received**
   - Check spam/junk folders
   - Verify the recipient email address
   - Check SendGrid dashboard for delivery status

### SendGrid Dashboard
- Monitor email delivery in SendGrid dashboard
- Check **Activity** → **Email Activity** for delivery status
- Review **Reports** for delivery statistics

## Security Notes

- Never commit your SendGrid API key to version control
- Use environment variables or secure configuration management in production
- Consider using SendGrid's IP whitelisting for additional security

## Production Deployment

For production deployment:

1. Use environment variables for the API key
2. Verify your domain in SendGrid
3. Set up proper DNS records (SPF, DKIM, DMARC)
4. Monitor email delivery rates and bounces
5. Consider implementing email templates in SendGrid's template editor

## Support

If you encounter issues:
1. Check SendGrid's documentation
2. Review application logs
3. Verify configuration settings
4. Test with SendGrid's email testing tools

