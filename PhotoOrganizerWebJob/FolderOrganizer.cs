using Microsoft.OneDrive.Sdk;
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
        private readonly OneDriveClient _client;
        private readonly Account _account;
        private readonly TextWriter _log;
        private readonly Dictionary<string, Item> _cachedFolders = new Dictionary<string, Item>();

        private int _itemsOrganized;

        public FolderOrganizer(OneDriveClient client, Account account, TextWriter log)
        {
            _client = client;
            _account = account;
            _log = log;
        }

        /// <summary>
        /// Use the view.changes method to get a list of changes that should be processed
        /// and organize those items accordingly into child folders.
        /// 
        /// Updates the Account provided to this instance and expects it to be persisted when
        /// this method returns.
        /// </summary>
        /// <returns></returns>
        public async Task OrganizeSourceFolderItemChangesAsync()
        {
            Item sourceFolderItem = await GetSourceFolderAsync();
            if (null == sourceFolderItem)
            {
                WriteLog("No source folder was returned. Exiting.");
                return;
            }

            WriteLog("Requesting view.changes with token {0}", _account.SyncToken);
            IItemChangesRequest firstPageRequest = SourceFolder.ItemChanges(_account.SyncToken).Request();
            IItemChangesCollectionPage pagedResponse = null;
            try
            {
                WriteLog("Requesting page of changes...");
                pagedResponse = await firstPageRequest.GetAsync();
            }
            catch (OneDriveException ex)
            {
                WriteLog("Error making first request to service: {0}", ex);
                return;
            }

            while (null != pagedResponse)
            {
                // Check to see if we need to reset our sync token
                if (null != pagedResponse.AdditionalData && pagedResponse.AdditionalData.ContainsKey("@changes.resync"))
                {
                    // Need to clear the sync token and start over again
                    WriteLog("Service requests a resync. Need to restart with a null sync token: {0}", pagedResponse.AdditionalData["@changes.resync"]);
                    _account.SyncToken = null;
                    await OrganizeSourceFolderItemChangesAsync();
                    return;
                }
                else if (null != pagedResponse.AdditionalData)
                {
                    // Save the current sync token for later
                    _account.SyncToken = pagedResponse.AdditionalData["@changes.token"] as string;
                    WriteLog("Received new sync token: {0}", _account.SyncToken);
                }

                WriteLog("Response page includes {0} items.", pagedResponse.CurrentPage.Count);

                // Process the items in this page
                await MoveItemsAsync(pagedResponse.CurrentPage, sourceFolderItem);

                // Retrieve the next page of results, if we got non-zero results back
                if (pagedResponse.CurrentPage.Count > 0)
                {
                    WriteLog("Requesting next page of view.changes...");
                    var nextRequest = pagedResponse.NextPageRequest;
                    try
                    {
                        pagedResponse = await nextRequest.GetAsync();
                    }
                    catch (OneDriveException ex)
                    {
                        WriteLog("Error making request to service: {0}", ex);
                        pagedResponse = null;
                    }
                }
                else
                {
                    WriteLog("No results, so we're at the end of the list.");
                    pagedResponse = null;
                }
            }

            _account.PhotosOrganized += _itemsOrganized;
        }

        private async Task<Item> GetSourceFolderAsync()
        {
            Item sourceFolderItem = null;
            try
            {
                WriteLog("Requesting source folder...");
                sourceFolderItem = await SourceFolder.Request().GetAsync();
            }
            catch (OneDriveException ex)
            {
                WriteLog("Unable to get source folder: {0}", ex);
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
            WriteLog("Moving items from the current page...");
            foreach (var item in items)
            {
                string skippedReason;
                if (!ShouldMoveItem(item, sourceFolder, out skippedReason))
                {
                    // skip folders, we don't want to move them
                    WriteLog("Skipping item '{0}': {1}", item.Name, skippedReason);
                    continue;
                }

                string destinationPath = ComputeDestinationPath(item);
                if (string.IsNullOrEmpty(destinationPath))
                {
                    WriteLog("Destination path was null, skipping file: {0}", item.Name);
                    continue;
                }

                WriteLog("Moving item {0} to folder {1}", item.Name, destinationPath);
                var destination = await ResolveDestinationFolderAsync(destinationPath, sourceFolder);
                if (null != destination)
                {
                    var patchedItemUpdate = new Item { ParentReference = new ItemReference { Id = destination.Id } };
                    try
                    {
                        WriteLog("Patching item {0} with parentReference.id = {1}", item.Name, destination.Id);
                        var movedItem = await _client.Drive.Items[item.Id].Request().UpdateAsync(patchedItemUpdate);
                        ++_itemsOrganized;
                    }
                    catch (OneDriveException ex)
                    {
                        if (ex.IsMatchCode(OneDriveErrorCode.NameAlreadyExists))
                        {
                            WriteLog("File {0} already exists in {1}. Need to rename.", item.Name, destinationPath);
                        }
                        else
                        {
                            WriteLog("Unable to move file {0}: {1}", item.Name, ex);
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
            if (_cachedFolders.TryGetValue(path, out destinationItem))
            {
                return destinationItem;
            }

            WriteLog("Creating destination folder {0}...", path);
            var emptyFolderPlaceholder = new Item { Folder = new Folder() };
            try
            {
                destinationItem =
                    await
                        _client.Drive.Items[sourceFolder.Id].ItemWithPath(path)
                            .Request()
                            .UpdateAsync(emptyFolderPlaceholder);
                if (null != destinationItem)
                {
                    _cachedFolders[path] = destinationItem;
                }
                return destinationItem;
            }
            catch (Exception ex)
            {
                WriteLog("Error creating folder: {0}", ex.Message);
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
                WriteLog("Using Photo.TakenDateTime for date");
                return string.Format(_account.SubfolderFormat, item.Photo.TakenDateTime);
            }
            else if (null != item.FileSystemInfo && null != item.FileSystemInfo.CreatedDateTime)
            {
                WriteLog("Using FileSystemInfo.CreatedDateTime for date");
                return string.Format(_account.SubfolderFormat, item.FileSystemInfo.CreatedDateTime);
            }
            else if (null != item.CreatedDateTime)
            {
                WriteLog("Using Item.CreatedDateTime for date");
                return string.Format(_account.SubfolderFormat, item.CreatedDateTime);
            }
            else
            {
                WriteLog("No date value available.");
                return null;
            }

        }

        private IItemRequestBuilder SourceFolder
        {
            get { return _client.Drive.Special[_account.SourceFolder]; }
        }


       
        #region Logging Methods

        private void WriteLog(string format, object value)
        {
            if (null != _log)
            {
                _log.WriteFormattedLine(format, value);
            }
        }

        private void WriteLog(string format, params object[] values)
        {
            if (null != _log)
            {
                _log.WriteFormattedLine(format, values);
            }
        }

        #endregion
    }
}
