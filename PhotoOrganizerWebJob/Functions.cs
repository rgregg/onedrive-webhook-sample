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
            log.WriteLine(message);

            var elements = HttpUtility.ParseQueryString(message);
            string userId = elements["id"];
            if (string.IsNullOrEmpty(userId))
            {
                await log.WriteLineAsync("Null or empty 'id' property");
                return;
            }

            await log.WriteFormattedLineAsync("Processing webhook for user: {0}", userId);

            var account = await AzureStorage.LookupAccountAsync(userId);
            if (null == account)
            {
                await log.WriteFormattedLineAsync("Unable to locate account for id: {0}", userId);
                return;
            }

            try
            {
                await WebhookActionForAccountAsync(account, log);
            }
            catch (Exception ex)
            {
                log.WriteFormattedLine("Error while running job for account {0}: {1}", account.Id, ex);
            }
        }

        internal static async Task<IOneDriveClient> CreateOneDriveClientAsync(Account account)
        {
            try
            {
                return await OneDriveClient.GetSilentlyAuthenticatedMicrosoftAccountClient(
                    SharedConfig.AppClientID,
                    SharedConfig.RedirectUri,
                    SharedConfig.Scopes,
                    account.RefreshToken);

            }
            catch (OneDriveException ex)
            {
                if (ex.IsMatch(OneDriveErrorCode.AuthenticationFailure.ToString()))
                {
                    // This refresh token is no longer valid, the user needs to log in again.
                }
            }
            return null;
        }

        public static async Task WebhookActionForAccountAsync(Account account, TextWriter log)
        {
            // Acquire a simple lock to ensure that only one thread is processing 
            // an account at the same time to avoid concurrency issues.
            // NOTE: If the web job is running on multiple VMs, this will not be sufficent to
            // ensure errors don't occur from one account being processed multiple times.
            bool acquiredLock = AccountLocker.TryAcquireLock(account.Id);
            if (acquiredLock && account.Enabled)
            {
                try
                {
                    await AzureStorage.InsertActivityAsync(
                        new Activity
                        {
                            UserId = account.Id,
                            Type = Activity.ActivityEventCode.LookingForChanges,
                            Message = "Account lock acquired"
                        });
                    
                    await log.WriteFormattedLineAsync("Connecting to OneDrive...");

                    // Build a new OneDriveClient with the account information
                    var client = await CreateOneDriveClientAsync(account);

                    // Execute our organization class
                    FolderOrganizer organizer = new FolderOrganizer(client, account, log);
                    var countOfItems = await organizer.OrganizeSourceFolderItemChangesAsync();

                    // Record the account activity
                    await AzureStorage.InsertActivityAsync(
                        new Activity
                        {
                            UserId = account.Id,
                            Type = Activity.ActivityEventCode.AccountProcessed,
                            Message = "Account organization complete.",
                            WorkItemCount =  countOfItems
                        });

                    // Record that we received another webhook and save the account back to table storage
                    account.WebhooksReceived += 1;
                    await AzureStorage.UpdateAccountAsync(account);
                    await log.WriteFormattedLineAsync("Updated account {0} with hooks received: {1}", account.Id, account.WebhooksReceived);
                }
                catch (Exception ex)
                {
                    log.WriteFormattedLine("Exception: {0}", ex);
                }
                finally
                {
                    AccountLocker.ReleaseLock(account.Id);
                }

                await log.WriteFormattedLineAsync("Processing completed for account: {0}", account.Id);
            }
            else
            {
                await log.WriteFormattedLineAsync("Failed to acquire lock for account. Another thread is already processing updates for this account.");
            }

        }

    }
}
