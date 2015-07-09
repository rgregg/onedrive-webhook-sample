using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoOrganizerShared.Models
{
    public class OneDriveNotification
    {
        [JsonProperty("subscriptionId")]
        public string SubscriptionId { get; set; }

        [JsonProperty("subscriptionExpirationDateTime")]
        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("entity")]
        public Dictionary<string, object> Entity { get; set; }
    }

    public class OneDriveWebhook
    {
        [JsonProperty("value")]
        public List<OneDriveNotification> Value { get; set; }
    }
}