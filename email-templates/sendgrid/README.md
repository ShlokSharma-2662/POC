# ShopSphere SendGrid order-confirmation template

This package contains:

- `order-confirmation.html`: responsive SendGrid dynamic-template HTML;
- `order-confirmation.txt`: plain-text fallback;
- `order-confirmation.sample-data.json`: SendGrid preview/test data.

## Add it to SendGrid

1. In SendGrid, open **Email API > Dynamic Templates**.
2. Create a template and add a version using the **Code Editor**.
3. Paste `order-confirmation.html` into the HTML editor.
4. Use `Order confirmed - {{orderNumber}}` as the subject.
5. Paste `order-confirmation.txt` into the version's plain-text editor.
6. Paste `order-confirmation.sample-data.json` into **Test Data** and preview it.
7. Save the generated dynamic-template ID as a backend secret or runtime setting.

The unsubscribe links use SendGrid's `{{{unsubscribe}}}` and
`{{{unsubscribe_preferences}}}` substitution tags. They require the appropriate
SendGrid unsubscribe or suppression-group configuration.

## Dynamic data contract

| Field | Type | Notes |
|-------|------|-------|
| `preheader` | string | Inbox preview text |
| `customerName` | string | Recipient display name |
| `orderNumber` | string | Human-readable order reference |
| `orderDate` | string | Already localized/formatted |
| `orderStatus` | string | For example, `Confirmed` |
| `orderItems` | array | Product rows |
| `orderItems[].productName` | string | Product name |
| `orderItems[].variant` | string | Optional size/color/variant |
| `orderItems[].quantity` | number | Purchased quantity |
| `orderItems[].unitPrice` | string | Already currency-formatted |
| `orderItems[].lineTotal` | string | Already currency-formatted |
| `orderItems[].imageUrl` | string | Optional absolute HTTPS URL |
| `subtotal` | string | Already currency-formatted |
| `shipping` | string | Formatted amount or `Free` |
| `tax` | string | Already currency-formatted |
| `total` | string | Already currency-formatted |
| `shippingAddressLines` | array | One address line per item |
| `phoneNumber` | string | Recipient phone |
| `paymentMethod` | string | Masked description only |
| `deliveryEstimate` | string | Human-readable date/range |
| `orderUrl` | string | Absolute HTTPS order-details URL |
| `shopUrl` | string | Absolute HTTPS storefront URL |
| `supportUrl` | string | Absolute HTTPS support URL or a `mailto:` link |
| `supportEmail` | string | Customer-support mailbox |
| `currentYear` | string | Four-digit year |

Never include a Stripe secret, card number, CVC, payment-intent client secret, or
other sensitive payment data in dynamic-template data. A masked description
such as `Visa ending in 4242` is sufficient.

## Backend integration

`EmailService` uses this dynamic template when
`SendGrid:OrderConfirmationTemplateId` is configured. It supplies the
lower-camel-case data contract below and retains the older inline HTML/text
message only as a local configuration fallback.

```csharp
var message = new SendGridMessage();
message.SetFrom(from);
message.AddTo(to);
message.SetTemplateId(configuration["SendGrid:OrderConfirmationTemplateId"]);
message.SetTemplateData(new
{
    preheader,
    customerName,
    orderNumber,
    orderDate,
    orderStatus,
    orderItems,
    subtotal,
    shipping,
    tax,
    total,
    shippingAddressLines,
    phoneNumber,
    paymentMethod,
    deliveryEstimate,
    orderUrl,
    shopUrl,
    supportUrl,
    supportEmail,
    currentYear
});
```

For Docker, the project reads the ID and sender settings from the Git-ignored
root `.secrets` directory. For direct local .NET runs, use User Secrets. Keep
the SendGrid API key in User Secrets, Docker secrets, Azure Key Vault, or an
equivalent secret manager; never place it in this folder or an `appsettings`
file.
