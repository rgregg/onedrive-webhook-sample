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
    public class Functions
    {
        private const string OneDriveApiRootUrl = "https://api.onedrive.com/v1.0";

        private static readonly IHttpProvider CachedHttpProvider = new HttpProvider(new Serializer());

        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
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

            await RunJobForAccount(account, log);
        }

        public static async Task RunJobForAccount(Account account, TextWriter log)
        {
            account.WebhooksReceived += 1;
            if (account.Enabled)
            {
                await OrganizePhotosInAccount(account, log);
            }
            await AzureStorage.UpdateAccountAsync(account);
        }

        /// <summary>
        /// Connect to the OneDrive API and organize the contents of the target folder
        /// </summary>
        /// <param name="account"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        private static async Task OrganizePhotosInAccount(Account account, TextWriter log)
        {
            OneDriveClient client = new OneDriveClient(OneDriveApiRootUrl, account, CachedHttpProvider);
            IChildrenCollectionPage response = null;

            await log.WriteLineAsync("Connecting to OneDrive...");
            try
            {
                var specialFolderName = account.SourceFolder;
                var childrenRequest = client.Drive.Special[specialFolderName].Children.Request();
                response = await childrenRequest.GetAsync();
                await log.WriteFormattedLineAsync("Found {0} items in the collection", response.Count);
            }
            catch (Exception ex)
            {
                log.WriteFormattedLine("Exception getting '{0}' children: {1}", account.SourceFolder, ex.ToString());
                return;
            }

            if (null == response)
            {
                await log.WriteLineAsync("Response was null. Aborting.");
                return;
            }

            foreach (var item in response)
            {
                string destinationFolder = null;
                await log.WriteFormattedLineAsync("Processing item: {0} [{1}]", item.Name, item.Id);

                if (null != item.Photo && null != item.Photo.TakenDateTime)
                {
                    destinationFolder = string.Format(account.SubfolderFormat, item.Photo.TakenDateTime.Value);
                    await log.WriteFormattedLineAsync("Moving photo {0} to {1}", item.Name, destinationFolder);
                }
                else if (null != item.FileSystemInfo && null != item.FileSystemInfo.CreatedDateTime)
                {
                    destinationFolder = string.Format(account.SubfolderFormat, item.FileSystemInfo.CreatedDateTime.Value);
                    await log.WriteFormattedLineAsync("Moving file {0} to {1} (based on clientCreatedDateTime)", item.Name, destinationFolder);
                }
                else if (null != item.CreatedDateTime)
                {
                    destinationFolder = string.Format(account.SubfolderFormat, item.CreatedDateTime.Value);
                    await log.WriteFormattedLineAsync("Moving file {0} to {1} (based on createdDateTime)", item.Name, destinationFolder);
                }
                else
                {
                    await log.WriteFormattedLineAsync("Skipped item {0}", item.Name);
                    continue;
                }

                if (null != destinationFolder)
                {
                    account.PhotosOrganized += 1;
                    var patchedItem = new Item { ParentReference = new ItemReference { Path = Path.Combine(item.ParentReference.Path, destinationFolder) } };

                    await log.WriteFormattedLineAsync("Patching item [{0}] with new parentReference: {1}", item.Id, patchedItem.ParentReference);

                    try
                    {
                        await client.Drive.Items[item.Id].Request().UpdateAsync(patchedItem);
                    }
                    catch (OneDriveException ex)
                    {
                        log.WriteFormattedLine("Exception thrown: {0}", ex.ToString());
                    }
                }
            }
        }
    }
}
