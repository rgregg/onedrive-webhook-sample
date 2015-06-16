using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;

namespace CameraRollOrganizer.Models
{
    public class Account : TableEntity
    {
        public Account(string id)
        {
            this.PartitionKey = id;
            this.RowKey = id;
        }

        public Account()
        {

        }

        public string Id { get; set; }

        public string DisplayName { get; set; }

        public string RefreshToken { get; set; }

        public string SyncToken { get; set; }

        public string SubfolderFormat { get; set; }

    }

    
}