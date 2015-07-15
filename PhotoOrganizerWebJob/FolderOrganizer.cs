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
        /// Use the /children collection of the source folder to discover items that
        /// need to be organized and move them according.
        /// 
        /// Updates the Account instance variable and expects it to be saved after the methods
        /// has returned.
        /// </summary>
        /// <returns></returns>
        public async Task OrganizeSourceFolderChildrenAsync()
        {
            Item sourceFolderItem = await GetSourceFolderAsync();
            if (null == sourceFolderItem)
                return;
            
            IChildrenCollectionRequest firstPageRequest = SourceFolder.Children.Request();
            //var response = await _client.Drive.Root.ItemChanges("").Request().GetAsync();
            IChildrenCollectionPage pagedResponse = await GetResponsePageAsync(firstPageRequest);
            while (null != pagedResponse)
            {
                await MoveItemsAsync(pagedResponse.CurrentPage, sourceFolderItem);
                pagedResponse = await GetResponsePageAsync(pagedResponse.NextPageRequest);
            }

            _account.PhotosOrganized += _itemsOrganized;
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
                return;

            IItemChangesRequest firstPageRequest = SourceFolder.ItemChanges(_account.SyncToken).Request();

            IItemCollectionPage pagedResponse = await GetResponsePageAsync(firstPageRequest);
            while (null != pagedResponse)
            {
                await MoveItemsAsync(pagedResponse.CurrentPage, sourceFolderItem);
                pagedResponse = await GetResponsePageAsync(pagedResponse.NextPageRequest);
            }

            _account.PhotosOrganized += _itemsOrganized;
        }

        private async Task<Item> GetSourceFolderAsync()
        {
            Item sourceFolderItem = null;
            try
            {
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
        private bool ShouldMoveItem(Item item, Item sourceFolder)
        {
            // Only move files, skip everything else
            if (null == item.File)
            {
                return false;
            }
            
            // Check to make sure the item is a parent of the source folder. We don't want
            // to move things that aren't in the root of the source folder.
            if (item.ParentReference.Id != sourceFolder.Id)
            {
                return false;
            }

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
            foreach (var item in items)
            {
                if (!ShouldMoveItem(item, sourceFolder))
                {
                    // skip folders, we don't want to move them
                    WriteLog("Skipping item: {0}", item.Name);
                    continue;
                }

                string destinationPath = ComputeDestinationPath(item);
                if (string.IsNullOrEmpty(destinationPath))
                    continue;

                WriteLog("Moving item {0} to folder {1}", item.Name, destinationPath);
                var destination = await ResolveDestinationFolderAsync(destinationPath);

                var patchedItemUpdate = new Item { ParentReference = new ItemReference { Id = destination.Id } };
                try
                {
                    var movedItem = await _client.Drive.Items[item.Id].Request().UpdateAsync(patchedItemUpdate);
                    ++_itemsOrganized;
                }
                catch (OneDriveException ex)
                {
                    WriteLog("Unable to move file {0}: {1}", item.Name, ex);
                }
            }
        }

        /// <summary>
        /// Check to see if we've previously resolved the folder and cached it, otherwise
        /// make an API call to retrieve the destination folder resource.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<Item> ResolveDestinationFolderAsync(string path)
        {
            Item destinationItem;
            if (_cachedFolders.TryGetValue(path, out destinationItem))
                return destinationItem;

            var emptyFolderPlaceholder = new Item { Folder = new Folder() };
            destinationItem = await SourceFolder.ItemWithPath(path).Request().UpdateAsync(emptyFolderPlaceholder);
            if (null != destinationItem)
            {
                _cachedFolders[path] = destinationItem;
            }
            return destinationItem;
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


        #region get paged responses for various types of requests
        /// <summary>
        /// Catch any errors that occur getting a response page from the service and 
        /// report out to the logger.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IChildrenCollectionPage> GetResponsePageAsync(IChildrenCollectionRequest request)
        {
            if (request == null)
            {
                return null;
            }

            try
            {
                return await request.GetAsync();
            }
            catch (OneDriveException ex)
            {
                WriteLog("Error retriving children collection [{1}]: {0}", ex);
            }

            return null;
        }

        /// <summary>
        /// Catch any errors that occur getting a response page from the service and 
        /// report out to the logger.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IItemCollectionPage> GetResponsePageAsync(IItemChangesRequest request)
        {
            if (request == null)
            {
                return null;
            }

            try
            {
                return await request.GetAsync();
            }
            catch (OneDriveException ex)
            {
                WriteLog("Error retriving children collection [{1}]: {0}", ex);
            }

            return null;
        }

        /// <summary>
        /// Catch any errors that occur getting a response page from the service and 
        /// report out to the logger.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IItemCollectionPage> GetResponsePageAsync(IItemCollectionRequest request)
        {
            if (request == null)
            {
                return null;
            }

            try
            {
                return await request.GetAsync();
            }
            catch (OneDriveException ex)
            {
                WriteLog("Error retriving children collection [{1}]: {0}", ex);
            }

            return null;
        }
        #endregion

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
