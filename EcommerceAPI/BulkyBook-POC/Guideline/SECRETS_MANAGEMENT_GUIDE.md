# Secrets Management Implementation Guide

## Overview
This guide helps you securely manage secrets by moving them from configuration files to secure storage.

## Step 1: For Development - Use User Secrets

### Setup User Secrets
1. Right-click on `Ecommerce.API` project → Manage User Secrets
2. Or run this command in the `Ecommerce.API` directory:
   ```bash
   dotnet user-secrets init
   ```

### Add Secrets to User Secrets
Run these commands in the `Ecommerce.API` directory:

```bash
# JWT Configuration
dotnet user-secrets set "Jwt:SecretKey" "Your-Actual-JWT-Secret-Key-Here-Minimum-32-Characters"

# OAuth Configuration
dotnet user-secrets set "OAuth:ClientSecret" "8WE8Q~jbfdnNMsbVYHFUaOR39o1IN2KY.1yOWbbx"

# SendGrid
dotnet user-secrets set "SendGrid:ApiKey" "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"

# Stripe
dotnet user-secrets set "Stripe:SecretKey" "sk_test_51RyrEsRq2tUM97cWQ6dHzU2hkz0tIm25lrZPrB4OMU0WTSaBn5GPYLzy0P08Eg1nsZep3bX7pTUZJNeCCuoCAbkV0073EeRhw0"

# Redis
dotnet user-secrets set "Redis:Password" "Rysun@123"

# RabbitMQ
dotnet user-secrets set "RabbitMQ:Password" "guest"

# Database Connection (if using SQL auth instead of Integrated Security)
# dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Your-Connection-String-Here"
```

### Verify Secrets
```bash
dotnet user-secrets list
```

## Step 2: Update appsettings.json

Remove all secret values from `appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json`. Keep only non-sensitive configuration.

## Step 3: For Production - Use Azure Key Vault

### Prerequisites
1. Azure subscription
2. Azure Key Vault created
3. Application registered in Azure AD (for Managed Identity)

### Setup Azure Key Vault

1. **Create Key Vault** (if not exists):
   ```bash
   az keyvault create --name YourKeyVaultName --resource-group YourResourceGroup --location eastus
   ```

2. **Add Secrets to Key Vault**:
   ```bash
   az keyvault secret set --vault-name YourKeyVaultName --name "Jwt--SecretKey" --value "Your-JWT-Secret"
   az keyvault secret set --vault-name YourKeyVaultName --name "OAuth--ClientSecret" --value "Your-OAuth-Secret"
   az keyvault secret set --vault-name YourKeyVaultName --name "SendGrid--ApiKey" --value "Your-SendGrid-Key"
   az keyvault secret set --vault-name YourKeyVaultName --name "Stripe--SecretKey" --value "Your-Stripe-Key"
   az keyvault secret set --vault-name YourKeyVaultName --name "Redis--Password" --value "Your-Redis-Password"
   az keyvault secret set --vault-name YourKeyVaultName --name "RabbitMQ--Password" --value "Your-RabbitMQ-Password"
   ```

3. **Configure Managed Identity** (for App Service):
   ```bash
   az webapp identity assign --name YourAppName --resource-group YourResourceGroup
   ```

4. **Grant Access to Key Vault**:
   ```bash
   az keyvault set-policy --name YourKeyVaultName --object-id <PrincipalId> --secret-permissions get list
   ```

### Update Program.cs to Use Key Vault

The code has been updated to support Key Vault. Set these environment variables or configuration:

```json
{
  "KeyVault": {
    "VaultName": "YourKeyVaultName",
    "Enabled": true
  }
}
```

## Step 4: Environment Variables (Alternative for Production)

For containerized deployments or App Service, you can use environment variables:

```bash
# Set in App Service Configuration or Docker environment
Jwt__SecretKey=Your-JWT-Secret
OAuth__ClientSecret=Your-OAuth-Secret
SendGrid__ApiKey=Your-SendGrid-Key
Stripe__SecretKey=Your-Stripe-Key
Redis__Password=Your-Redis-Password
RabbitMQ__Password=Your-RabbitMQ-Password
```

**Note:** Use double underscore `__` for nested configuration keys in environment variables.

## Step 5: Generate New Secrets

### Generate a Strong JWT Secret Key
```bash
# Using PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Using OpenSSL (if available)
openssl rand -base64 32
```

### Rotate Secrets
1. Generate new secret
2. Update in Key Vault/User Secrets
3. Restart application
4. Old tokens will expire naturally (based on token lifetime)

## Security Best Practices

1. ✅ **Never commit secrets to source control**
2. ✅ **Use different secrets for each environment**
3. ✅ **Rotate secrets regularly** (every 90 days recommended)
4. ✅ **Use Managed Identity** for Azure resources
5. ✅ **Limit access** to Key Vault using RBAC
6. ✅ **Enable Key Vault logging** for audit trails
7. ✅ **Use Key Vault soft delete** for recovery

## Migration Checklist

- [ ] Backup current appsettings files
- [ ] Set up User Secrets for development
- [ ] Remove secrets from appsettings.json files
- [ ] Test application locally with User Secrets
- [ ] Set up Azure Key Vault for production
- [ ] Configure Managed Identity
- [ ] Migrate secrets to Key Vault
- [ ] Update production configuration
- [ ] Test production deployment
- [ ] Update CI/CD pipelines to use Key Vault
- [ ] Document secret locations for team

## Troubleshooting

### "Secret not found" error
- Verify secret name matches exactly (case-sensitive)
- Check User Secrets are initialized: `dotnet user-secrets list`
- Verify Key Vault access permissions

### "Access denied" to Key Vault
- Check Managed Identity is assigned
- Verify Key Vault access policy
- Check network rules if Key Vault has firewall enabled

### Application can't read secrets
- Verify configuration provider order in Program.cs
- Check environment variables are set correctly
- Review application logs for configuration errors

