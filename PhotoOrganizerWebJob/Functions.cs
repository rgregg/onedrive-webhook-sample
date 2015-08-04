using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Web;
using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using Microsoft.OneDrive.Sdk;

namespace PhotoOrganizerWebJob
{
    using PhotoOrganizerShared.Utility;

    public class Functions
    {
        private static readonly IHttpProvider CachedHttpProvider = new HttpProvider(new Serializer());
        private static KeyedLock AccountLocker = new KeyedLock();

        /// <summary>
        /// This method is automatically exuected by the Azure SDK whenever a new item is added to the queue named
        /// "subscriptions" -- it parses the queue message, finds the associated account, and then kicks off
        /// the webhook processing job.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task ProcessQueueMessage([QueueTrigger("subscriptions")] string message, TextWriter log)
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
                await WebhookActionForAccount(account, log);
            }
            catch (Exception ex)
            {
                log.WriteFormattedLine("Error while running job for account {0}: {1}", account.Id, ex);
            }
        }

        public static async Task WebhookActionForAccount(Account account, TextWriter log)
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
                    OneDriveClient client = new OneDriveClient(SharedConfig.Default.OneDriveBaseUrl, account, CachedHttpProvider);
                    FolderOrganizer organizer = new FolderOrganizer(client, account, log);
                    await organizer.OrganizeSourceFolderItemChangesAsync();

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
