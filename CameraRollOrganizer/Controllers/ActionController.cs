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
        [HttpGet, Route("/api/action/createfile")]
        public async Task<IHttpActionResult> CreateFile()
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            var sessionCookieValue = cookies["session"].Value;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue);
            if (null == account)
                return  JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie missing or invalid." });

            OneDriveClient client = new OneDriveClient(Config.Default.OneDriveBaseUrl, account, new HttpProvider(new Serializer()));
            var item = await client.Drive.Root.ItemWithPath("test_file.txt").Content.Request().PutAsync<Item>(GetDummyFileStream());

            return JsonResponseEx.Create(HttpStatusCode.OK, item);
        }


        private Stream GetDummyFileStream()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("This is a dummy file that can be deleted");
            return new MemoryStream(bytes);            
        }

    }
}
