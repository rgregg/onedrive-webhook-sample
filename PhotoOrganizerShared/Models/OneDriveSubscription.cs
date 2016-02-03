using Newtonsoft.Json;
using System;

namespace PhotoOrganizerShared.Models
{
    public class OneDriveSubscription
    {
        [JsonProperty("id")]
        public string Id
        {
            get; set;
        }

        [JsonProperty("notificationUrl")]
        public string NotificationUrl
        {
            get; set;
        }

        [JsonProperty("notificationType")]
        public string NotificationType
        {
            get; set;
        }

        [JsonProperty("expirationDateTime")]
        public DateTimeOffset ExpirationDateTime
        {
            get; set;
        }

        [JsonProperty("resource")]
        public string Resource
        {
            get; set;
        }

        [JsonProperty("context")]
        public string Context
        {
            get; set;
        }
    }
}
