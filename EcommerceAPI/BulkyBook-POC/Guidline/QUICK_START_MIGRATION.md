# Quick Start: Secrets Migration

## 🚀 Fast Migration (5 minutes)

### Step 1: Run the Migration Script

Open PowerShell and navigate to the Ecommerce.API project directory:

```powershell
cd EcommerceAPI\BulkyBook-POC\Ecommerce.API
```

Run the migration script:

```powershell
..\scripts\migrate-secrets.ps1
```

This script will:
- ✅ Initialize User Secrets
- ✅ Extract secrets from your current `appsettings.json`
- ✅ Set them in User Secrets automatically

### Step 2: Verify Secrets Are Set

```powershell
dotnet user-secrets list
```

You should see all your secrets listed.

### Step 3: Test the Application

```powershell
dotnet run
```

The application should start normally. If you see errors about missing secrets, verify they were set correctly.

### Step 4: Done! ✅

Your secrets are now secure! The `appsettings.json` files have been updated to use placeholders.

## 📋 What Was Changed

### appsettings.json
- ✅ Secrets replaced with `"SET_IN_USER_SECRETS_OR_KEY_VAULT"`
- ✅ Added `KeyVault` configuration section

### appsettings.Development.json
- ✅ Secrets replaced with placeholders
- ✅ Ready for User Secrets

### appsettings.Production.json
- ✅ Secrets replaced with `"SET_IN_KEY_VAULT_OR_ENVIRONMENT_VARIABLES"`
- ✅ KeyVault section added (set `VaultName` when ready)

## 🔍 Verify Everything Works

Test these features:
- [ ] Application starts without errors
- [ ] User registration works
- [ ] User login generates JWT tokens
- [ ] Email sending works (if SendGrid is configured)
- [ ] OAuth login works (if OAuth is configured)

## ⚠️ Important Notes

1. **Secrets are now in User Secrets** - They're stored securely on your machine
2. **appsettings.json is safe to commit** - No real secrets in the files anymore
3. **For production** - Set up Azure Key Vault (see SECRETS_MANAGEMENT_GUIDE.md)

## 🆘 Troubleshooting

### "Secret not found" error
```powershell
# Check if secrets are set
dotnet user-secrets list

# If missing, set them manually
dotnet user-secrets set "Jwt:SecretKey" "Your-Secret-Key"
```

### Application won't start
- Verify all required secrets are set
- Check the error message - it will tell you which secret is missing
- See SECRETS_MANAGEMENT_GUIDE.md for detailed help

## 📚 Next Steps

- For production deployment, see `SECRETS_MANAGEMENT_GUIDE.md`
- For detailed migration steps, see `SECRETS_MIGRATION_CHECKLIST.md`

---

**Status:** ✅ Migration Complete - Secrets are now secure!

