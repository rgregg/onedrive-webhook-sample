using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace CameraRollOrganizer.Controllers
{
    public class WebhookController : ApiController
    {
        [HttpPost, Route("api/webhook")]
        public async Task<IHttpActionResult> Post(OneDriveWebhook webhook)
        {
            if (null == webhook)
            {
                return BadRequest("Missing correctly formatted notification value");
            }

            //TrackIncomingWebhook(webhook);

            foreach (var notification in webhook.Value)
            {
                await AzureStorage.AddToPendingSubscriptionQueueAsync(notification);
            }
            return Ok();
        }

        [HttpGet, Route("api/queuedepth")]
        public async Task<IHttpActionResult> QueueDepth()
        {
            var queueDepth = await AzureStorage.GetPendingSubscriptionQueueDepthAsync();
            return Ok(new { depth  = queueDepth });
        }
    }
}