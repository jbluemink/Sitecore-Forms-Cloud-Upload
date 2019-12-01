using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Stockpick.Form.Cloud.Crypto
{
    public class Keyvault
    {
        public const int SALT_SIZE = 24; // size in bytes
        public const int HASH_SIZE = 24; // size in bytes
        public const int ITERATIONS = 100100; // number of pbkdf2 iterations

        public async static Task<string> GetToken(string authority, string resource, string scope)
        {
            var username = Sitecore.Configuration.Settings.GetSetting("Stockpick.Forms.KeyFault.ClientId");
            var pass = Sitecore.Configuration.Settings.GetSetting("Stockpick.Forms.KeyFault.ClientSecret");
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(username, pass);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException(
                    "Failed to obtain the JWT token check: Stockpick.Forms.KeyFault.ClientId and Stockpick.Forms.KeyFault.ClientSecret");

            return result.AccessToken;
        }

        public static byte[] CreateHash(byte[] salt, string input)
        {
            // Generate the hash
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS);
            return pbkdf2.GetBytes(HASH_SIZE);
        }

        public static byte[] CreateSalt()
        {
            // Generate a salt
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            byte[] salt = new byte[SALT_SIZE];
            return salt;
        }

    }
}
