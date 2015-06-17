using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace CameraRollOrganizer.Utility
{
    public static class Config
    {

        public static string MsaClientId
        {
            get { return ConfigurationManager.AppSettings["MsaClientId"]; }
        }

        public static string MsaClientSecret
        {
            get { return ConfigurationManager.AppSettings["MsaClientSecret"]; }
        }

        public static string MsaAuthorizationService
        {
            get { return ConfigurationManager.AppSettings["MsaAuthorizationService"]; }
        }

        public static string MsaTokenService
        {
            get { return ConfigurationManager.AppSettings["MsaTokenService"]; }
        }

        public static string MsaRedirectionTarget
        {
            get { return ConfigurationManager.AppSettings["MsaRedirectTarget"]; }
        }

        public static string MsaClientScopes
        {
            get { return ConfigurationManager.AppSettings["MsaScopes"]; }
        }

        public static string OneDriveBaseUrl
        {
            get { return "https://api.onedrive.com/v1.0"; }
        }

        public static OAuthHelper MicrosoftAccountOAuth()
        {
            return new OAuthHelper(Config.MsaTokenService, Config.MsaClientId, Config.MsaClientSecret, Config.MsaRedirectionTarget);
        }

        public static string CookiePassword
        {
            get { return ConfigurationManager.AppSettings["CookiePassword"]; }
        }

    }
}