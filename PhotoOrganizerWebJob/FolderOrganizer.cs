using Microsoft.OneDrive.Sdk;
using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoOrganizerWebJob
{
    /// <summary>
    /// Handles organizing the files within a folder into child folders
    /// specified by a format string
    /// </summary>
    internal class FolderOrganizer
    {
        private readonly OneDriveClient client;
        private readonly Account account;
        private readonly TextWriter log;
        private readonly Dictionary<string, Item> cachedFolders = new Dictionary<string, Item>();

        private int itemsOrganized;

        public FolderOrganizer(OneDriveClient client, Account account, TextWriter log)
        {
            this.client = client;
            this.account = account;
            this.log = log;
        }

        /// <summary>
        /// Use the view.changes method to get a list of changes that should be processed
        /// and organize those items accordingly into child folders.
        /// 
        /// Updates the Account provided to this instance and expects it to be persisted when
        /// this method returns.
        /// </summary>
        /// <returns>The number of items that were processed</returns>
        public async Task<long> OrganizeSourceFolderItemChangesAsync()
        {
            Item sourceFolderItem = await this.GetSourceFolderAsync();
            if (null == sourceFolderItem)
            {
                this.WriteLog("No source folder was returned. Exiting.");
                return 0;
            }

            this.WriteLog("Requesting view.changes with token {0}", this.account.SyncToken);
            IItemChangesRequest firstPageRequest = this.SourceFolder.ItemChanges(this.account.SyncToken).Request();
            IItemChangesCollectionPage pagedResponse = null;
            try
            {
                this.WriteLog("Requesting page of changes...");
                pagedResponse = await firstPageRequest.GetAsync();
            }
            catch (OneDriveException ex)
            {
                this.WriteLog("Error making first request to service: {0}", ex);
                return 0;
            }

            while (null != pagedResponse)
            {
                // Check to see if we need to reset our sync token
                if (null != pagedResponse.AdditionalData && pagedResponse.AdditionalData.ContainsKey("@changes.resync"))
                {
                    // Need to clear the sync token and start over again
                    this.WriteLog("Service requests a resync. Need to restart with a null sync token: {0}", pagedResponse.AdditionalData["@changes.resync"]);
                    this.account.SyncToken = null;
                    return await this.OrganizeSourceFolderItemChangesAsync();
                }
                
                if (null != pagedResponse.AdditionalData)
                {
                    // Save the current sync token for later
                    this.account.SyncToken = pagedResponse.AdditionalData["@changes.token"] as string;
                    this.WriteLog("Received new sync token: {0}", this.account.SyncToken);
                }

                this.WriteLog("Response page includes {0} items.", pagedResponse.CurrentPage.Count);

                // Process the items in this page
                await this.MoveItemsAsync(pagedResponse.CurrentPage, sourceFolderItem);

                // Retrieve the next page of results, if we got non-zero results back
                if (pagedResponse.CurrentPage.Count > 0)
                {
                    this.WriteLog("Requesting next page of view.changes...");
                    var nextRequest = pagedResponse.NextPageRequest;
                    try
                    {
                        pagedResponse = await nextRequest.GetAsync();
                    }
                    catch (OneDriveException ex)
                    {
                        this.WriteLog("Error making request to service: {0}", ex);
                        pagedResponse = null;
                    }
                }
                else
                {
                    this.WriteLog("No results, so we're at the end of the list.");
                    pagedResponse = null;
                }
            }

            this.account.PhotosOrganized += this.itemsOrganized;
            return this.itemsOrganized;
        }

        private async Task<Item> GetSourceFolderAsync()
        {
            Item sourceFolderItem = null;
            try
            {
                this.WriteLog("Requesting source folder...");
                sourceFolderItem = await this.SourceFolder.Request().GetAsync();
            }
            catch (OneDriveException ex)
            {
                this.WriteLog("Unable to get source folder: {0}", ex);
            }
            return sourceFolderItem;
        }

        /// <summary>
        /// Determine whether or not an item qualifies to be organized
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ShouldMoveItem(Item item, Item sourceFolder, out string reason)
        {
            // Only move files, skip everything else
            if (null == item.File)
            {
                reason = "File was null";
                return false;
            }
            
            // Check to make sure the item is a parent of the source folder. We don't want
            // to move things that aren't in the root of the source folder.
            if (item.ParentReference.Id != sourceFolder.Id)
            {
                reason = "ParentReference.Id != sourceFolder.Id";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Move the items in the collection that should be moved to the approriate
        /// destination folder based on a date value on the item.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task MoveItemsAsync(IList<Item> items, Item sourceFolder)
        {
            this.WriteLog("Moving items from the current page...");
            foreach (var item in items)
            {
                string skippedReason;
                if (!this.ShouldMoveItem(item, sourceFolder, out skippedReason))
                {
                    // skip folders, we don't want to move them
                    this.WriteLog("Skipping item '{0}': {1}", item.Name, skippedReason);
                    continue;
                }

                string destinationPath = this.ComputeDestinationPath(item);
                if (string.IsNullOrEmpty(destinationPath))
                {
                    this.WriteLog("Destination path was null, skipping file: {0}", item.Name);
                    continue;
                }

                this.WriteLog("Moving item {0} to folder {1}", item.Name, destinationPath);
                var destination = await this.ResolveDestinationFolderAsync(destinationPath, sourceFolder);
                if (null != destination)
                {
                    var patchedItemUpdate = new Item { ParentReference = new ItemReference { Id = destination.Id } };
                    try
                    {
                        this.WriteLog("Patching item {0} with parentReference.id = {1}", item.Name, destination.Id);
                        var movedItem = await this.client.Drive.Items[item.Id].Request().UpdateAsync(patchedItemUpdate);
                        ++this.itemsOrganized;

                        // Record the account activity
                        await AzureStorage.InsertActivityAsync(
                            new Activity
                            {
                                UserId = this.account.Id,
                                Type = Activity.ActivityEventCode.FileMoved,
                                Message = string.Format("Moved file {0} to path {1}", item.Id, destinationPath)
                            });

                    }
                    catch (OneDriveException ex)
                    {
                        if (ex.IsMatchCode(OneDriveErrorCode.NameAlreadyExists))
                        {
                            this.WriteLog("File {0} already exists in {1}. Need to rename.", item.Name, destinationPath);
                        }
                        else
                        {
                            this.WriteLog("Unable to move file {0}: {1}", item.Name, ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check to see if we've previously resolved the folder and cached it, otherwise
        /// make an API call to retrieve the destination folder resource.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<Item> ResolveDestinationFolderAsync(string path, Item sourceFolder)
        {
            Item destinationItem;
            if (this.cachedFolders.TryGetValue(path, out destinationItem))
            {
                return destinationItem;
            }

            this.WriteLog("Creating destination folder {0}...", path);
            var emptyFolderPlaceholder = new Item { Folder = new Folder() };
            try
            {
                destinationItem =
                    await this.client.Drive.Items[sourceFolder.Id].ItemWithPath(path)
                            .Request()
                            .UpdateAsync(emptyFolderPlaceholder);
                if (null != destinationItem)
                {
                    this.cachedFolders[path] = destinationItem;
                }
                return destinationItem;
            }
            catch (Exception ex)
            {
                this.WriteLog("Error creating folder: {0}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Find a valid date value for a photo or file and then use the SubfolderFormat
        /// string to convert that into a path.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string ComputeDestinationPath(Item item)
        {
            if (null != item.Photo && null != item.Photo.TakenDateTime)
            {
                this.WriteLog("Using Photo.TakenDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.Photo.TakenDateTime);
            }
            else if (null != item.FileSystemInfo && null != item.FileSystemInfo.CreatedDateTime)
            {
                this.WriteLog("Using FileSystemInfo.CreatedDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.FileSystemInfo.CreatedDateTime);
            }
            else if (null != item.CreatedDateTime)
            {
                this.WriteLog("Using Item.CreatedDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.CreatedDateTime);
            }
            else
            {
                this.WriteLog("No date value available.");
                return null;
            }

        }

        private IItemRequestBuilder SourceFolder
        {
            get { return this.client.Drive.Special[this.account.SourceFolder]; }
        }


       
        #region Logging Methods

        private void WriteLog(string format, object value)
        {
            if (null != this.log)
            {
                this.log.WriteFormattedLine(format, value);
            }

            var t = AzureStorage.InsertActivityAsync(
                new Activity
                {
                    UserId = this.account.Id,
                    Type = Activity.ActivityEventCode.MessageLogged,
                    Message =
                        string.Format(format, value)
                });
            t.Wait();
        }

        private void WriteLog(string format, params object[] values)
        {
            if (null != this.log)
            {
                this.log.WriteFormattedLine(format, values);
            }

            var t = AzureStorage.InsertActivityAsync(
                new Activity
                {
                    UserId = this.account.Id,
                    Type = Activity.ActivityEventCode.MessageLogged,
                    Message = string.Format(format, values)
                });
            t.Wait();
        }

        #endregion
    }
}
