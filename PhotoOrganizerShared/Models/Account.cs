using Microsoft.WindowsAzure.Storage.Table;
using PhotoOrganizerShared.Utility;
using System;
using System.Threading.Tasks;

namespace PhotoOrganizerShared.Models
{
    public class Account : TableEntity
    {
        private const string DefaultSubfolderFormatString = "Years/{0:yyyy/MM - MMMM}";
        private const string DefaultSpecialFolderIdentifier = "cameraroll";

        public Account()
        {
            SetDefaultPropertyValues();
        }

        public Account(OAuthToken token)
        {
            SetDefaultPropertyValues();

            SetTokenResponse(token);
        }

        private void SetDefaultPropertyValues()
        {
            this.Enabled = false;
            this.SubfolderFormat = DefaultSubfolderFormatString;
            this.SourceFolder = DefaultSpecialFolderIdentifier;
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

        public long PhotosOrganized { get; set; }

        public long WebhooksReceived { get; set; }

        public bool Enabled { get; set; }

        public string SourceFolder { get; set; }

        private string CachedAccessToken { get; set; }

        private DateTimeOffset? CachedAccessTokenExpiration { get; set; }

        public async Task<string> AuthenticateAsync()
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

            OAuthHelper oauth = SharedConfig.MicrosoftAccountOAuth();
            var token = await oauth.RedeemRefreshTokenAsync(this.RefreshToken);
            if (null != token)
            {
                SetTokenResponse(token);
                return token.AccessToken;
            }

            throw new InvalidOperationException("Unable to authenticate. Need to sign-in again.");
        }

        string AccessToken
        {
            get { return CachedAccessToken; }
            set { CachedAccessToken = value; } 
        }

        public string SubscriptionIdentifier { get; set; }

        public void SetTokenResponse(OAuthToken token)
        {
            CachedAccessToken = token.AccessToken;
            CachedAccessTokenExpiration = DateTimeOffset.Now.AddSeconds(token.AccessTokenExpirationDuration);

            if (null != token.RefreshToken)
            {
                RefreshToken = token.RefreshToken;
            }
        }
    }

    
}