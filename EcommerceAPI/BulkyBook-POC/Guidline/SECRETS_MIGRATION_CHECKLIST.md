# Secrets Migration Checklist

Use this checklist to migrate secrets from appsettings.json to secure storage.

## ✅ Pre-Migration Steps

- [ ] Backup current `appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json`
- [ ] Review all secrets that need to be migrated
- [ ] Generate new strong secrets for production (especially JWT SecretKey)
- [ ] Document current secret values in a secure location (password manager)

## 🔧 Development Environment Setup

- [ ] Navigate to `Ecommerce.API` project directory
- [ ] Run `dotnet user-secrets init` (or use the provided PowerShell script)
- [ ] Set all required secrets using `dotnet user-secrets set` commands
- [ ] Verify secrets are set: `dotnet user-secrets list`
- [ ] Remove secrets from `appsettings.Development.json`
- [ ] Test application locally to ensure it works with User Secrets

## 🚀 Production Environment Setup

### Option A: Azure Key Vault (Recommended)

- [ ] Create Azure Key Vault (if not exists)
- [ ] Add secrets to Key Vault with proper naming (use `--` separator)
  - [ ] `Jwt--SecretKey`
  - [ ] `OAuth--ClientSecret`
  - [ ] `SendGrid--ApiKey`
  - [ ] `Stripe--SecretKey`
  - [ ] `Redis--Password`
  - [ ] `RabbitMQ--Password`
- [ ] Configure Managed Identity for App Service
- [ ] Grant Key Vault access to Managed Identity
- [ ] Update `appsettings.Production.json` to enable Key Vault:
  ```json
  "KeyVault": {
    "Enabled": true,
    "VaultName": "YourKeyVaultName"
  }
  ```
- [ ] Remove secrets from `appsettings.Production.json`
- [ ] Test production deployment

### Option B: Environment Variables

- [ ] Set environment variables in App Service Configuration or Docker
- [ ] Use double underscore `__` for nested keys (e.g., `Jwt__SecretKey`)
- [ ] Remove secrets from `appsettings.Production.json`
- [ ] Test production deployment

## 📝 Post-Migration Steps

- [ ] Verify application starts successfully
- [ ] Test authentication (JWT tokens)
- [ ] Test email sending (SendGrid)
- [ ] Test OAuth login (if used)
- [ ] Test payment processing (Stripe)
- [ ] Test Redis connection (if enabled)
- [ ] Test RabbitMQ connection (if enabled)
- [ ] Update CI/CD pipelines to use Key Vault or environment variables
- [ ] Document secret locations for team
- [ ] Remove old secrets from version control history (if committed)
- [ ] Add `appsettings.json` to `.gitignore` if it contains any secrets
- [ ] Update team documentation

## 🔒 Security Verification

- [ ] Verify no secrets are in `appsettings.json` files
- [ ] Verify no secrets are committed to Git
- [ ] Verify Key Vault access is properly restricted
- [ ] Verify User Secrets are not shared or committed
- [ ] Set up secret rotation schedule
- [ ] Enable Key Vault logging for audit

## 📚 Files Modified

- [x] `Program.cs` - Added Key Vault support
- [x] `ServiceCollectionExtensions.cs` - Added secret validation
- [x] `JwtTokenGenerator.cs` - Already validates secrets
- [x] `OAuthService.cs` - Added secret validation
- [x] `EmailService.cs` - Added secret validation
- [x] `Ecommerce.API.csproj` - Added Azure Key Vault packages
- [x] Created `KeyVaultSecretManager.cs`
- [x] Created `appsettings.template.json`
- [x] Created `SECRETS_MANAGEMENT_GUIDE.md`
- [x] Created `setup-user-secrets.ps1` script

## 🆘 Troubleshooting

If you encounter issues:

1. **"Secret not found" error**
   - Check secret name matches exactly (case-sensitive)
   - Verify User Secrets: `dotnet user-secrets list`
   - Check Key Vault secret names use `--` separator

2. **"Access denied" to Key Vault**
   - Verify Managed Identity is assigned
   - Check Key Vault access policies
   - Verify network rules allow access

3. **Application won't start**
   - Check all required secrets are set
   - Verify configuration provider order
   - Review application logs for specific errors

## 📞 Support

Refer to `SECRETS_MANAGEMENT_GUIDE.md` for detailed instructions and troubleshooting.

