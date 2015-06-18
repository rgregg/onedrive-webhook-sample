using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace PhotoOrganizerShared.Utility
{
    public interface IConfig
    {
        string MsaClientId
        {
            get;
        }

        string MsaClientSecret
        {
            get;
        }

        string MsaAuthorizationService
        {
            get;
        }

        string MsaTokenService
        {
            get;
        }

        string MsaRedirectionTarget
        {
            get;
        }

        string MsaClientScopes
        {
            get;
        }

        string OneDriveBaseUrl
        {
            get;
        }

        OAuthHelper MicrosoftAccountOAuth();

        string CookiePassword
        {
            get;
        }

        string AzureStorageConnectionString { get; }

    }

    public static class SharedConfig
    {
        public static IConfig Default { get; set; }
    }
}