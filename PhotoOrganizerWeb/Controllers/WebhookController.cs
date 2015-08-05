using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace PhotoOrganizerWeb.Controllers
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

            ActionController.LastWebhookReceived = webhook;

            foreach (var notification in webhook.Value)
            {
                try
                {
                    // Record the activity of receiving the webhook
                    await AzureStorage.InsertActivityAsync(
                            new Activity
                            {
                                UserId = notification.UserId,
                                Type = Activity.ActivityEventCode.WebhookReceived,
                                Message = Newtonsoft.Json.JsonConvert.SerializeObject(notification)
                            });

                    // Enqueue an action to process this account again
                    await AzureStorage.AddToPendingSubscriptionQueueAsync(notification);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Exception adding the webhook to storage queue: " + ex.Message);
                }
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