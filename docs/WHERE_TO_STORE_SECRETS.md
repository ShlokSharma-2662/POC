# Where to Store Secrets - Quick Guide

## 🔍 Secrets That Need to Be Stored

These values in `appsettings.json` marked as `"SET_IN_USER_SECRETS_OR_KEY_VAULT"` need to be stored securely:

1. `Jwt:SecretKey` - JWT token signing key
2. `OAuth:ClientSecret` - Microsoft OAuth client secret
3. `GoogleOAuth:ClientSecret` - Google OAuth client secret (if you want to move it)
4. `SendGrid:ApiKey` - SendGrid email service API key
5. `Stripe:SecretKey` - Stripe payment gateway secret key
6. `Redis:Password` - Redis cache password
7. `RabbitMQ:Password` - RabbitMQ message queue password

---

## 🛠️ Option 1: User Secrets (For Development/Local)

**Best for:** Local development on your machine

### Step 1: Navigate to API Project
```bash
cd EcommerceAPI/BulkyBook-POC/Ecommerce.API
```

### Step 2: Initialize User Secrets (First Time Only)
```bash
dotnet user-secrets init
```

### Step 3: Add Each Secret

```bash
# JWT Secret Key (generate a strong key - minimum 32 characters)
dotnet user-secrets set "Jwt:SecretKey" "Your-Actual-JWT-Secret-Key-Here-Minimum-32-Characters"

# Microsoft OAuth Client Secret
dotnet user-secrets set "OAuth:ClientSecret" "8WE8Q~jbfdnNMsbVYHFUaOR39o1IN2KY.1yOWbbx"

# Google OAuth Client Secret (if you want to move it from appsettings.json)
dotnet user-secrets set "GoogleOAuth:ClientSecret" "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"

# SendGrid API Key
dotnet user-secrets set "SendGrid:ApiKey" "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"

# Stripe Secret Key
dotnet user-secrets set "Stripe:SecretKey" "sk_test_51RyrEsRq2tUM97cWQ6dHzU2hkz0tIm25lrZPrB4OMU0WTSaBn5GPYLzy0P08Eg1nsZep3bX7pTUZJNeCCuoCAbkV0073EeRhw0"

# Redis Password
dotnet user-secrets set "Redis:Password" "Rysun@123"

# RabbitMQ Password
dotnet user-secrets set "RabbitMQ:Password" "guest"
```

### Step 4: Verify Secrets Are Stored
```bash
dotnet user-secrets list
```

You should see all your secrets listed.

### Step 5: Test Your Application
Run your application - it will automatically read from User Secrets!

---

## ☁️ Option 2: Azure Key Vault (For Production)

**Best for:** Production environments, Azure deployments

### Step 1: Create Key Vault (if not exists)
```bash
az keyvault create --name YourKeyVaultName --resource-group YourResourceGroup --location eastus
```

### Step 2: Add Secrets to Key Vault

**Important:** Key Vault uses `--` (double dash) instead of `:` (colon) for nested keys.

```bash
# JWT Secret Key
az keyvault secret set --vault-name YourKeyVaultName --name "Jwt--SecretKey" --value "Your-JWT-Secret"

# Microsoft OAuth Client Secret
az keyvault secret set --vault-name YourKeyVaultName --name "OAuth--ClientSecret" --value "Your-OAuth-Secret"

# Google OAuth Client Secret
az keyvault secret set --vault-name YourKeyVaultName --name "GoogleOAuth--ClientSecret" --value "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"

# SendGrid API Key
az keyvault secret set --vault-name YourKeyVaultName --name "SendGrid--ApiKey" --value "Your-SendGrid-Key"

# Stripe Secret Key
az keyvault secret set --vault-name YourKeyVaultName --name "Stripe--SecretKey" --value "Your-Stripe-Key"

# Redis Password
az keyvault secret set --vault-name YourKeyVaultName --name "Redis--Password" --value "Your-Redis-Password"

# RabbitMQ Password
az keyvault secret set --vault-name YourKeyVaultName --name "RabbitMQ--Password" --value "Your-RabbitMQ-Password"
```

### Step 3: Enable Key Vault in Production Config

Update `appsettings.Production.json`:
```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultName": "YourKeyVaultName"
  }
}
```

### Step 4: Configure Access

For App Service with Managed Identity:
```bash
# Assign Managed Identity
az webapp identity assign --name YourAppName --resource-group YourResourceGroup

# Grant Key Vault access (use PrincipalId from previous command)
az keyvault set-policy --name YourKeyVaultName --object-id <PrincipalId> --secret-permissions get list
```

---

## 📍 Where User Secrets Are Stored

### Windows Location:
```
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

Example:
```
C:\Users\YourUsername\AppData\Roaming\Microsoft\UserSecrets\57eca317-f067-4c1b-bc97-a7e3a328568a\secrets.json
```

**Note:** This file is automatically excluded from Git via `.gitignore`

---

## 🔄 Configuration Priority Order

The application reads secrets in this order (first match wins):

1. **Azure Key Vault** (if enabled in production)
2. **Environment Variables** (if set)
3. **User Secrets** (development only)
4. **appsettings.json** (fallback with placeholder)

---

## ✅ Quick Setup Commands

### For Development - One-Time Setup:
```bash
cd EcommerceAPI/BulkyBook-POC/Ecommerce.API

# Initialize
dotnet user-secrets init

# Add all secrets at once (copy and paste all commands)
dotnet user-secrets set "Jwt:SecretKey" "Your-JWT-Secret"
dotnet user-secrets set "OAuth:ClientSecret" "Your-OAuth-Secret"
dotnet user-secrets set "GoogleOAuth:ClientSecret" "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"
dotnet user-secrets set "SendGrid:ApiKey" "Your-SendGrid-Key"
dotnet user-secrets set "Stripe:SecretKey" "Your-Stripe-Key"
dotnet user-secrets set "Redis:Password" "Your-Redis-Password"
dotnet user-secrets set "RabbitMQ:Password" "Your-RabbitMQ-Password"

# Verify
dotnet user-secrets list
```

---

## 🎯 Current Status

### What's Currently in appsettings.json:
- ✅ `GoogleOAuth:ClientSecret` = `"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"` (you kept this)
- ⚠️ Other secrets = `"SET_IN_USER_SECRETS_OR_KEY_VAULT"` (need to be set)

### What You Should Do:

**For Google OAuth ClientSecret:**
- **Option A:** Keep it in `appsettings.json` (works, but not recommended for production)
- **Option B:** Move to User Secrets (recommended for development)
- **Option C:** Move to Key Vault (required for production)

**For Other Secrets:**
- Set them in User Secrets (development) or Key Vault (production)

---

## 📝 Example: Setting Up Google OAuth Secret

### Move from appsettings.json to User Secrets:

```bash
cd EcommerceAPI/BulkyBook-POC/Ecommerce.API

# 1. Add to User Secrets
dotnet user-secrets set "GoogleOAuth:ClientSecret" "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"

# 2. Update appsettings.json to use placeholder
# Change: "ClientSecret": "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"
# To:     "ClientSecret": "SET_IN_USER_SECRETS_OR_KEY_VAULT"

# 3. Verify it works
dotnet user-secrets list
```

---

## 🔍 How to Check If Secrets Are Working

### Test User Secrets:
```bash
# List all secrets
dotnet user-secrets list

# Get specific secret
dotnet user-secrets get "GoogleOAuth:ClientSecret"
```

### Test Key Vault:
```bash
# List all secrets
az keyvault secret list --vault-name YourKeyVaultName

# Get specific secret
az keyvault secret show --vault-name YourKeyVaultName --name "GoogleOAuth--ClientSecret"
```

### Test in Application:
1. Run the application
2. Try to use the feature (e.g., Google Sign-In)
3. If secret is missing, you'll get a clear error message
4. If secret is found, feature will work normally

---

## 💡 Summary

**For Development:**
- ✅ Use **User Secrets** - Fast, easy, local only
- ✅ Run: `dotnet user-secrets set "Key:Value" "actual-secret"`
- ✅ Secrets stored in: `%APPDATA%\Microsoft\UserSecrets\...`

**For Production:**
- ✅ Use **Azure Key Vault** - Secure, encrypted, enterprise-grade
- ✅ Run: `az keyvault secret set --vault-name X --name "Key--Value" --value "secret"`
- ✅ Enable in: `appsettings.Production.json`

**Never:**
- ❌ Commit secrets to Git
- ❌ Share secrets in plain text
- ❌ Store secrets in appsettings.json for production

---

## 🆘 Troubleshooting

### "Secret not found" error:
1. Check secret name matches exactly (case-sensitive)
2. Verify User Secrets initialized: `dotnet user-secrets list`
3. Check Key Vault access permissions
4. Verify configuration key format (`:` for User Secrets, `--` for Key Vault)

### Application can't read secrets:
1. Restart the application after adding secrets
2. Check configuration provider order in `Program.cs`
3. Verify environment (development vs production)
4. Check application logs for configuration errors


