namespace PhotoOrganizerWebJob
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Azure.WebJobs;
    using Microsoft.OneDrive.Sdk;
    using PhotoOrganizerShared;
    using PhotoOrganizerShared.Models;
    using PhotoOrganizerShared.Utility;

    public class Functions
    {
        private static KeyedLock AccountLocker = new KeyedLock();

        /// <summary>
        /// This method is automatically exuected by the Azure SDK whenever a new item is added to the queue named
        /// "subscriptions" -- it parses the queue message, finds the associated account, and then kicks off
        /// the webhook processing job.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task ProcessQueueMessageAsync([QueueTrigger("subscriptions")] string message, TextWriter log)
        {
            #region Logging
            WebJobLogger logger = new WebJobLogger(log);
            logger.WriteLog("Received queue message: {0}", message);
            #endregion

            // Decode the queue message, which is encoded in query string format
            var elements = HttpUtility.ParseQueryString(message);
            string userId = elements["id"];
            #region Error Checking
            if (string.IsNullOrEmpty(userId))
            {
                logger.WriteLog("User ID was null or empty. Message ignored.");
                return;
            }
            #endregion

            // Find a matching account in our known accounts table
            var account = await AzureStorage.LookupAccountAsync(userId);
            logger.Account = account;

            #region Error Checking
            if (null == account)
            {
                logger.WriteLog("Unable to locate account for id: {0}. Message skipped", userId);
                return;
            }
            logger.WriteLog("Processing changes for user id: {0}", userId);
            #endregion

            try
            {
                await ProcessChangesInOneDriveAsync(account, logger);
            }
            catch (Exception ex)
            {
                logger.WriteLog("Error while running job for user id '{0}'. Message skipped.\r\n{1}", userId, ex);
            }
        }

        /// <summary>
        /// Process the changes for an account's OneDrive and organize any new files returned.
        /// </summary>
        /// <returns></returns>
        public static async Task ProcessChangesInOneDriveAsync(Account account, WebJobLogger log)
        {
            #region Acquire lock for account or abort processing
            // Acquire a simple lock to ensure that only one thread is processing 
            // an account at the same time to avoid concurrency issues.
            // NOTE: If the web job is running on multiple VMs, this will not be sufficent to
            // ensure errors don't occur from one account being processed multiple times.
            bool acquiredLock = AccountLocker.TryAcquireLock(account.Id);
            if (!acquiredLock || !account.Enabled)
            {
                log.WriteLog("Failed to acquire lock for account. Another thread is already processing updates for this account or the account is disabled.");
                return;
            }
            log.WriteLog("Account lock acquired. Connecting to OneDrive.");
            #endregion

            try
            {
                // Build a new OneDriveClient with the account information
                var client = await SharedConfig.GetOneDriveClientForAccountAsync(account);

                // Execute our organization code
                FolderOrganizer organizer = new FolderOrganizer(client, account, log);
                await organizer.OrganizeSourceFolderItemChangesAsync();

                // Record that we received another webhook and save the account back to table storage
                account.WebhooksReceived += 1;
                await AzureStorage.UpdateAccountAsync(account);
                log.WriteLog("Updated account {0} with hooks received: {1}", account.Id, account.WebhooksReceived);
            }
            #region Error Handling
            catch (Exception ex)
            {
                log.WriteLog("Exception: {0}", ex);
            }
            finally
            {
                AccountLocker.ReleaseLock(account.Id);
            }

            log.WriteLog("Processing completed for account: {0}", account.Id);
            #endregion
        }

        #region Subscription management
        /// <summary>
        /// Ensure that we have a valid subscription to receive webhooks for the target folder 
        /// on this account.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task SubscribeToWebhooksForAccount(Account account, WebJobLogger log)
        {
            try
            {
                log.WriteLog(ActivityEventCode.CreatingSubscription, "Creating subscription on OneDrive service.");

                log.WriteLog("Connecting to OneDrive...");

                // Build a new OneDriveClient with the account information
                var client = await SharedConfig.GetOneDriveClientForAccountAsync(account);

                await CreateNewSubscriptionAsync(account, client, log);

                account.WebhooksReceived += 1;
                await AzureStorage.UpdateAccountAsync(account);
                log.WriteLog("Updated account {0} with hooks received: {1}", account.Id, account.WebhooksReceived);
            }
            catch (Exception ex)
            {
                log.WriteLog("Exception: {0}", ex);
            }
            finally
            {
                AccountLocker.ReleaseLock(account.Id);
            }
        }

        /// <summary>
        /// Create a new notification subscription with the service.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static async Task CreateNewSubscriptionAsync(Account account, IOneDriveClient client, WebJobLogger log)
        {
            log.WriteLog("Creating subscription for account");

            // Make a request to create a new subscription
            OneDriveSubscription postSub = CreateSubscription(account);
            try
            {
                var result = await client.SendRequestAsync<OneDriveSubscription>("POST", "/special/cameraroll/subscriptions", postSub);
                account.SubscriptionIdentifier = result.Id;
                log.WriteLog("Subscription created. ID: {0}. Expiration: {1}", result.Id, result.ExpirationDateTime);
            }
            catch (Exception ex)
            {
                log.WriteLog("Error creating subscription: {0}", ex);
                // TODO: Handle errors when creating subscriptions
            }
        }

        /// <summary>
        /// PATCH an existing subscription on the service to extend the expiration time.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static async Task UpdateExistingSubscriptionAsync(Account account, IOneDriveClient client, WebJobLogger log)
        {
            log.WriteLog("Updating existing subscription");

            // Make a request to create a new subscription
            string queryUrl = "/special/cameraroll/subscriptions/" + account.SubscriptionIdentifier;
            OneDriveSubscription postSub = CreateSubscription(account);

            try
            {
                var result = await client.SendRequestAsync<OneDriveSubscription>("PATCH", queryUrl, postSub);
                account.SubscriptionIdentifier = result.Id;
                log.WriteLog("Subscription updated. ID: {0}. Expiration: {1}", result.Id, result.ExpirationDateTime);
            }
            catch (Exception ex)
            {
                log.WriteLog("Error updating subscription: {0}", ex);
                // TODO: Handle the case where the existing subscription actually doesn't exist any more.
            }
        }

        /// <summary>
        /// Generate a subscription object for the camera roll with an expiration of 180 days.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private static OneDriveSubscription CreateSubscription(Account account)
        {
            return new OneDriveSubscription
            {
                Resource = "/drive/special/cameraroll",
                NotificationType = "webhook",
                NotificationUrl = SharedConfig.AppBaseUrl + "/api/webhook",
                Context = account.Id,
                ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(180)
            };
        }
        #endregion

    }
}
