using System;
using System.Diagnostics;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Services.Media
{
    /// <summary>
    /// Picture service for Windows Azure
    /// </summary>
    public partial class AzurePictureService : PictureService
    {
        #region Fields
        
        private static BlobContainerClient _containerClient;

        private readonly MediaSettings _mediaSettings;
        private readonly NopConfig _config;

        #endregion

        #region Ctor

        public AzurePictureService(IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            ISettingService settingService,
            IWebHelper webHelper,
            ILogger logger,
            IDbContext dbContext,
            IEventPublisher eventPublisher,
            MediaSettings mediaSettings,
            NopConfig config,
            IDataProvider dataProvider)
            : base(pictureRepository,
                productPictureRepository,
                settingService,
                webHelper,
                logger,
                dbContext,
                eventPublisher,
                mediaSettings,
                dataProvider)
        {
            this._mediaSettings = mediaSettings;
            this._config = config;

            if (String.IsNullOrEmpty(_config.AzureBlobStorageConnectionString))
                throw new Exception("Azure connection string for BLOB is not specified");
            if (String.IsNullOrEmpty(_config.AzureBlobStorageContainerName))
                throw new Exception("Azure container name for BLOB is not specified");
            if (String.IsNullOrEmpty(_config.AzureBlobStorageEndPoint))
                throw new Exception("Azure end point for BLOB is not specified");

            var blobServiceClient = new BlobServiceClient(_config.AzureBlobStorageConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(_config.AzureBlobStorageContainerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        #endregion

        #region Utilities
        
        /// <summary>
        /// Delete picture thumbs
        /// </summary>
        /// <param name="picture">Picture</param>
        protected override void DeletePictureThumbs(Picture picture)
        {
            string prefix = picture.Id.ToString("0000000");
            var blobs = _containerClient.GetBlobs(prefix: prefix);
            foreach (var blobItem in blobs)
            {
                _containerClient.DeleteBlobIfExists(blobItem.Name);
            }
        }

        /// <summary>
        /// Get picture (thumb) local path
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <returns>Local picture thumb path</returns>
        protected override string GetThumbLocalPath(string thumbFileName)
        {
            var thumbFilePath = _config.AzureBlobStorageEndPoint + _config.AzureBlobStorageContainerName + "/" + thumbFileName;
            return thumbFilePath;
        }

        /// <summary>
        /// Get picture (thumb) URL 
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <returns>Local picture thumb path</returns>
        protected override string GetThumbUrl(string thumbFileName, string storeLocation = null)
        {
            var url = _config.AzureBlobStorageEndPoint + _config.AzureBlobStorageContainerName + "/";
            url = url + thumbFileName;
            return url;
        }

        /// <summary>
        /// Get a value indicating whether some file (thumb) already exists
        /// </summary>
        /// <param name="thumbFilePath">Thumb file path</param>
        /// <param name="thumbFileName">Thumb file name</param>
        /// <returns>Result</returns>
        protected override bool GeneratedThumbExists(string thumbFilePath, string thumbFileName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(thumbFileName);
                return blobClient.Exists();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Save a value indicating whether some file (thumb) already exists
        /// </summary>
        /// <param name="thumbFilePath">Thumb file path</param>
        /// <param name="thumbFileName">Thumb file name</param>
        /// <param name="mimeType">MIME type</param>
        /// <param name="binary">Picture binary</param>
        protected override void SaveThumb(string thumbFilePath, string thumbFileName, string mimeType, byte[] binary)
        {
            var blobClient = _containerClient.GetBlobClient(thumbFileName);
            
            using (var stream = new MemoryStream(binary))
            {
                blobClient.Upload(stream, overwrite: true);
            }

            //set content type and cache control
            var headers = new BlobHttpHeaders();
            if (!String.IsNullOrEmpty(mimeType))
                headers.ContentType = mimeType;
            if (!string.IsNullOrEmpty(_mediaSettings.AzureCacheControlHeader))
                headers.CacheControl = _mediaSettings.AzureCacheControlHeader;
            
            blobClient.SetHttpHeaders(headers);
        }

        #endregion
    }
}
