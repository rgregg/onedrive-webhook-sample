using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CameraRollOrganizer.Models
{
    public static class AzureStorage
    {
        private static CloudStorageAccount StorageAccount { get; set; }

        private static CloudTable AccountTable { get; set; }

        private static CloudQueue SubscriptionQueue { get; set; }
        

        static AzureStorage()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
            AzureStorage.StorageAccount = CloudStorageAccount.Parse(connectionString);

            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            AccountTable = tableClient.GetTableReference("account");
            AccountTable.CreateIfNotExists();

            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();
            SubscriptionQueue = queueClient.GetQueueReference("subscriptions");
            SubscriptionQueue.CreateIfNotExists();
        }


        #region Account Table
        public static async Task InsertAccountAsync(Models.Account account)
        {
            TableOperation insertOperation = TableOperation.Insert(account);
            await AccountTable.ExecuteAsync(insertOperation);
        }

        public static async Task<Models.Account> LookupAccountAsync(string id)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<Account>(id, id);
            TableResult result = await AccountTable.ExecuteAsync(retrieveOperation);
            return (Account)result.Result;
        }

        public static async Task UpdateAccountAsync(Models.Account account)
        {
            TableOperation updateOperation = TableOperation.InsertOrReplace(account);
            await AccountTable.ExecuteAsync(updateOperation);
        }
        #endregion

        #region Subscription Queue

        public static async Task AddToPendingSubscriptionQueueAsync(OneDriveNotification notification)
        {
            CloudQueueMessage message = new CloudQueueMessage(notification.UserId);
            await SubscriptionQueue.AddMessageAsync(message);
        }

        public static async Task<OneDriveNotification> GetPendingSubscriptionAsync()
        {
            CloudQueueMessage message = await SubscriptionQueue.GetMessageAsync(TimeSpan.FromMinutes(5), null, null);

            OneDriveNotification notification = new OneDriveNotification { UserId = message.AsString };
            await SubscriptionQueue.DeleteMessageAsync(message);

            return notification;
        }

        public static async Task<int> GetPendingSubscriptionQueueDepthAsync()
        {
            await SubscriptionQueue.FetchAttributesAsync();
            int? cachedMessageCount = SubscriptionQueue.ApproximateMessageCount;

            if (cachedMessageCount.HasValue)
                return cachedMessageCount.Value;
            else
                return -1;
        }


        #endregion


    }
}