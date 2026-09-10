using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Seo;

namespace Nop.Services.Media
{
    /// <summary>
    /// Picture service
    /// </summary>
    /// <remarks>
    /// Task 4.2 (design section 7, Requirements 5.8, 5.9, 5.11). Both legacy imaging
    /// dependencies were removed from this file:
    ///
    ///   * <c>ImageResizer</c> 4.0.5 (<c>ImageBuilder.Current.Build</c> + <c>ResizeSettings</c>)
    ///     has no net10.0 release at all - it is a net45/System.Web-era pipeline.
    ///   * <c>System.Drawing</c> (<c>Bitmap</c>, <c>Size</c>) is Windows-only from .NET 6 and
    ///     throws <c>PlatformNotSupportedException</c> elsewhere.
    ///
    /// Both are replaced by <c>SixLabors.ImageSharp</c>: <c>new Bitmap(...)</c> becomes
    /// <c>Image.Load(path, out IImageFormat format)</c> / <c>Image.Load(stream, out format)</c>,
    /// <c>Bitmap.Size</c> becomes <c>image.Size()</c> (which returns
    /// <see cref="SixLabors.ImageSharp.Size"/>, also replacing <c>System.Drawing.Size</c> in
    /// <see cref="CalculateDimensions"/>), and each <c>ImageBuilder.Current.Build</c> becomes
    /// <c>image.Mutate(x =&gt; x.Resize(new ResizeOptions { ... }))</c> followed by
    /// <c>image.Save(stream, encoder)</c>.
    ///
    /// IMAGESHARP VERSION: the package is pinned to the 2.1.x line because it is the last
    /// line published under a plain Apache-2.0 licence (see the rationale in
    /// src/Directory.Packages.props). Three consequences for the code below, all verified
    /// against the 2.1.13 assembly:
    ///   * There is no <c>ImageMetadata.DecodedImageFormat</c> (a 3.0+ addition). The decoded
    ///     format is instead obtained from the <c>Image.Load(..., out IImageFormat format)</c>
    ///     overloads and threaded explicitly through <see cref="ResizeImage"/> into
    ///     <see cref="EncodeImage"/>, which is why both take an <see cref="IImageFormat"/>.
    ///   * <c>ImageFormatManager</c> exposes <c>FindEncoder(format)</c>, not
    ///     <c>GetEncoder(format)</c>.
    ///   * <c>Size</c> is an extension method on <c>IImageInfo</c>
    ///     (<c>SixLabors.ImageSharp.ImageInfoExtensions.Size</c>), so it is called as
    ///     <c>image.Size()</c>, not read as a property.
    /// Everything else used here - <see cref="ImageFormatException"/> (base of
    /// <c>UnknownImageFormatException</c> and <c>InvalidImageContentException</c>),
    /// <see cref="ResizeOptions"/>, <see cref="ResizeMode.Max"/>,
    /// <c>KnownResamplers.Bicubic</c>, <c>JpegEncoder.Quality</c>, <c>image.Mutate(...)</c>
    /// and <c>image.Save(stream, encoder)</c> - has the same shape in 2.1.x as in 4.x.
    ///
    /// RESIZE SEMANTICS - preserved, with one deliberate refinement:
    ///   * <see cref="CalculateDimensions"/> is untouched. It is still the single place that
    ///     decides the target box from <c>ResizeType</c> (LongestSide / Width / Height) and it
    ///     still computes a box that already matches the source aspect ratio, still clamps to a
    ///     minimum of 1px, and still uses <c>Math.Round</c>.
    ///   * ImageResizer was invoked with an explicit Width AND Height, for which its default
    ///     <c>FitMode</c> is <c>Pad</c> - it produced exactly the requested box and filled any
    ///     sub-pixel remainder with background. The <c>Math.Round</c> comment below records that
    ///     nopCommerce was fighting exactly that white padding. ImageSharp is therefore used
    ///     with <see cref="ResizeMode.Max"/>, which scales to fit inside the same box preserving
    ///     aspect ratio and never pads. Observable difference: an output edge may be up to one
    ///     pixel shorter than ImageResizer's padded output, and no background is ever
    ///     introduced. No image is distorted and no target-size rule changed.
    ///   * <c>Scale = ScaleMode.Both</c> (ImageResizer: upscaling permitted) is preserved -
    ///     <see cref="ResizeMode.Max"/> scales up as well as down.
    ///   * <c>Quality</c> mapped onto <see cref="JpegEncoder.Quality"/>; see
    ///     <see cref="GetImageEncoder"/> for the other formats.
    ///   * <see cref="ValidatePicture"/> used ImageResizer's <c>MaxWidth</c>/<c>MaxHeight</c>,
    ///     which shrink-to-fit and never enlarge. That is reproduced by only resizing when the
    ///     source actually exceeds the limit.
    ///   * <c>ImageResizer.Plugins.PrettyGifs</c> (GIF palette quantization) is covered by
    ///     ImageSharp's built-in GIF encoder, which quantizes by default (design section 7).
    /// </remarks>
    public partial class PictureService : IPictureService
    {
        #region Const

        private const int MULTIPLE_THUMB_DIRECTORIES_LENGTH = 3;

        #endregion

        #region Fields

        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILogger _logger;
        private readonly IDbContext _dbContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly MediaSettings _mediaSettings;
        private readonly IDataProvider _dataProvider;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pictureRepository">Picture repository</param>
        /// <param name="productPictureRepository">Product picture repository</param>
        /// <param name="settingService">Setting service</param>
        /// <param name="webHelper">Web helper</param>
        /// <param name="logger">Logger</param>
        /// <param name="dbContext">Database context</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="mediaSettings">Media settings</param>
        /// <param name="dataProvider">Data provider</param>
        public PictureService(IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            ISettingService settingService,
            IWebHelper webHelper,
            ILogger logger,
            IDbContext dbContext,
            IEventPublisher eventPublisher,
            MediaSettings mediaSettings,
            IDataProvider dataProvider)
        {
            this._pictureRepository = pictureRepository;
            this._productPictureRepository = productPictureRepository;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._logger = logger;
            this._dbContext = dbContext;
            this._eventPublisher = eventPublisher;
            this._mediaSettings = mediaSettings;
            this._dataProvider = dataProvider;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Calculates picture dimensions whilst maintaining aspect
        /// </summary>
        /// <param name="originalSize">The original picture size</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="resizeType">Resize type</param>
        /// <param name="ensureSizePositive">A value indicatingh whether we should ensure that size values are positive</param>
        /// <returns></returns>
        protected virtual Size CalculateDimensions(Size originalSize, int targetSize,
            ResizeType resizeType = ResizeType.LongestSide, bool ensureSizePositive = true)
        {
            float width, height;

            switch (resizeType)
            {
                case ResizeType.LongestSide:
                    if (originalSize.Height > originalSize.Width)
                    {
                        // portrait
                        width = originalSize.Width * (targetSize / (float)originalSize.Height);
                        height = targetSize;
                    }
                    else
                    {
                        // landscape or square
                        width = targetSize;
                        height = originalSize.Height * (targetSize / (float) originalSize.Width);
                    }
                    break;
                case ResizeType.Width:
                    width = targetSize;
                    height = originalSize.Height * (targetSize / (float)originalSize.Width);
                    break;
                case ResizeType.Height:
                    width = originalSize.Width * (targetSize / (float)originalSize.Height);
                    height = targetSize;
                    break;
                default:
                    throw new Exception("Not supported ResizeType");
            }

            if (ensureSizePositive)
            {
                if (width < 1)
                    width = 1;
                if (height < 1)
                    height = 1;
            }

            //we invoke Math.Round to ensure that no white background is rendered - http://www.nopcommerce.com/boards/t/40616/image-resizing-bug.aspx
            return new Size((int)Math.Round(width), (int)Math.Round(height));
        }

        /// <summary>
        /// Gets the ImageSharp resampler used for all thumbnail generation.
        /// </summary>
        /// <remarks>
        /// Bicubic is the closest match to the high-quality interpolation ImageResizer used by
        /// default, and is ImageSharp's own default for <c>Resize</c>.
        /// </remarks>
        protected virtual IResampler Resampler
        {
            get { return KnownResamplers.Bicubic; }
        }

        /// <summary>
        /// Gets the encoder to write an image back with
        /// </summary>
        /// <param name="format">The format the image was decoded from; may be null</param>
        /// <param name="quality">Requested quality (only meaningful for lossy formats)</param>
        /// <returns>Encoder</returns>
        /// <remarks>
        /// Replaces ImageResizer's implicit "save in the source format, honour
        /// <c>ResizeSettings.Quality</c>" behaviour. JPEG is the only in-scope format for which
        /// quality is a parameter; for every other format ImageSharp's registered default
        /// encoder is used, which for GIF performs the palette quantization that
        /// <c>ImageResizer.Plugins.PrettyGifs</c> used to provide. An unrecognised format falls
        /// back to JPEG, matching <see cref="GetFileExtensionFromMimeType"/>'s own JPEG default.
        /// </remarks>
        protected virtual IImageEncoder GetImageEncoder(IImageFormat format, int quality)
        {
            if (format == null)
                return new JpegEncoder { Quality = quality };

            if (format is JpegFormat)
                return new JpegEncoder { Quality = quality };

            var encoder = SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager.FindEncoder(format);
            return encoder ?? new JpegEncoder { Quality = quality };
        }

        /// <summary>
        /// Resizes an already-loaded image into a byte array, preserving its format
        /// </summary>
        /// <param name="image">Image to resize; mutated in place</param>
        /// <param name="format">The format the image was decoded from; may be null</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <returns>Encoded, resized image binary</returns>
        /// <remarks>
        /// The single replacement for
        /// <c>ImageBuilder.Current.Build(source, destStream, new ResizeSettings { Width, Height,
        /// Scale = ScaleMode.Both, Quality })</c>. See the class remarks for how the resize
        /// semantics map across, and for why the decoded format has to be passed in rather than
        /// read back off the image.
        /// </remarks>
        protected virtual byte[] ResizeImage(Image image, IImageFormat format, int targetSize)
        {
            var newSize = CalculateDimensions(image.Size(), targetSize);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = newSize,
                //aspect-ratio-preserving fit inside the computed box, no padding, upscaling
                //allowed - see the class remarks
                Mode = ResizeMode.Max,
                Sampler = Resampler
            }));

            return EncodeImage(image, format);
        }

        /// <summary>
        /// Encodes an image into a byte array using the format it was decoded from
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="format">The format the image was decoded from; may be null</param>
        /// <returns>Encoded image binary</returns>
        protected virtual byte[] EncodeImage(Image image, IImageFormat format)
        {
            var encoder = GetImageEncoder(format, _mediaSettings.DefaultImageQuality);

            using (var destStream = new MemoryStream())
            {
                image.Save(destStream, encoder);
                return destStream.ToArray();
            }
        }

        /// <summary>
        /// Returns the file extension from mime type.
        /// </summary>
        /// <param name="mimeType">Mime type</param>
        /// <returns>File extension</returns>
        protected virtual string GetFileExtensionFromMimeType(string mimeType)
        {
            if (mimeType == null)
                return null;

            //(3.90 pointed at System.Web.MimeMapping here for more mime types; on net10.0 the
            //equivalent is Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider,
            //as used by ExportImport/ImportManager.cs)

            string[] parts = mimeType.Split('/');
            string lastPart = parts[parts.Length - 1];
            switch (lastPart)
            {
                case "pjpeg":
                    lastPart = "jpg";
                    break;
                case "x-png":
                    lastPart = "png";
                    break;
                case "x-icon":
                    lastPart = "ico";
                    break;
            }
            return lastPart;
        }

        /// <summary>
        /// Loads a picture from file
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <param name="mimeType">MIME type</param>
        /// <returns>Picture binary</returns>
        protected virtual byte[] LoadPictureFromFile(int pictureId, string mimeType)
        {
            string lastPart = GetFileExtensionFromMimeType(mimeType);
            string fileName = string.Format("{0}_0.{1}", pictureId.ToString("0000000"), lastPart);
            var filePath = GetPictureLocalPath(fileName);
            if (!File.Exists(filePath))
                return new byte[0];
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// Save picture on file system
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <param name="pictureBinary">Picture binary</param>
        /// <param name="mimeType">MIME type</param>
        protected virtual void SavePictureInFile(int pictureId, byte[] pictureBinary, string mimeType)
        {
            string lastPart = GetFileExtensionFromMimeType(mimeType);
            string fileName = string.Format("{0}_0.{1}", pictureId.ToString("0000000"), lastPart);
            File.WriteAllBytes(GetPictureLocalPath(fileName), pictureBinary);
        }

        /// <summary>
        /// Delete a picture on file system
        /// </summary>
        /// <param name="picture">Picture</param>
        protected virtual void DeletePictureOnFileSystem(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException("picture");

            string lastPart = GetFileExtensionFromMimeType(picture.MimeType);
            string fileName = string.Format("{0}_0.{1}", picture.Id.ToString("0000000"), lastPart);
            string filePath = GetPictureLocalPath(fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Delete picture thumbs
        /// </summary>
        /// <param name="picture">Picture</param>
        protected virtual void DeletePictureThumbs(Picture picture)
        {
            string filter = string.Format("{0}*.*", picture.Id.ToString("0000000"));
            var thumbDirectoryPath = CommonHelper.MapPath("~/content/images/thumbs");
            string[] currentFiles = System.IO.Directory.GetFiles(thumbDirectoryPath, filter, SearchOption.AllDirectories);
            foreach (string currentFileName in currentFiles)
            {
                var thumbFilePath = GetThumbLocalPath(currentFileName);
                File.Delete(thumbFilePath);
            }
        }

        /// <summary>
        /// Get picture (thumb) local path
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <returns>Local picture thumb path</returns>
        protected virtual string GetThumbLocalPath(string thumbFileName)
        {
            var thumbsDirectoryPath = CommonHelper.MapPath("~/content/images/thumbs");
            if (_mediaSettings.MultipleThumbDirectories)
            {
                //get the first two letters of the file name
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(thumbFileName);
                if (fileNameWithoutExtension != null && fileNameWithoutExtension.Length > MULTIPLE_THUMB_DIRECTORIES_LENGTH)
                {
                    var subDirectoryName = fileNameWithoutExtension.Substring(0, MULTIPLE_THUMB_DIRECTORIES_LENGTH);
                    thumbsDirectoryPath = Path.Combine(thumbsDirectoryPath, subDirectoryName);
                    if (!System.IO.Directory.Exists(thumbsDirectoryPath))
                    {
                        System.IO.Directory.CreateDirectory(thumbsDirectoryPath);
                    }
                }
            }
            var thumbFilePath = Path.Combine(thumbsDirectoryPath, thumbFileName);
            return thumbFilePath;
        }

        /// <summary>
        /// Get picture (thumb) URL 
        /// </summary>
        /// <param name="thumbFileName">Filename</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <returns>Local picture thumb path</returns>
        protected virtual string GetThumbUrl(string thumbFileName, string storeLocation = null)
        {
            storeLocation = !String.IsNullOrEmpty(storeLocation)
                                    ? storeLocation
                                    : _webHelper.GetStoreLocation();
            var url = storeLocation + "content/images/thumbs/";

            if (_mediaSettings.MultipleThumbDirectories)
            {
                //get the first two letters of the file name
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(thumbFileName);
                if (fileNameWithoutExtension != null && fileNameWithoutExtension.Length > MULTIPLE_THUMB_DIRECTORIES_LENGTH)
                {
                    var subDirectoryName = fileNameWithoutExtension.Substring(0, MULTIPLE_THUMB_DIRECTORIES_LENGTH);
                    url = url + subDirectoryName + "/";
                }
            }

            url = url + thumbFileName;
            return url;
        }

        /// <summary>
        /// Get picture local path. Used when images stored on file system (not in the database)
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Local picture path</returns>
        protected virtual string GetPictureLocalPath(string fileName)
        {
            return Path.Combine(CommonHelper.MapPath("~/content/images/"), fileName);
        }

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <param name="picture">Picture</param>
        /// <param name="fromDb">Load from database; otherwise, from file system</param>
        /// <returns>Picture binary</returns>
        protected virtual byte[] LoadPictureBinary(Picture picture, bool fromDb)
        {
            if (picture == null)
                throw new ArgumentNullException("picture");

            var result = fromDb
                ? picture.PictureBinary
                : LoadPictureFromFile(picture.Id, picture.MimeType);
            return result;
        }

        /// <summary>
        /// Get a value indicating whether some file (thumb) already exists
        /// </summary>
        /// <param name="thumbFilePath">Thumb file path</param>
        /// <param name="thumbFileName">Thumb file name</param>
        /// <returns>Result</returns>
        protected virtual bool GeneratedThumbExists(string thumbFilePath, string thumbFileName)
        {
            return File.Exists(thumbFilePath);
        }

        /// <summary>
        /// Save a value indicating whether some file (thumb) already exists
        /// </summary>
        /// <param name="thumbFilePath">Thumb file path</param>
        /// <param name="thumbFileName">Thumb file name</param>
        /// <param name="mimeType">MIME type</param>
        /// <param name="binary">Picture binary</param>
        protected virtual void SaveThumb(string thumbFilePath, string thumbFileName, string mimeType, byte[] binary)
        {
            File.WriteAllBytes(thumbFilePath, binary);
        }

        #endregion

        #region Getting picture local path/URL methods

        /// <summary>
        /// Gets the loaded picture binary depending on picture storage settings
        /// </summary>
        /// <param name="picture">Picture</param>
        /// <returns>Picture binary</returns>
        public virtual byte[] LoadPictureBinary(Picture picture)
        {
            return LoadPictureBinary(picture, this.StoreInDb);
        }

        /// <summary>
        /// Get picture SEO friendly name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Result</returns>
        public virtual string GetPictureSeName(string name)
        {
            return SeoExtensions.GetSeName(name, true, false);
        }

        /// <summary>
        /// Gets the default picture URL
        /// </summary>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="defaultPictureType">Default picture type</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <returns>Picture URL</returns>
        public virtual string GetDefaultPictureUrl(int targetSize = 0,
            PictureType defaultPictureType = PictureType.Entity,
            string storeLocation = null)
        {
            string defaultImageFileName;
            switch (defaultPictureType)
            {
                case PictureType.Avatar:
                    defaultImageFileName = _settingService.GetSettingByKey("Media.Customer.DefaultAvatarImageName", "default-avatar.jpg");
                    break;
                case PictureType.Entity:
                default:
                    defaultImageFileName = _settingService.GetSettingByKey("Media.DefaultImageName", "default-image.png");
                    break;
            }
            string filePath = GetPictureLocalPath(defaultImageFileName);
            if (!File.Exists(filePath))
            {
                return "";
            }


            if (targetSize == 0)
            {
                string url = (!String.IsNullOrEmpty(storeLocation)
                                 ? storeLocation
                                 : _webHelper.GetStoreLocation())
                                 + "content/images/" + defaultImageFileName;
                return url;
            }
            else
            {
                string fileExtension = Path.GetExtension(filePath);
                string thumbFileName = string.Format("{0}_{1}{2}",
                    Path.GetFileNameWithoutExtension(filePath),
                    targetSize,
                    fileExtension);
                var thumbFilePath = GetThumbLocalPath(thumbFileName);
                if (!GeneratedThumbExists(thumbFilePath, thumbFileName))
                {
                    IImageFormat format;
                    using (var image = Image.Load(filePath, out format))
                    {
                        var destBinary = ResizeImage(image, format, targetSize);
                        SaveThumb(thumbFilePath, thumbFileName, "", destBinary);
                    }
                }
                var url = GetThumbUrl(thumbFileName, storeLocation);
                return url;
            }
        }

        /// <summary>
        /// Get a picture URL
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <param name="defaultPictureType">Default picture type</param>
        /// <returns>Picture URL</returns>
        public virtual string GetPictureUrl(int pictureId,
            int targetSize = 0,
            bool showDefaultPicture = true,
            string storeLocation = null,
            PictureType defaultPictureType = PictureType.Entity)
        {
            var picture = GetPictureById(pictureId);
            return GetPictureUrl(picture, targetSize, showDefaultPicture, storeLocation, defaultPictureType);
        }

        /// <summary>
        /// Get a picture URL
        /// </summary>
        /// <param name="picture">Picture instance</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
        /// <param name="storeLocation">Store location URL; null to use determine the current store location automatically</param>
        /// <param name="defaultPictureType">Default picture type</param>
        /// <returns>Picture URL</returns>
        public virtual string GetPictureUrl(Picture picture,
            int targetSize = 0,
            bool showDefaultPicture = true,
            string storeLocation = null,
            PictureType defaultPictureType = PictureType.Entity)
        {
            string url = string.Empty;
            byte[] pictureBinary = null;
            if (picture != null)
                pictureBinary = LoadPictureBinary(picture);
            if (picture == null || pictureBinary == null || pictureBinary.Length == 0)
            {
                if (showDefaultPicture)
                {
                    url = GetDefaultPictureUrl(targetSize, defaultPictureType, storeLocation);
                }
                return url;
            }

            if (picture.IsNew)
            {
                DeletePictureThumbs(picture);

                //we do not validate picture binary here to ensure that no exception ("Parameter is not valid") will be thrown
                picture = UpdatePicture(picture.Id,
                    pictureBinary,
                    picture.MimeType,
                    picture.SeoFilename,
                    picture.AltAttribute,
                    picture.TitleAttribute,
                    false,
                    false);
            }

            var seoFileName = picture.SeoFilename; // = GetPictureSeName(picture.SeoFilename); //just for sure
            
            string lastPart = GetFileExtensionFromMimeType(picture.MimeType);
            string thumbFileName;
            if (targetSize == 0)
            {
                thumbFileName = !String.IsNullOrEmpty(seoFileName)
                    ? string.Format("{0}_{1}.{2}", picture.Id.ToString("0000000"), seoFileName, lastPart)
                    : string.Format("{0}.{1}", picture.Id.ToString("0000000"), lastPart);
            }
            else
            {
                thumbFileName = !String.IsNullOrEmpty(seoFileName)
                    ? string.Format("{0}_{1}_{2}.{3}", picture.Id.ToString("0000000"), seoFileName, targetSize, lastPart)
                    : string.Format("{0}_{1}.{2}", picture.Id.ToString("0000000"), targetSize, lastPart);
            }
            string thumbFilePath = GetThumbLocalPath(thumbFileName);

            //the named mutex helps to avoid creating the same files in different threads,
            //and does not decrease performance significantly, because the code is blocked only for the specific file.
            using (var mutex = new Mutex(false, thumbFileName))
            {
                if(!GeneratedThumbExists(thumbFilePath, thumbFileName))
                { 
                    mutex.WaitOne();

                    //check, if the file was created, while we were waiting for the release of the mutex.
                    if (!GeneratedThumbExists(thumbFilePath, thumbFileName))
                    {
                        byte[] pictureBinaryResized;

                        //resizing required
                        if (targetSize != 0)
                        {
                            using (var stream = new MemoryStream(pictureBinary))
                            {
                                Image image = null;
                                IImageFormat format = null;
                                try
                                {
                                    //try-catch to ensure that picture binary is really OK. Otherwise, we can get
                                    //an ImageFormatException if the binary is corrupted for some reasons.
                                    //(ImageSharp's UnknownImageFormatException/InvalidImageContentException both
                                    //derive from ImageFormatException; it replaces the GDI+ ArgumentException
                                    //"Parameter is not valid" the original code caught.)
                                    //the out-parameter overload is how ImageSharp 2.x reports the decoded
                                    //format - see the class remarks
                                    image = Image.Load(stream, out format);
                                }
                                catch (ImageFormatException exc)
                                {
                                    _logger.Error(string.Format("Error generating picture thumb. ID={0}", picture.Id),
                                        exc);
                                }

                                if (image == null)
                                {
                                    //image could not be loaded for some reasons
                                    return url;
                                }

                                using (image)
                                {
                                    pictureBinaryResized = ResizeImage(image, format, targetSize);
                                }
                            }
                        }
                        else
                        {
                            //create a copy of pictureBinary
                            pictureBinaryResized = pictureBinary.ToArray();
                        }

                        SaveThumb(thumbFilePath, thumbFileName, picture.MimeType, pictureBinaryResized);
                    }
                    
                    mutex.ReleaseMutex();
                }
                
            }
            url = GetThumbUrl(thumbFileName, storeLocation);
            return url;
        }

        /// <summary>
        /// Get a picture local path
        /// </summary>
        /// <param name="picture">Picture instance</param>
        /// <param name="targetSize">The target picture size (longest side)</param>
        /// <param name="showDefaultPicture">A value indicating whether the default picture is shown</param>
        /// <returns></returns>
        public virtual string GetThumbLocalPath(Picture picture, int targetSize = 0, bool showDefaultPicture = true)
        {
            string url = GetPictureUrl(picture, targetSize, showDefaultPicture);
            if (String.IsNullOrEmpty(url))
                return String.Empty;

            return GetThumbLocalPath(Path.GetFileName(url));
        }

        #endregion

        #region CRUD methods

        /// <summary>
        /// Gets a picture
        /// </summary>
        /// <param name="pictureId">Picture identifier</param>
        /// <returns>Picture</returns>
        public virtual Picture GetPictureById(int pictureId)
        {
            if (pictureId == 0)
                return null;

            return _pictureRepository.GetById(pictureId);
        }

        /// <summary>
        /// Deletes a picture
        /// </summary>
        /// <param name="picture">Picture</param>
        public virtual void DeletePicture(Picture picture)
        {
            if (picture == null)
                throw new ArgumentNullException("picture");

            //delete thumbs
            DeletePictureThumbs(picture);

            //delete from file system
            if (!this.StoreInDb)
                DeletePictureOnFileSystem(picture);

            //delete from database
            _pictureRepository.Delete(picture);

            //event notification
            _eventPublisher.EntityDeleted(picture);
        }

        /// <summary>
        /// Gets a collection of pictures
        /// </summary>
        /// <param name="pageIndex">Current page</param>
        /// <param name="pageSize">Items on each page</param>
        /// <returns>Paged list of pictures</returns>
        public virtual IPagedList<Picture> GetPictures(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _pictureRepository.Table
                        orderby p.Id descending
                        select p;
            var pics = new PagedList<Picture>(query, pageIndex, pageSize);
            return pics;
        }
        
        /// <summary>
        /// Gets pictures by product identifier
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="recordsToReturn">Number of records to return. 0 if you want to get all items</param>
        /// <returns>Pictures</returns>
        public virtual IList<Picture> GetPicturesByProductId(int productId, int recordsToReturn = 0)
        {
            if (productId == 0)
                return new List<Picture>();


            var query = from p in _pictureRepository.Table
                        join pp in _productPictureRepository.Table on p.Id equals pp.PictureId
                        orderby pp.DisplayOrder, pp.Id
                        where pp.ProductId == productId
                        select p;

            if (recordsToReturn > 0)
                query = query.Take(recordsToReturn);

            var pics = query.ToList();
            return pics;
        }

        /// <summary>
        /// Inserts a picture
        /// </summary>
        /// <param name="pictureBinary">The picture binary</param>
        /// <param name="mimeType">The picture MIME type</param>
        /// <param name="seoFilename">The SEO filename</param>
        /// <param name="altAttribute">"alt" attribute for "img" HTML element</param>
        /// <param name="titleAttribute">"title" attribute for "img" HTML element</param>
        /// <param name="isNew">A value indicating whether the picture is new</param>
        /// <param name="validateBinary">A value indicating whether to validated provided picture binary</param>
        /// <returns>Picture</returns>
        public virtual Picture InsertPicture(byte[] pictureBinary, string mimeType, string seoFilename,
            string altAttribute = null, string titleAttribute = null,
            bool isNew = true, bool validateBinary = true)
        {
            mimeType = CommonHelper.EnsureNotNull(mimeType);
            mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

            seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

            if (validateBinary)
                pictureBinary = ValidatePicture(pictureBinary, mimeType);

            var picture = new Picture
            {
                PictureBinary = this.StoreInDb ? pictureBinary : new byte[0],
                MimeType = mimeType,
                SeoFilename = seoFilename,
                AltAttribute = altAttribute,
                TitleAttribute = titleAttribute,
                IsNew = isNew,
            };
            _pictureRepository.Insert(picture);

            if (!this.StoreInDb)
                SavePictureInFile(picture.Id, pictureBinary, mimeType);

            //event notification
            _eventPublisher.EntityInserted(picture);

            return picture;
        }

        /// <summary>
        /// Updates the picture
        /// </summary>
        /// <param name="pictureId">The picture identifier</param>
        /// <param name="pictureBinary">The picture binary</param>
        /// <param name="mimeType">The picture MIME type</param>
        /// <param name="seoFilename">The SEO filename</param>
        /// <param name="altAttribute">"alt" attribute for "img" HTML element</param>
        /// <param name="titleAttribute">"title" attribute for "img" HTML element</param>
        /// <param name="isNew">A value indicating whether the picture is new</param>
        /// <param name="validateBinary">A value indicating whether to validated provided picture binary</param>
        /// <returns>Picture</returns>
        public virtual Picture UpdatePicture(int pictureId, byte[] pictureBinary, string mimeType,
            string seoFilename, string altAttribute = null, string titleAttribute = null,
            bool isNew = true, bool validateBinary = true)
        {
            mimeType = CommonHelper.EnsureNotNull(mimeType);
            mimeType = CommonHelper.EnsureMaximumLength(mimeType, 20);

            seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

            if (validateBinary)
                pictureBinary = ValidatePicture(pictureBinary, mimeType);

            var picture = GetPictureById(pictureId);
            if (picture == null)
                return null;

            //delete old thumbs if a picture has been changed
            if (seoFilename != picture.SeoFilename)
                DeletePictureThumbs(picture);

            picture.PictureBinary = this.StoreInDb ? pictureBinary : new byte[0];
            picture.MimeType = mimeType;
            picture.SeoFilename = seoFilename;
            picture.AltAttribute = altAttribute;
            picture.TitleAttribute = titleAttribute;
            picture.IsNew = isNew;

            _pictureRepository.Update(picture);

            if (!this.StoreInDb)
                SavePictureInFile(picture.Id, pictureBinary, mimeType);

            //event notification
            _eventPublisher.EntityUpdated(picture);

            return picture;
        }

        /// <summary>
        /// Updates a SEO filename of a picture
        /// </summary>
        /// <param name="pictureId">The picture identifier</param>
        /// <param name="seoFilename">The SEO filename</param>
        /// <returns>Picture</returns>
        public virtual Picture SetSeoFilename(int pictureId, string seoFilename)
        {
            var picture = GetPictureById(pictureId);
            if (picture == null)
                throw new ArgumentException("No picture found with the specified id");

            //update if it has been changed
            if (seoFilename != picture.SeoFilename)
            {
                //update picture
                picture = UpdatePicture(picture.Id,
                    LoadPictureBinary(picture),
                    picture.MimeType,
                    seoFilename,
                    picture.AltAttribute,
                    picture.TitleAttribute,
                    true,
                    false);
            }
            return picture;
        }

        /// <summary>
        /// Validates input picture dimensions
        /// </summary>
        /// <param name="pictureBinary">Picture binary</param>
        /// <param name="mimeType">MIME type</param>
        /// <returns>Picture binary or throws an exception</returns>
        /// <remarks>
        /// Replaces <c>ImageBuilder.Current.Build(pictureBinary, destStream,
        /// new ResizeSettings { MaxWidth, MaxHeight, Quality })</c>. ImageResizer's
        /// <c>MaxWidth</c>/<c>MaxHeight</c> meant "shrink to fit within this box, never enlarge",
        /// which is <see cref="ResizeMode.Max"/> plus the explicit oversize guard below.
        /// As before, the image is always re-encoded (so <c>Quality</c> is applied) even when no
        /// resize is needed, keeping the returned binary's shape identical to 3.90's.
        /// </remarks>
        public virtual byte[] ValidatePicture(byte[] pictureBinary, string mimeType)
        {
            IImageFormat format;
            using (var image = Image.Load(pictureBinary, out format))
            {
                var maximumSize = _mediaSettings.MaximumImageSize;

                //never upscale - ImageResizer's MaxWidth/MaxHeight only ever shrank
                if (maximumSize > 0 && (image.Width > maximumSize || image.Height > maximumSize))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maximumSize, maximumSize),
                        Mode = ResizeMode.Max,
                        Sampler = Resampler
                    }));
                }

                return EncodeImage(image, format);
            }
        }

        /// <summary>
        /// Helper class for making pictures hashes from DB
        /// </summary>
        private class HashItem: IComparable, IComparable<HashItem>
        {
            public int PictureId { get; set; }
            public byte[] Hash { get; set; }

            public int CompareTo(object obj)
            {
                return CompareTo(obj as HashItem);
            }

            public int CompareTo(HashItem other)
            {
                return other == null ? -1 : PictureId.CompareTo(other.PictureId);
            }
        }

        /// <summary>
        /// Get pictures hashes
        /// </summary>
        /// <param name="picturesIds">Pictures Ids</param>
        /// <returns></returns>
        public IDictionary<int, string> GetPicturesHash(int[] picturesIds)
        {
            var supportedLengthOfBinaryHash = _dataProvider.SupportedLengthOfBinaryHash();
            if(supportedLengthOfBinaryHash == 0 || !picturesIds.Any())
                return new Dictionary<int, string>();

            const string strCommand = "SELECT [Id] as [PictureId], HASHBYTES('sha1', substring([PictureBinary], 0, {0})) as [Hash] FROM [Picture] where id in ({1})";
            return _dbContext.SqlQuery<HashItem>(String.Format(strCommand, supportedLengthOfBinaryHash, picturesIds.Select(p => p.ToString()).Aggregate((all, current) => all + ", " + current))).Distinct()
                .ToDictionary(p => p.PictureId, p => BitConverter.ToString(p.Hash).Replace("-", ""));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the images should be stored in data base.
        /// </summary>
        public virtual bool StoreInDb
        {
            get
            {
                return _settingService.GetSettingByKey("Media.Images.StoreInDB", true);
            }
            set
            {
                //check whether it's a new value
                if (this.StoreInDb == value)
                    return;

                //save the new setting value
                _settingService.SetSetting("Media.Images.StoreInDB", value);

                int pageIndex = 0;
                const int pageSize = 400;
                var originalProxyCreationEnabled = _dbContext.ProxyCreationEnabled;
                try
                {
                    //we set this property for performance optimization
                    //it could be critical if you we have several thousand pictures
                    _dbContext.ProxyCreationEnabled = false;

                    while (true)
                    {
                        var pictures = this.GetPictures(pageIndex, pageSize);
                        pageIndex++;

                        //all pictures converted?
                        if (!pictures.Any())
                            break;

                        foreach (var picture in pictures)
                        {
                            var pictureBinary = LoadPictureBinary(picture, !value);

                            //we used the code below before. but it's too slow
                            //let's do it manually (uncommented code) - copy some logic from "UpdatePicture" method
                            /*just update a picture (all required logic is in "UpdatePicture" method)
                            we do not validate picture binary here to ensure that no exception ("Parameter is not valid") will be thrown when "moving" pictures
                            UpdatePicture(picture.Id,
                                          pictureBinary,
                                          picture.MimeType,
                                          picture.SeoFilename,
                                          true,
                                          false);*/
                            if (value)
                                //delete from file system. now it's in the database
                                DeletePictureOnFileSystem(picture);
                            else
                                //now on file system
                                SavePictureInFile(picture.Id, pictureBinary, picture.MimeType);
                            //update appropriate properties
                            picture.PictureBinary = value ? pictureBinary : new byte[0];
                            picture.IsNew = true;
                            //raise event?
                            //_eventPublisher.EntityUpdated(picture);
                        }
                        //save all at once
                        _pictureRepository.Update(pictures);
                        //detach them in order to release memory
                        foreach (var picture in pictures)
                        {
                            _dbContext.Detach(picture);
                        }
                    }
                }
                finally
                {
                    _dbContext.ProxyCreationEnabled = originalProxyCreationEnabled;
                }
            }
        }

        #endregion
    }
}
