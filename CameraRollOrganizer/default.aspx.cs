using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CameraRollOrganizer
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var clientId = ConfigurationManager.AppSettings["MsaClientId"];

            Utility.QueryStringBuilder qsb = new Utility.QueryStringBuilder();
            qsb.Add("client_id", clientId);
            qsb.Add("scope", "wl.offline_access onedrive.readwrite");
            qsb.Add("response_type", "code");
            qsb.Add("redirect_uri", "");

            string relativeUrl = Page.ResolveUrl("~/redirect.ashx");
            var redirectUri = new Uri(HttpContext.Current.Request.Url, relativeUrl);
            qsb.Add("redirect_uri", redirectUri.AbsoluteUri);
            qsb.StartCharacter = '?';

            linkToSignIn.NavigateUrl = "https://login.live.com/oauth20_authorize.srf" + qsb.ToString();
        }
    }
}