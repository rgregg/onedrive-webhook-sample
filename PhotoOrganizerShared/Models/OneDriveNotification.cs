using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoOrganizerShared.Models
{
    /// <summary>
    /// This class represents a single notification message from the OneDrive service.
    /// </summary>
    public class OneDriveNotification
    {
        public OneDriveNotification()
        {
            this.ReceivedDateTime = DateTimeOffset.UtcNow;
        }

        //[JsonProperty("subscriptionId")]
        //public string SubscriptionId { get; set; }

        //[JsonProperty("subscriptionExpirationDateTime")]
        //public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        //[JsonProperty("entity")]
        //public Dictionary<string, object> Entity { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonIgnore]
        public DateTimeOffset ReceivedDateTime { get; private set; }
    }

    /// <summary>
    /// This is the serialized format of the actual webhook, which includes
    /// a "value" array of notification messages.
    /// </summary>
    public class OneDriveWebhook
    {
        [JsonProperty("value")]
        public List<OneDriveNotification> Value { get; set; }
    }
}