using Microsoft.AspNetCore.Hosting;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Seo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using IODirectory = System.IO.Directory;

namespace Nop.Services.Media;

public class PictureService : IPictureService
{
    private const int MultipleThumbDirectoriesLength = 3;

    private readonly IRepository<Picture> _pictureRepository;
    private readonly IRepository<ProductPicture> _productPictureRepository;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly INopLogger _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly MediaSettings _mediaSettings;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PictureService(
        IRepository<Picture> pictureRepository,
        IRepository<ProductPicture> productPictureRepository,
        ISettingService settingService,
        IWebHelper webHelper,
        INopLogger logger,
        IEventPublisher eventPublisher,
        MediaSettings mediaSettings,
        IWebHostEnvironment webHostEnvironment)
    {
        _pictureRepository = pictureRepository;
        _productPictureRepository = productPictureRepository;
        _settingService = settingService;
        _webHelper = webHelper;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _mediaSettings = mediaSettings;
        _webHostEnvironment = webHostEnvironment;
    }

    #region Utilities

    protected virtual (int width, int height) CalculateDimensions(
        int originalWidth, int originalHeight, int targetSize)
    {
        float width, height;

        if (originalHeight > originalWidth)
        {
            width = originalWidth * (targetSize / (float)originalHeight);
            height = targetSize;
        }
        else
        {
            width = targetSize;
            height = originalHeight * (targetSize / (float)originalWidth);
        }

        return (Math.Max(1, (int)Math.Round(width)), Math.Max(1, (int)Math.Round(height)));
    }

    protected virtual string GetFileExtensionFromMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return string.Empty;

        var lastPart = mimeType.Split('/')[^1];
        return lastPart switch
        {
            "pjpeg" => "jpg",
            "x-png" => "png",
            "x-icon" => "ico",
            _ => lastPart
        };
    }

    protected virtual string GetImagesLocalPath() =>
        Path.Combine(_webHostEnvironment.WebRootPath, "content", "images");

    protected virtual string GetPictureLocalPath(string fileName) =>
        Path.Combine(GetImagesLocalPath(), fileName);

    protected virtual string GetThumbsDirectoryPath() =>
        Path.Combine(GetImagesLocalPath(), "thumbs");

    protected virtual string GetThumbLocalPath(string thumbFileName)
    {
        var thumbsDir = GetThumbsDirectoryPath();

        if (_mediaSettings.MultipleThumbDirectories)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(thumbFileName);
            if (nameWithoutExt is { Length: > MultipleThumbDirectoriesLength })
            {
                var subDir = nameWithoutExt[..MultipleThumbDirectoriesLength];
                thumbsDir = Path.Combine(thumbsDir, subDir);
                IODirectory.CreateDirectory(thumbsDir);
            }
        }

        return Path.Combine(thumbsDir, thumbFileName);
    }

    protected virtual string GetThumbUrl(string thumbFileName, string? storeLocation = null)
    {
        storeLocation = !string.IsNullOrEmpty(storeLocation)
            ? storeLocation
            : _webHelper.GetStoreLocation();

        var url = storeLocation + "content/images/thumbs/";

        if (_mediaSettings.MultipleThumbDirectories)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(thumbFileName);
            if (nameWithoutExt is { Length: > MultipleThumbDirectoriesLength })
            {
                url = url + nameWithoutExt[..MultipleThumbDirectoriesLength] + "/";
            }
        }

        return url + thumbFileName;
    }

    protected virtual byte[] LoadPictureFromFile(int pictureId, string? mimeType)
    {
        var ext = GetFileExtensionFromMimeType(mimeType);
        var fileName = $"{pictureId:0000000}_0.{ext}";
        var filePath = GetPictureLocalPath(fileName);

        return File.Exists(filePath) ? File.ReadAllBytes(filePath) : [];
    }

    protected virtual void SavePictureInFile(int pictureId, byte[] pictureBinary, string? mimeType)
    {
        var ext = GetFileExtensionFromMimeType(mimeType);
        var fileName = $"{pictureId:0000000}_0.{ext}";
        File.WriteAllBytes(GetPictureLocalPath(fileName), pictureBinary);
    }

    protected virtual void DeletePictureOnFileSystem(Picture picture)
    {
        var ext = GetFileExtensionFromMimeType(picture.MimeType);
        var fileName = $"{picture.Id:0000000}_0.{ext}";
        var filePath = GetPictureLocalPath(fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    protected virtual void DeletePictureThumbs(Picture picture)
    {
        var filter = $"{picture.Id:0000000}*.*";
        var thumbsDir = GetThumbsDirectoryPath();

        if (!IODirectory.Exists(thumbsDir))
            return;

        foreach (var file in IODirectory.GetFiles(thumbsDir, filter, SearchOption.AllDirectories))
            File.Delete(file);
    }

    #endregion

    #region Methods

    public Task<byte[]> LoadPictureBinaryAsync(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        var result = StoreInDb
            ? picture.PictureBinary ?? []
            : LoadPictureFromFile(picture.Id, picture.MimeType);

        return Task.FromResult(result);
    }

    public string GetPictureSeName(string name) =>
        SeoExtensions.GetSeName(name, true, false);

    public async Task<string> GetDefaultPictureUrlAsync(
        int targetSize = 0,
        PictureType defaultPictureType = PictureType.Entity,
        string? storeLocation = null)
    {
        var defaultImageFileName = defaultPictureType switch
        {
            PictureType.Avatar => await _settingService.GetSettingByKeyAsync(
                "Media.Customer.DefaultAvatarImageName", "default-avatar.jpg"),
            _ => await _settingService.GetSettingByKeyAsync(
                "Media.DefaultImageName", "default-image.png")
        };

        var filePath = GetPictureLocalPath(defaultImageFileName);
        if (!File.Exists(filePath))
            return string.Empty;

        if (targetSize == 0)
        {
            storeLocation ??= _webHelper.GetStoreLocation();
            return storeLocation + "content/images/" + defaultImageFileName;
        }

        var fileExtension = Path.GetExtension(filePath);
        var thumbFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{targetSize}{fileExtension}";
        var thumbFilePath = GetThumbLocalPath(thumbFileName);

        if (!File.Exists(thumbFilePath))
        {
            using var image = Image.Load(filePath);
            var (w, h) = CalculateDimensions(image.Width, image.Height, targetSize);
            image.Mutate(x => x.Resize(w, h));
            await image.SaveAsync(thumbFilePath);
        }

        return GetThumbUrl(thumbFileName, storeLocation);
    }

    public async Task<string> GetPictureUrlAsync(
        int pictureId,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string? storeLocation = null,
        PictureType defaultPictureType = PictureType.Entity)
    {
        var picture = await GetPictureByIdAsync(pictureId);
        return await GetPictureUrlAsync(picture, targetSize, showDefaultPicture, storeLocation, defaultPictureType);
    }

    public async Task<string> GetPictureUrlAsync(
        Picture? picture,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string? storeLocation = null,
        PictureType defaultPictureType = PictureType.Entity)
    {
        var url = string.Empty;

        byte[]? pictureBinary = null;
        if (picture is not null)
            pictureBinary = await LoadPictureBinaryAsync(picture);

        if (picture is null || pictureBinary is null || pictureBinary.Length == 0)
        {
            if (showDefaultPicture)
                url = await GetDefaultPictureUrlAsync(targetSize, defaultPictureType, storeLocation);
            return url;
        }

        if (picture.IsNew)
        {
            DeletePictureThumbs(picture);
            picture = await UpdatePictureAsync(picture.Id, pictureBinary, picture.MimeType ?? string.Empty,
                picture.SeoFilename, picture.AltAttribute, picture.TitleAttribute, false, false);
            if (picture is null)
                return url;
        }

        var seoFileName = picture.SeoFilename;
        var ext = GetFileExtensionFromMimeType(picture.MimeType);

        string thumbFileName;
        if (targetSize == 0)
        {
            thumbFileName = !string.IsNullOrEmpty(seoFileName)
                ? $"{picture.Id:0000000}_{seoFileName}.{ext}"
                : $"{picture.Id:0000000}.{ext}";
        }
        else
        {
            thumbFileName = !string.IsNullOrEmpty(seoFileName)
                ? $"{picture.Id:0000000}_{seoFileName}_{targetSize}.{ext}"
                : $"{picture.Id:0000000}_{targetSize}.{ext}";
        }

        var thumbFilePath = GetThumbLocalPath(thumbFileName);

        if (!File.Exists(thumbFilePath))
        {
            if (targetSize != 0)
            {
                try
                {
                    using var image = Image.Load(pictureBinary);
                    var (w, h) = CalculateDimensions(image.Width, image.Height, targetSize);
                    image.Mutate(x => x.Resize(w, h));

                    IODirectory.CreateDirectory(Path.GetDirectoryName(thumbFilePath)!);
                    await image.SaveAsync(thumbFilePath);
                }
                catch (Exception exc)
                {
                    _logger.Error($"Error generating picture thumb. ID={picture.Id}", exc);
                    return url;
                }
            }
            else
            {
                IODirectory.CreateDirectory(Path.GetDirectoryName(thumbFilePath)!);
                await File.WriteAllBytesAsync(thumbFilePath, pictureBinary);
            }
        }

        url = GetThumbUrl(thumbFileName, storeLocation);
        return url;
    }

    public async Task<string> GetThumbLocalPathAsync(
        Picture picture, int targetSize = 0, bool showDefaultPicture = true)
    {
        var url = await GetPictureUrlAsync(picture, targetSize, showDefaultPicture);
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        return GetThumbLocalPath(Path.GetFileName(url));
    }

    public Task<Picture?> GetPictureByIdAsync(int pictureId)
    {
        return Task.FromResult(pictureId == 0 ? null : _pictureRepository.GetById(pictureId));
    }

    public async Task DeletePictureAsync(Picture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        DeletePictureThumbs(picture);

        if (!StoreInDb)
            DeletePictureOnFileSystem(picture);

        _pictureRepository.Delete(picture);

        await _eventPublisher.EntityDeletedAsync(picture);
    }

    public Task<IPagedList<Picture>> GetPicturesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _pictureRepository.Table.OrderByDescending(p => p.Id);
        IPagedList<Picture> result = new PagedList<Picture>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public Task<IList<Picture>> GetPicturesByProductIdAsync(int productId, int recordsToReturn = 0)
    {
        if (productId == 0)
            return Task.FromResult<IList<Picture>>(new List<Picture>());

        var query = from p in _pictureRepository.Table
                    join pp in _productPictureRepository.Table on p.Id equals pp.PictureId
                    where pp.ProductId == productId
                    orderby pp.DisplayOrder, pp.Id
                    select p;

        if (recordsToReturn > 0)
            query = query.Take(recordsToReturn);

        IList<Picture> result = query.ToList();
        return Task.FromResult(result);
    }

    public async Task<Picture> InsertPictureAsync(
        byte[] pictureBinary,
        string mimeType,
        string? seoFilename,
        string? altAttribute = null,
        string? titleAttribute = null,
        bool isNew = true,
        bool validateBinary = true)
    {
        mimeType = CommonHelper.EnsureMaximumLength(CommonHelper.EnsureNotNull(mimeType), 20);
        seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

        if (validateBinary)
            pictureBinary = await ValidatePictureAsync(pictureBinary, mimeType);

        var picture = new Picture
        {
            PictureBinary = StoreInDb ? pictureBinary : [],
            MimeType = mimeType,
            SeoFilename = seoFilename,
            AltAttribute = altAttribute,
            TitleAttribute = titleAttribute,
            IsNew = isNew,
        };
        _pictureRepository.Insert(picture);

        if (!StoreInDb)
            SavePictureInFile(picture.Id, pictureBinary, mimeType);

        await _eventPublisher.EntityInsertedAsync(picture);

        return picture;
    }

    public async Task<Picture?> UpdatePictureAsync(
        int pictureId,
        byte[] pictureBinary,
        string mimeType,
        string? seoFilename,
        string? altAttribute = null,
        string? titleAttribute = null,
        bool isNew = true,
        bool validateBinary = true)
    {
        mimeType = CommonHelper.EnsureMaximumLength(CommonHelper.EnsureNotNull(mimeType), 20);
        seoFilename = CommonHelper.EnsureMaximumLength(seoFilename, 100);

        if (validateBinary)
            pictureBinary = await ValidatePictureAsync(pictureBinary, mimeType);

        var picture = await GetPictureByIdAsync(pictureId);
        if (picture is null)
            return null;

        if (seoFilename != picture.SeoFilename)
            DeletePictureThumbs(picture);

        picture.PictureBinary = StoreInDb ? pictureBinary : [];
        picture.MimeType = mimeType;
        picture.SeoFilename = seoFilename;
        picture.AltAttribute = altAttribute;
        picture.TitleAttribute = titleAttribute;
        picture.IsNew = isNew;

        _pictureRepository.Update(picture);

        if (!StoreInDb)
            SavePictureInFile(picture.Id, pictureBinary, mimeType);

        await _eventPublisher.EntityUpdatedAsync(picture);

        return picture;
    }

    public async Task<Picture> SetSeoFilenameAsync(int pictureId, string seoFilename)
    {
        var picture = await GetPictureByIdAsync(pictureId)
            ?? throw new ArgumentException("No picture found with the specified id");

        if (seoFilename != picture.SeoFilename)
        {
            picture = await UpdatePictureAsync(picture.Id,
                await LoadPictureBinaryAsync(picture),
                picture.MimeType ?? string.Empty,
                seoFilename,
                picture.AltAttribute,
                picture.TitleAttribute,
                true, false) ?? picture;
        }

        return picture;
    }

    public Task<byte[]> ValidatePictureAsync(byte[] pictureBinary, string mimeType)
    {
        using var image = Image.Load(pictureBinary);
        var format = Image.DetectFormat(pictureBinary);

        var maxSize = _mediaSettings.MaximumImageSize;
        if (maxSize > 0 && (image.Width > maxSize || image.Height > maxSize))
        {
            var (w, h) = CalculateDimensions(image.Width, image.Height, maxSize);
            image.Mutate(x => x.Resize(w, h));
        }

        using var output = new MemoryStream();
        image.Save(output, format);
        return Task.FromResult(output.ToArray());
    }

    public bool StoreInDb
    {
        get => _settingService.GetSettingByKeyAsync("Media.Images.StoreInDB", true)
            .GetAwaiter().GetResult();
        set
        {
            if (StoreInDb == value)
                return;

            _settingService.SetSettingAsync("Media.Images.StoreInDB", value)
                .GetAwaiter().GetResult();

            var pageIndex = 0;
            const int pageSize = 400;

            while (true)
            {
                var pictures = GetPicturesAsync(pageIndex, pageSize)
                    .GetAwaiter().GetResult();
                pageIndex++;

                if (!pictures.Any())
                    break;

                foreach (var picture in pictures)
                {
                    var pictureBinary = value
                        ? LoadPictureFromFile(picture.Id, picture.MimeType)
                        : picture.PictureBinary ?? [];

                    if (value)
                        DeletePictureOnFileSystem(picture);
                    else
                        SavePictureInFile(picture.Id, pictureBinary, picture.MimeType);

                    picture.PictureBinary = value ? pictureBinary : [];
                    picture.IsNew = true;
                }

                _pictureRepository.Update(pictures);
            }
        }
    }

    #endregion
}
