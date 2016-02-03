using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using PhotoOrganizerShared.Models;

namespace PhotoOrganizerShared.Utility
{
    public static class SharedConfig
    {
        public static string AppBaseUrl { get { return "https://camerarollorganizer.azurewebsites.net"; } }

        public static string AppClientID
        {
            get { return "0000000048159D5E"; }
        }

        public static string AppClientSecret
        {
            get { return "Z4srwsF3EREvD4eJOSSwPKAMMMZPR6dg"; }
        }

        public static string AuthorizationService
        {
            get { return "https://login.live.com/oauth20_authorize.srf"; }
        }

        public static string TokenService
        {
            get { return "https://login.live.com/oauth20_token.srf"; }
        }

        public static string[] Scopes
        {
            get { return new string[] { "wl.offline_access", "onedrive.readwrite" }; }
        }

        public static string RedirectUri
        {
            get { return "https://camerarollorganizer.azurewebsites.net/redirect"; }
        }

        public static string CookieAuthPassword
        {
            get { return "rYh5epgaW6szK80Ujb1cEU6hFsPjS4e7"; }
        }

        public static string AzureStorageConnectionString
        {
            get { return "DefaultEndpointsProtocol=https;AccountName=camerarollorganizer;AccountKey=f8bDQYI2afpj0DDdPrlH7XBGc2PScOnFKVREoyq4wT9/mBx+i6M3A/5khKtBQQrP8olFg4sC0OyiM2EMCwjpdg=="; }
        }

        public static bool UseViewChanges
        {
            get { return true; }
        }

        public static string OAuthScopes()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var scope in Scopes)
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.Append(scope);
            }
            return sb.ToString();
        }

        public static OAuthHelper MicrosoftAccountOAuth()
        {
            return new OAuthHelper(TokenService, AppClientID, AppClientSecret, RedirectUri);
        }

        public static async Task<IOneDriveClient> GetOneDriveClientForAccountAsync(Account account)
        {
            try
            {
                return await OneDriveClient.GetSilentlyAuthenticatedMicrosoftAccountClient(AppClientID, RedirectUri, Scopes, AppClientSecret, account.RefreshToken);
            }
            catch (OneDriveException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            return null;
        }


        private class ODAuthProvider : IAuthenticationProvider
        {
            private readonly Account sourceAccount;

            public ODAuthProvider(Account account)
            {
                this.sourceAccount = account;
                this.CurrentAccountSession = new AccountSession();
            }

            public AccountSession CurrentAccountSession { get; set; }

            public async Task AppendAuthHeaderAsync(System.Net.Http.HttpRequestMessage request)
            {
                string accessToken = await sourceAccount.AuthenticateAsync();
                request.Headers.Add("Authorization", "Bearer " + accessToken);
            }

            public async Task<AccountSession> AuthenticateAsync()
            {
                return this.CurrentAccountSession;
            }

            public Task SignOutAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
