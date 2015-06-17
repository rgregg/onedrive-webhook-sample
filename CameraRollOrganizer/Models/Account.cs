using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using CameraRollOrganizer.Utility;

namespace CameraRollOrganizer.Models
{
    public class Account : TableEntity, Microsoft.OneDrive.Sdk.IAuthenticator
    {
        private const string DefaultSubfolderFormatString = "yyyy/MM - MMMM";

        public Account()
        {

        }

        public Account(OAuthToken token)
        {
            this.RefreshToken = token.RefreshToken;
            this.CachedAccessToken = token.AccessToken;
            this.CachedAccessTokenExpiration = DateTimeOffset.Now.AddSeconds(token.AccessTokenExpirationDuration);

            this.SubfolderFormat = DefaultSubfolderFormatString;
        }

        public string Id 
        {
            get { return this.RowKey; }
            set
            {
                this.RowKey = value;
                this.PartitionKey = value;
            }
        }

        public string DisplayName { get; set; }

        public string RefreshToken { get; set; }

        public string SyncToken { get; set; }

        public string SubfolderFormat { get; set; }

        
        private string CachedAccessToken { get; set; }

        private DateTimeOffset? CachedAccessTokenExpiration { get; set; }

        public async Task<string> Authenticate()
        {
            if (null != CachedAccessTokenExpiration && null != CachedAccessToken 
                && CachedAccessTokenExpiration < DateTimeOffset.Now.AddMinutes(5))
            {
                return CachedAccessToken;
            }

            if (null == this.RefreshToken)
            {
                throw new InvalidOperationException("No refresh token available. Cannot authenticate.");
            }

            OAuthHelper oauth = Config.MicrosoftAccountOAuth();
            var token = await oauth.RedeemRefreshTokenAsync(this.RefreshToken);
            if (null != token)
            {
                CachedAccessToken = token.AccessToken;
                CachedAccessTokenExpiration = DateTimeOffset.Now.AddSeconds(token.AccessTokenExpirationDuration);

                if (null != RefreshToken)
                {
                    RefreshToken = token.RefreshToken;
                }

                return token.AccessToken;
            }

            throw new InvalidOperationException("Unable to authenticate. Need to sign-in again.");
        }
    }

    
}