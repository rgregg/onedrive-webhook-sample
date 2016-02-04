using Microsoft.OneDrive.Sdk;
using PhotoOrganizerShared;
using PhotoOrganizerShared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoOrganizerWebJob
{
    /// <summary>
    /// Handles organizing the files within a folder into child folders
    /// specified by a format string
    /// </summary>
    internal class FolderOrganizer
    {
        #region Instance variables
        private readonly IOneDriveClient client;
        private readonly Account account;
        private readonly WebJobLogger log;
        private readonly Dictionary<string, Item> cachedFolders = new Dictionary<string, Item>();
        private int itemsOrganized;
        #endregion

        #region Constructor
        public FolderOrganizer(IOneDriveClient client, Account account, WebJobLogger log)
        {
            this.client = client;
            this.account = account;
            this.log = log;
        }
        #endregion

        /// <summary>
        /// Use the view.delta method to get a list of changes that should be processed
        /// and organize those items accordingly into child folders.
        /// 
        /// Updates the Account provided to this instance and expects it to be persisted when
        /// this method returns.
        /// </summary>
        /// <returns>The number of items that were processed</returns>
        public async Task<long> OrganizeSourceFolderItemChangesAsync()
        {
            Item sourceFolderItem = await this.GetSourceFolderAsync();
            #region Error Handling and Logging
            if (null == sourceFolderItem)
            {
                this.log.WriteLog("No source folder was returned. Exiting.");
                return 0;
            }
            this.log.WriteLog("Requesting view.changes with token {0}", this.account.SyncToken);
            #endregion

            // Build a request to OneDrive
            IItemDeltaRequest firstPageRequest = this.SourceFolder.Delta(this.account.SyncToken).Request();

            try
            {
                var pagedResponse = await firstPageRequest.GetAsync();
                while (null != pagedResponse)
                {
                    #region Save the current sync token for later
                    this.account.SyncToken = pagedResponse.Token;
                    this.log.WriteLog("Received new sync token: {0}", this.account.SyncToken);
                    this.log.WriteLog("Response page includes {0} items.", pagedResponse.CurrentPage.Count);
                    #endregion

                    // Process the items in this page
                    await this.MoveItemsAsync(pagedResponse.CurrentPage, sourceFolderItem);

                    // Save the new sync token so we don't have to do this page of results again if something goes bad.
                    await AzureStorage.UpdateAccountAsync(account);

                    // Retrieve the next page of results, if we got non-zero results back
                    if (pagedResponse.NextPageRequest != null)
                    {
                        pagedResponse = await pagedResponse.NextPageRequest.GetAsync();
                    }
                    else
                    {
                        this.log.WriteLog("No results, so we're at the end of the list.");
                        pagedResponse = null;
                    }
                }
            }
            #region Error Handling
            catch (OneDriveException ex)
            {
                this.log.WriteLog("Error making request to service: {0}", ex);
                if (ex.IsMatchCode(OneDriveErrorCode.ResyncRequired))
                {
                    this.log.WriteLog("Resetting sync token and starting again.", ex);
                    this.account.SyncToken = null;
                    return await OrganizeSourceFolderItemChangesAsync();
                }
                return 0;
            }
            catch (Exception exc)
            {
                this.log.WriteLog("General exception organizing items: {0}", exc);
                return 0;
            }
            #endregion

            this.account.PhotosOrganized += this.itemsOrganized;
            return this.itemsOrganized;
        }

        #region Helper Methods
        private async Task<Item> GetSourceFolderAsync()
        {
            Item sourceFolderItem = null;
            try
            {
                this.log.WriteLog("Requesting source folder...");
                sourceFolderItem = await this.SourceFolder.Request().GetAsync();
            }
            catch (OneDriveException ex)
            {
                this.log.WriteLog("Unable to get source folder: {0}", ex);
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
        #endregion

        /// <summary>
        /// Move the items in the collection that should be moved to the approriate
        /// destination folder based on a date value on the item.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private async Task MoveItemsAsync(IList<Item> items, Item sourceFolder)
        {
            this.log.WriteLog("Moving items from the current page...");
            foreach (var item in items)
            {
                #region Check to see if item should be skipped
                Console.Write(".");

                string skippedReason;
                if (!this.ShouldMoveItem(item, sourceFolder, out skippedReason))
                {
                    // skip folders, we don't want to move them
                    this.log.WriteLog(null, "Skipping item '{0}': {1}", item.Name, skippedReason);
                    continue;
                }
                #endregion

                string destinationPath = this.ComputeDestinationPath(item);
                #region Error Handling
                if (string.IsNullOrEmpty(destinationPath))
                {
                    this.log.WriteLog(null, "Destination path was null, skipping file: {0}", item.Name);
                    continue;
                }
                this.log.WriteLog("Moving item {0} to folder {1}", item.Name, destinationPath);
                #endregion

                var destinationFolder = await this.CreateFolderFromPathAsync(destinationPath, sourceFolder);
                if (null != destinationFolder)
                {
                    var fileItemChanges = new Item { ParentReference = new ItemReference { Id = destinationFolder.Id } };
                    try
                    {
                        #region Logging
                        this.log.WriteLog(null, "Patching item {0} with parentReference.id = {1}", item.Name, destinationFolder.Id);
                        #endregion

                        var movedItem = await this.client.Drive.Items[item.Id].Request().Select("id").UpdateAsync(fileItemChanges);

                        #region Logging
                        ++this.itemsOrganized;
                        this.log.WriteLog(null, "Moved file {0} to path {1}", item.Id, destinationPath);
                        #endregion
                    }
                    catch (OneDriveException ex)
                    {
                        #region Error Handling
                        if (ex.IsMatchCode(OneDriveErrorCode.NameAlreadyExists))
                        {
                            this.log.WriteLog(null, "File {0} already exists in {1}. Need to rename.", item.Name, destinationPath);
                        }
                        else
                        {
                            this.log.WriteLog("Unable to move file {0}: {1}", item.Name, ex);
                        }
                        #endregion
                    }
                }
            }
        }

        /// <summary>
        /// Create a destination path on the service, if it doesn't already exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<Item> CreateFolderFromPathAsync(string path, Item sourceFolder)
        {
            Item destinationItem;
            #region Caching
            if (this.cachedFolders.TryGetValue(path, out destinationItem))
            {
                return destinationItem;
            }

            this.log.WriteLog("Creating destination folder {0}...", path);
            #endregion
            
            try
            {
                // Build a request to create a new folder with path relative to sourceFolder
                var request = this.client.Drive.Items[sourceFolder.Id].ItemWithPath(path).Request();
                var newFolderItem = new Item { Folder = new Folder() };
                destinationItem = await request.UpdateAsync(newFolderItem);

                #region Caching
                if (null != destinationItem)
                {
                    this.cachedFolders[path] = destinationItem;
                }
                #endregion
                return destinationItem;
            }
            #region Error Handling
            catch (Exception ex)
            {
                this.log.WriteLog("Error creating folder: {0}", ex.Message);
            }
            return null;
            #endregion
        }

        #region Helper Methods
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
                this.log.WriteLog("Using Photo.TakenDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.Photo.TakenDateTime);
            }
            else if (null != item.FileSystemInfo && null != item.FileSystemInfo.CreatedDateTime)
            {
                this.log.WriteLog("Using FileSystemInfo.CreatedDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.FileSystemInfo.CreatedDateTime);
            }
            else if (null != item.CreatedDateTime)
            {
                this.log.WriteLog("Using Item.CreatedDateTime for date");
                return string.Format(this.account.SubfolderFormat, item.CreatedDateTime);
            }
            else
            {
                this.log.WriteLog("No date value available.");
                return null;
            }

        }

        private IItemRequestBuilder SourceFolder
        {
            get { return this.client.Drive.Special[this.account.SourceFolder]; }
        }
        #endregion




    }
}
