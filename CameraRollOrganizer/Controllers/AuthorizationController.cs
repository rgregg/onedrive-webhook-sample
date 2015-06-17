using CameraRollOrganizer.Utility;
using Microsoft.OneDrive.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace CameraRollOrganizer.Controllers
{
    public class AuthorizationController : ApiController
    {

        [HttpGet, Route("redirect")]
        public async Task<IHttpActionResult> AuthRedirect(string code)
        {
            // Redeem authorization code for account information

            Utility.OAuthHelper helper = new Utility.OAuthHelper(Utility.Config.MsaTokenService, 
                Utility.Config.MsaClientId, Utility.Config.MsaClientSecret, Utility.Config.MsaRedirectionTarget);

            var token = await helper.RedeemAuthorizationCodeAsync(code);
            if (null == token)
            {
                return this.JsonResponse(HttpStatusCode.InternalServerError, new { message = "Invalid response from token service.", code = "tokenServiceNullResponse" });
            }
            
            Models.Account account = new Models.Account(token);
            OneDriveClient client = new OneDriveClient(Config.OneDriveBaseUrl, account, new HttpProvider(new Serializer()));

            var rootDrive = await client.Drive.Request().GetAsync();

            account.Id = rootDrive.Id;
            account.DisplayName = rootDrive.Owner.User.DisplayName;
            
            await Models.AzureStorage.InsertAccountAsync(account);

            return this.JsonResponse(HttpStatusCode.OK, new { message = "Account information saved." });
        }

    }
}
