using PhotoOrganizerShared.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace CameraRollOrganizer
{
    public class WebAppConfig : IConfig
    {
        public static PhotoOrganizerShared.Utility.IConfig Default
        {
            get
            {
                if (null == SharedConfig.Default)
                {
                    SharedConfig.Default = new WebAppConfig();
                }
                return SharedConfig.Default;
            }
        }

        public string MsaClientId
        {
            get { return ConfigurationManager.AppSettings["MsaClientId"]; }
        }

        public string MsaClientSecret
        {
            get { return ConfigurationManager.AppSettings["MsaClientSecret"]; }
        }

        public string MsaAuthorizationService
        {
            get { return ConfigurationManager.AppSettings["MsaAuthorizationService"]; }
        }

        public string MsaTokenService
        {
            get { return ConfigurationManager.AppSettings["MsaTokenService"]; }
        }

        public string MsaRedirectionTarget
        {
            get { return ConfigurationManager.AppSettings["MsaRedirectTarget"]; }
        }

        public string MsaClientScopes
        {
            get { return ConfigurationManager.AppSettings["MsaScopes"]; }
        }

        public string OneDriveBaseUrl
        {
            get { return "https://api.onedrive.com/v1.0"; }
        }

        public OAuthHelper MicrosoftAccountOAuth()
        {
            return new OAuthHelper(this.MsaTokenService, this.MsaClientId, this.MsaClientSecret, this.MsaRedirectionTarget);
        }

        public string CookiePassword
        {
            get { return ConfigurationManager.AppSettings["CookiePassword"]; }
        }

        public string AzureStorageConnectionString { get { return ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString; } }

        public bool UseViewChanges { get { return Convert.ToBoolean(ConfigurationManager.AppSettings["UseViewChanges"]); } }

    }
}