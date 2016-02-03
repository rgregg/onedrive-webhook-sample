using PhotoOrganizerWeb.Utility;
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

namespace PhotoOrganizerWeb.Controllers
{
    using PhotoOrganizerWeb.Utility;

    public class AuthorizationController : ApiController
    {

        [HttpGet, Route("redirect")]
        public async Task<IHttpActionResult> AuthRedirect(string code)
        {
            // Redeem authorization code for account information

            OAuthHelper helper = new OAuthHelper(SharedConfig.TokenService,
                SharedConfig.AppClientID,
                SharedConfig.AppClientSecret,
                SharedConfig.RedirectUri);

            var token = await helper.RedeemAuthorizationCodeAsync(code);
            if (null == token)
            {
                return JsonResponseEx.Create(HttpStatusCode.InternalServerError, new { message = "Invalid response from token service.", code = "tokenServiceNullResponse" });
            }

            Account account = new Account(token);
            var client = await SharedConfig.GetOneDriveClientForAccountAsync(account);
            var rootDrive = await client.Drive.Request().GetAsync();

            account.Id = rootDrive.Id;
            account.DisplayName = rootDrive.Owner.User.DisplayName;

            var existingAccount = await AzureStorage.LookupAccountAsync(rootDrive.Id);
            if (null == existingAccount)
            {
                await AzureStorage.InsertAccountAsync(account);
                existingAccount = account;
            }
            else
            {
                existingAccount.SetTokenResponse(token);
                await AzureStorage.UpdateAccountAsync(existingAccount);
            }

            var authCookie = CookieForAccount(existingAccount);

            return RedirectResponse.Create("/default.aspx", authCookie);
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
            nv["id"] = null != account ? account.Id.Encrypt(SharedConfig.CookieAuthPassword) : "";

            var cookie = new CookieHeaderValue("session", nv);
#if !DEBUG
            cookie.Secure = true;
#endif
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

            return await AccountFromCookie(sessionCookie.Values, true);
        }

        public static async Task<Account> AccountFromCookie(NameValueCollection storedCookieValue, bool shouldDecode)
        {
            string accountId = null;
            string encryptedAccountId = storedCookieValue["id"];
            if (shouldDecode && null != encryptedAccountId)
            {
                encryptedAccountId = HttpUtility.UrlDecode(encryptedAccountId);
            }

            if (null != encryptedAccountId)
            {
                accountId = encryptedAccountId.Decrypt(SharedConfig.CookieAuthPassword);
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
