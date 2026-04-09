using Nop.Core;
using Nop.Core.Domain.Media;

namespace Nop.Services.Media;

public interface IPictureService
{
    Task<byte[]> LoadPictureBinaryAsync(Picture picture);

    string GetPictureSeName(string name);

    Task<string> GetDefaultPictureUrlAsync(
        int targetSize = 0,
        PictureType defaultPictureType = PictureType.Entity,
        string? storeLocation = null);

    Task<string> GetPictureUrlAsync(
        int pictureId,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string? storeLocation = null,
        PictureType defaultPictureType = PictureType.Entity);

    Task<string> GetPictureUrlAsync(
        Picture? picture,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string? storeLocation = null,
        PictureType defaultPictureType = PictureType.Entity);

    Task<string> GetThumbLocalPathAsync(
        Picture picture,
        int targetSize = 0,
        bool showDefaultPicture = true);

    Task<Picture?> GetPictureByIdAsync(int pictureId);

    Task DeletePictureAsync(Picture picture);

    Task<IPagedList<Picture>> GetPicturesAsync(int pageIndex = 0, int pageSize = int.MaxValue);

    Task<IList<Picture>> GetPicturesByProductIdAsync(int productId, int recordsToReturn = 0);

    Task<Picture> InsertPictureAsync(
        byte[] pictureBinary,
        string mimeType,
        string? seoFilename,
        string? altAttribute = null,
        string? titleAttribute = null,
        bool isNew = true,
        bool validateBinary = true);

    Task<Picture?> UpdatePictureAsync(
        int pictureId,
        byte[] pictureBinary,
        string mimeType,
        string? seoFilename,
        string? altAttribute = null,
        string? titleAttribute = null,
        bool isNew = true,
        bool validateBinary = true);

    Task<Picture> SetSeoFilenameAsync(int pictureId, string seoFilename);

    Task<byte[]> ValidatePictureAsync(byte[] pictureBinary, string mimeType);

    bool StoreInDb { get; set; }
}
