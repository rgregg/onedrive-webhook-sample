using CameraRollOrganizer.Utility;
using Microsoft.OneDrive.Sdk;
using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using PhotoOrganizerShared.Utility;
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

            OAuthHelper helper = new OAuthHelper(Config.Default.MsaTokenService,
                Config.Default.MsaClientId,
                Config.Default.MsaClientSecret,
                Config.Default.MsaRedirectionTarget);

            var token = await helper.RedeemAuthorizationCodeAsync(code);
            if (null == token)
            {
                return JsonResponseEx.Create(HttpStatusCode.InternalServerError, new { message = "Invalid response from token service.", code = "tokenServiceNullResponse" });
            }
            
            Account account = new Account(token);
            OneDriveClient client = new OneDriveClient(Config.Default.OneDriveBaseUrl, account, new HttpProvider(new Serializer()));

            var rootDrive = await client.Drive.Request().GetAsync();

            account.Id = rootDrive.Id;
            account.DisplayName = rootDrive.Owner.User.DisplayName;
            
            await AzureStorage.InsertAccountAsync(account);

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

        public static CookieHeaderValue CookieForAccount(Account account)
        {
            var nv = new NameValueCollection();
            nv["id"] = null != account ? account.Id.Encrypt(Config.Default.CookiePassword) : "";
            //nv["token"] = null != account ? account.AccessToken.Encrypt(Config.CookiePassword) : "";

            var cookie = new CookieHeaderValue("session", nv);
            //cookie.Secure = true;
            cookie.HttpOnly = true;
            cookie.Expires = null != account ? DateTimeOffset.Now.AddMinutes(120) : DateTimeOffset.Now;
            cookie.Path = "/";

            return cookie;
        }

        public static async Task<Account> AccountFromCookie(HttpCookieCollection cookies)
        {
            var sessionCookie = cookies["session"];
            if (null == sessionCookie) 
                return null;

            return await AccountFromCookie(sessionCookie.Values);
        }

        public static async Task<Account> AccountFromCookie(NameValueCollection storedCookieValue)
        {
            string accountId = null;
            string encryptedAccountId = HttpUtility.UrlDecode(storedCookieValue["id"]);
            if (null != encryptedAccountId)
            {
                accountId = encryptedAccountId.Decrypt(Config.Default.CookiePassword);
            }

            if (null != accountId)
            {
                var account = await AzureStorage.LookupAccountAsync(accountId);
                return account;
            }

            return null;
        }

    }
}
