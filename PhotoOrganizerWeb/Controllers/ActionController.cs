using PhotoOrganizerWeb.Utility;
using Microsoft.OneDrive.Sdk;
using Newtonsoft.Json;
using PhotoOrganizerShared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PhotoOrganizerWeb.Controllers
{
    using PhotoOrganizerWeb.Utility;
    using PhotoOrganizerShared;
    using PhotoOrganizerShared.Models;

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
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue, false);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            var client = await SharedConfig.GetOneDriveClientForAccountAsync(account);
            var item = await client.Drive.Special[account.SourceFolder].ItemWithPath("test_file.txt").Content.Request().PutAsync<Item>(this.TestFileStream());

            await AzureStorage.InsertActivityAsync(
                new Activity
                {
                    UserId = account.Id,
                    Type = Activity.ActivityEventCode.FileChanged,
                    Message = string.Format("Creating test file test_file.txt with resource id: {0}", item.Id)
                });

            return JsonResponseEx.Create(HttpStatusCode.OK, item);
        }

        [HttpGet, Route("api/stats")]
        public async Task<IHttpActionResult> FetchStatisticsAsync()
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            if (cookies == null)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie is missing." });
            }
            var sessionCookieValue = cookies["session"].Values;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue, false);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            var client = await SharedConfig.GetOneDriveClientForAccountAsync(account);
            var cameraRollFolder = await client.Drive.Special["cameraroll"].Request().GetAsync();


            var responseObj = new
            {
                itemCount = cameraRollFolder.Folder.ChildCount,
                totalSize = cameraRollFolder.Size,
                lastModified = cameraRollFolder.LastModifiedDateTime
            };

            return JsonResponseEx.Create(HttpStatusCode.OK, responseObj);
            
        }


        internal static PhotoOrganizerShared.Models.OneDriveWebhook LastWebhookReceived { get; set; }

        [HttpGet, Route("api/action/lastwebhook")]
        public IHttpActionResult LastWebhook()
        {
            if (LastWebhookReceived != null)
            {
                return JsonResponseEx.Create(HttpStatusCode.OK, LastWebhookReceived);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet, Route("api/action/activity")]
        public async Task<IHttpActionResult> RecentActivity(string cid = null)
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            if (cookies == null)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie is missing." });
            }
            var sessionCookieValue = cookies["session"].Values;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue, false);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            var activity = await AzureStorage.RecentActivityAsync(cid ?? account.Id);
            return JsonResponseEx.Create(HttpStatusCode.OK, new { value = activity});
        }

        [HttpGet, Route("api/action/testhook")]
        public async Task<IHttpActionResult> CreateTestWebhook()
        {
            var cookies = Request.Headers.GetCookies("session").FirstOrDefault();
            if (cookies == null)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Session cookie is missing." });
            }
            var sessionCookieValue = cookies["session"].Values;
            var account = await AuthorizationController.AccountFromCookie(sessionCookieValue, false);
            if (null == account)
            {
                return JsonResponseEx.Create(HttpStatusCode.Unauthorized, new { message = "Failed to locate an account for the auth cookie." });
            }

            OneDriveNotification notification = new OneDriveNotification { UserId = account.Id };
            await AzureStorage.AddToPendingSubscriptionQueueAsync(notification);

            return Ok();
        }


        private Stream TestFileStream()
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("This is a dummy file that can be deleted");
            return new MemoryStream(bytes);            
        }



    }
}
