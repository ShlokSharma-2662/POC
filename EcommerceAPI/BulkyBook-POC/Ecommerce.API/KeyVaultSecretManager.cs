using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace Ecommerce.API
{
    /// <summary>
    /// Custom Key Vault Secret Manager that handles secret name mapping
    /// Key Vault secret names use '--' as separator, but configuration uses ':'
    /// </summary>
    public class KeyVaultSecretManager : Azure.Extensions.AspNetCore.Configuration.Secrets.KeyVaultSecretManager
    {
        public override string GetKey(KeyVaultSecret secret)
        {
            // Convert Key Vault secret names (using '--') to configuration keys (using ':')
            // Example: "Jwt--SecretKey" becomes "Jwt:SecretKey"
            return secret.Name.Replace("--", ":");
        }

        public override bool Load(SecretProperties secret)
        {
            // Load all secrets from Key Vault
            return true;
        }
    }
}

