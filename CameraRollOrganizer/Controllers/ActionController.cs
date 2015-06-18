using CameraRollOrganizer.Utility;
using Microsoft.OneDrive.Sdk;
using PhotoOrganizerShared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace CameraRollOrganizer.Controllers
{
    public class ActionController : ApiController
    {
        [HttpGet, Route("api/action/createfile")]
        public async Task<IHttpActionResult> CreateFile()
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            if (cookies == null)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie is missing." });
            }
            var sessionCookieValue = cookies["session"].Values;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            OneDriveClient client = new OneDriveClient(Config.Default.OneDriveBaseUrl, account, new HttpProvider(new Serializer()));
            var item = await client.Drive.Special[account.SourceFolder].ItemWithPath("test_file.txt").Content.Request().PutAsync<Item>(GetDummyFileStream());
            
            return JsonResponseEx.Create(HttpStatusCode.OK, item);
        }

        [HttpGet, Route("api/action/subscriptions")]
        public async Task<IHttpActionResult> Subscriptions()
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            if (cookies == null)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie is missing." });
            }
            var sessionCookieValue = cookies["session"].Values;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            var accessToken = await account.Authenticate();
            if (null == accessToken)
            {
                return JsonResponseEx.Create(HttpStatusCode.InternalServerError, new { message = "Error getting access_token" });
            }

            var request = HttpWebRequest.CreateHttp("https://storage.live.com/MyData/LiveFolders/?NotificationSubscriptions");
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var response = await request.GetResponseAsync();
            var stream = response.GetResponseStream();

            return ContentResponseEx.Create(HttpStatusCode.OK, stream, "text/xml");
        }


        private Stream GetDummyFileStream()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("This is a dummy file that can be deleted");
            return new MemoryStream(bytes);            
        }

    }
}
