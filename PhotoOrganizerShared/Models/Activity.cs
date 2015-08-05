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
        internal const string DateTimeOffsetFormat = "o";

        public Activity()
        {
            this.RowKey = DateTime.UtcNow.ToString(DateTimeOffsetFormat);
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

        public DateTime EventDateUtc
        {
            get
            {
                try
                {
                    return DateTime.Parse(this.RowKey, null, DateTimeStyles.RoundtripKind);
                }
                catch
                {
                    return DateTime.MinValue;
                }
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