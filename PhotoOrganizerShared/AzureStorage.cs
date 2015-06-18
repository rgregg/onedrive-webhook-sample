using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using PhotoOrganizerShared.Models;
using PhotoOrganizerShared.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PhotoOrganizerShared
{
    public static class AzureStorage
    {
        private static CloudStorageAccount StorageAccount { get; set; }

        private static CloudTable AccountTable { get; set; }

        private static CloudQueue SubscriptionQueue { get; set; }
        

        static AzureStorage()
        {
            InitializeConnections();
        }

        private static void InitializeConnections()
        {
            string connectionString = SharedConfig.Default.AzureStorageConnectionString;
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
            TableOperation insertOperation = TableOperation.InsertOrReplace(account);
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
            QueryStringBuilder qsb = new QueryStringBuilder { StartCharacter = null };
            qsb.Add("id", notification.UserId);

            CloudQueueMessage message = new CloudQueueMessage(qsb.ToString());
            await SubscriptionQueue.AddMessageAsync(message);
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