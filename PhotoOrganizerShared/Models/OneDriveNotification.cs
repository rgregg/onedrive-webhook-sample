using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoOrganizerShared.Models
{
    public class OneDriveNotification
    {
        public string SubscriptionId { get; set; }

        public DateTimeOffset SubscriptionExpirationDateTime { get; set; }

        public string UserId { get; set; }

        public Dictionary<string, object> Entity { get; set; }
    }

    public class OneDriveWebhook
    {
        public List<OneDriveNotification> Value { get; set; }
    }
}