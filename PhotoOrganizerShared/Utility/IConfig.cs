﻿using System;
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

        bool UseViewChanges { get; }

        string AzureStorageConnectionString { get; }

    }
}