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

            var account = await AzureStorage.LookupAccountAsync(userId);
            if (null == account)
            {
                await log.WriteLineAsync("Unable to locate matching account for id: " + userId);
                return;
            }

            await OrganizeAccountCameraRoll(account, log);
        }

        public static async Task OrganizeAccountCameraRoll(Account account, TextWriter log)
        {
            account.WebhooksReceived += 1;
            if (account.Enabled)
            {
                await CallOneDriveApi(account, log);
            }
            await AzureStorage.UpdateAccountAsync(account);
        }

        private static async Task CallOneDriveApi(Account account, TextWriter log)
        {
            OneDriveClient client = new OneDriveClient(OneDriveApiRootUrl, account, CachedHttpProvider);
            IChildrenCollectionPage response = null;
            try
            {
                var childrenRequest = client.Drive.Special["cameraroll"].Children.Request();
                response = await childrenRequest.GetAsync();
            }
            catch (Exception ex)
            {
                log.WriteLine("Exception getting cameraroll children: " + ex.ToString());
            }

            if (null != response)
            {
                foreach (var item in response)
                {
                    string destinationFolder = null;
                    if (null != item.Photo && null != item.Photo.TakenDateTime)
                    {
                        await log.WriteLineAsync("Processing photo: " + item.Name);
                        destinationFolder = item.Photo.TakenDateTime.Value.ToString(account.SubfolderFormat);
                    }
                    else if (null != item.Image && null != item.CreatedDateTime)
                    {
                        await log.WriteLineAsync("Processing image: " + item.Name);
                        destinationFolder = item.CreatedDateTime.Value.ToString(account.SubfolderFormat);
                    }
                    else
                    {
                        await log.WriteLineAsync("Skipped item: " + item.Name);
                        continue;
                    }

                    if (null != destinationFolder)
                    {
                        account.PhotosOrganized += 1;
                        var patchedItem = new Item { ParentReference = new ItemReference { Path = item.ParentReference.Path + "/" + destinationFolder } };
                        try
                        {
                            await client.Drive.Items[item.Id].Request().UpdateAsync(patchedItem);
                        }
                        catch (OneDriveException ex)
                        {
                            log.WriteLine("Exception thrown: " + ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
