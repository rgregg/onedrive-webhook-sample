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

        private static CloudTable ActivityTable { get; set; }


        static AzureStorage()
        {
        }

        public static void InitializeConnections()
        {
            string connectionString = SharedConfig.AzureStorageConnectionString;
            AzureStorage.StorageAccount = CloudStorageAccount.Parse(connectionString);

            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
            AccountTable = tableClient.GetTableReference("account");
            AccountTable.CreateIfNotExists();

            ActivityTable = tableClient.GetTableReference("activity");
            ActivityTable.CreateIfNotExists();

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

        #region Activity Table

        public static async Task InsertActivityAsync(Activity activity)
        {
            try
            {
                TableOperation insertOperation = TableOperation.Insert(activity);
                await ActivityTable.ExecuteAsync(insertOperation);
            }
            catch
            {
                // Ignore errors here, since we're just logging activity.
            }
        }

        public static async Task<List<Activity>> RecentActivityAsync(string userId)
        {
            string TweleveHoursAgo = DateTime.UtcNow.Subtract(new TimeSpan(12, 0, 0)).ToString(Activity.DateTimeOffsetFormat);

            TableQuery<Activity> rangeQuery = new TableQuery<Activity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, TweleveHoursAgo)));

            var responses = ActivityTable.ExecuteQuery(rangeQuery);
            return responses.ToList();
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