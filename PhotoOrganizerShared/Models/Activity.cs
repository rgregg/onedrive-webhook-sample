namespace PhotoOrganizerShared.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;


    public class Activity : TableEntity
    {
        internal const string DateTimeOffsetFormat = "u";

        public Activity()
        {
            this.RowKey = DateTimeOffset.UtcNow.ToString(DateTimeOffsetFormat);
            this.Type = ActivityEventCode.MessageLogged;
        }

        /// <summary>
        /// Unique identifier of the user who generated this activity (maps to PartitionKey)
        /// </summary>
        public string UserId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        public DateTimeOffset EventDateUtc
        {
            get
            {
                return DateTimeOffset.ParseExact(this.RowKey, DateTimeOffsetFormat, DateTimeFormatInfo.InvariantInfo);
            }
        }

        public string EventType { get; set; }

        public ActivityEventCode Type
        {
            set { this.EventType = value.ToString(); }
        }

        public string Message { get; set; }

        public long WorkItemCount { get; set; }

        public enum ActivityEventCode
        {
            MessageLogged,
            FileChanged,
            WebhookReceived,
            AccountProcessed,
            LookingForChanges
        }
    }
}