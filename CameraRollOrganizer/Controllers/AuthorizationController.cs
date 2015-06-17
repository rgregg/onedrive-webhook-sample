using CameraRollOrganizer.Utility;
using Microsoft.OneDrive.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
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
                return JsonResponseEx.Create(HttpStatusCode.InternalServerError, new { message = "Invalid response from token service.", code = "tokenServiceNullResponse" });
            }
            
            Models.Account account = new Models.Account(token);
            OneDriveClient client = new OneDriveClient(Config.OneDriveBaseUrl, account, new HttpProvider(new Serializer()));

            var rootDrive = await client.Drive.Request().GetAsync();

            account.Id = rootDrive.Id;
            account.DisplayName = rootDrive.Owner.User.DisplayName;
            
            await Models.AzureStorage.InsertAccountAsync(account);

            var authCookie = CookieForAccount(account);

            return JsonResponseEx.Create(HttpStatusCode.OK, new { message = "Account information saved." }, authCookie);
        }


        [HttpGet, Route("signout")]
        public HttpResponseMessage SignOut()
        {
            var message = new HttpResponseMessage(HttpStatusCode.Redirect);
            message.Headers.Add("Location", "/default.aspx");
            message.Headers.AddCookies(new CookieHeaderValue[] { CookieForAccount(null) });

            return message;
        }

        public static CookieHeaderValue CookieForAccount(Models.Account account)
        {
            var nv = new NameValueCollection();
            nv["id"] = null != account ? account.Id.Encrypt(Config.CookiePassword) : "";
            //nv["token"] = null != account ? account.AccessToken.Encrypt(Config.CookiePassword) : "";

            var cookie = new CookieHeaderValue("session", nv);
            //cookie.Secure = true;
            cookie.HttpOnly = true;
            cookie.Expires = null != account ? DateTimeOffset.Now.AddMinutes(120) : DateTimeOffset.Now;
            cookie.Path = "/";

            return cookie;
        }

        public static async Task<Models.Account> AccountFromCookie(HttpCookieCollection cookies)
        {
            var sessionCookie = cookies["session"];
            if (null == sessionCookie) 
                return null;


            var values = sessionCookie.Values;
            string accountId = null;
            string encryptedAccountId = HttpUtility.UrlDecode(values["id"]);
            if (null != encryptedAccountId)
            {
                accountId = encryptedAccountId.Decrypt(Config.CookiePassword);
            }

            if (null != accountId)
            {
                var account = await Models.AzureStorage.LookupAccountAsync(accountId);
                return account;
            }

            return null;
        }

    }
}
