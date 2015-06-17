using CameraRollOrganizer.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CameraRollOrganizer
{
    public partial class _default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            RegisterAsyncTask(new PageAsyncTask(InitPageAsync));
            
            
           
        }

        private async Task InitPageAsync()
        {
            var account = await Controllers.AuthorizationController.AccountFromCookie(Request.Cookies);
            if (null == account)
            {
                GenerateSignInLink();
                panelLoggedIn.Visible = false;
                panelNoCurrentUser.Visible = true;
            }
            else
            {
                panelLoggedIn.Visible = true;
                panelNoCurrentUser.Visible = false;

                labelAccountDisplayName.Text = account.DisplayName;
                labelAccountId.Text = account.Id;
                labelPhotosOrganizedCount.Text = account.PhotosOrganized.ToString();
                labelWebhooksReceived.Text = account.WebhooksReceived.ToString();
            }

        }

        private void GenerateSignInLink()
        {
            Utility.QueryStringBuilder qsb = new Utility.QueryStringBuilder();
            qsb.Add("client_id", Utility.Config.MsaClientId);
            qsb.Add("scope", Utility.Config.MsaClientScopes);
            qsb.Add("response_type", "code");
            qsb.Add("redirect_uri", Utility.Config.MsaRedirectionTarget);
            linkToSignIn.NavigateUrl = Utility.Config.MsaAuthorizationService + qsb.ToString();
        }
      
    }
}