using CameraRollOrganizer.Utility;
using PhotoOrganizerShared;
using PhotoOrganizerShared.Utility;
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
        private PhotoOrganizerShared.Models.Account account = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(InitPageAsync));
        }

        private async Task InitPageAsync()
        {
            account = await Controllers.AuthorizationController.AccountFromCookie(Request.Cookies);
            if (!Page.IsPostBack)
            {
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
                    labelPhotosOrganizedCount.Text = account.PhotosOrganized.ToString();
                    labelWebhooksReceived.Text = account.WebhooksReceived.ToString();
                    textBoxFolderFormatString.Text = account.SubfolderFormat;
                    checkBoxEnableAccount.Checked = account.Enabled;
                }
            }
        }

        private void GenerateSignInLink()
        {
            QueryStringBuilder qsb = new QueryStringBuilder();
            qsb.Add("client_id", WebAppConfig.Default.MsaClientId);
            qsb.Add("scope", WebAppConfig.Default.MsaClientScopes);
            qsb.Add("response_type", "code");
            qsb.Add("redirect_uri", WebAppConfig.Default.MsaRedirectionTarget);
            linkToSignIn.NavigateUrl = WebAppConfig.Default.MsaAuthorizationService + qsb.ToString();
        }

        protected async void buttonSaveChanges_Click(object sender, EventArgs e)
        {
            var updateAccount = await Controllers.AuthorizationController.AccountFromCookie(Request.Cookies);
            if (null != updateAccount)
            {
                bool validFormatString;
                try
                {
                    string.Format(textBoxFolderFormatString.Text, DateTimeOffset.Now);
                    labelErrors.Text = "";
                    validFormatString = true;
                }
                catch (Exception ex) 
                {
                    labelErrors.Text = ex.Message;
                    validFormatString = false; 
                }

                if (validFormatString)
                    updateAccount.SubfolderFormat = textBoxFolderFormatString.Text;

                updateAccount.Enabled = checkBoxEnableAccount.Checked;
                await AzureStorage.UpdateAccountAsync(updateAccount);
            }
        }
      
    }
}