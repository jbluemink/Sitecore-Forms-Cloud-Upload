﻿using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

namespace Stockpick.Form.Cloud.Crypto
{
    public class Keyvault
    {
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

    }
}
