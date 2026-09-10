using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Services.Catalog;

namespace Nop.Services.Media
{
    /// <summary>
    /// Extensions
    /// </summary>
    /// <remarks>
    /// Task 4.2 (design section 5): <c>System.Web.HttpPostedFileBase</c> -&gt;
    /// <see cref="IFormFile"/>. <c>InputStream</c> becomes
    /// <see cref="IFormFile.OpenReadStream"/> and <c>ContentLength</c> (int) becomes
    /// <see cref="IFormFile.Length"/> (long). The original single <c>Read</c> call was also
    /// replaced by <see cref="Stream.CopyTo(Stream)"/> because a single <c>Read</c> is not
    /// guaranteed to fill the buffer on a non-buffered ASP.NET Core request stream.
    /// </remarks>
    public static class Extensions
    {
        /// <summary>
        /// Gets the download binary array
        /// </summary>
        /// <param name="postedFile">Posted file</param>
        /// <returns>Download binary array</returns>
        public static byte[] GetDownloadBits(this IFormFile postedFile)
        {
            return ReadAllBytes(postedFile);
        }

        /// <summary>
        /// Gets the picture binary array
        /// </summary>
        /// <param name="postedFile">Posted file</param>
        /// <returns>Picture binary array</returns>
        public static byte[] GetPictureBits(this IFormFile postedFile)
        {
            return ReadAllBytes(postedFile);
        }

        /// <summary>
        /// Reads a posted file in full
        /// </summary>
        /// <param name="postedFile">Posted file</param>
        /// <returns>File binary</returns>
        private static byte[] ReadAllBytes(IFormFile postedFile)
        {
            if (postedFile == null)
                throw new ArgumentNullException("postedFile");

            using (var stream = postedFile.OpenReadStream())
            using (var destination = new MemoryStream())
            {
                stream.CopyTo(destination);
                return destination.ToArray();
            }
        }

        /// <summary>
        /// Get product picture (for shopping cart and order details pages)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Atributes (in XML format)</param>
        /// <param name="pictureService">Picture service</param>
        /// <param name="productAttributeParser">Product attribute service</param>
        /// <returns>Picture</returns>
        public static Picture GetProductPicture(this Product product, string attributesXml,
            IPictureService pictureService,
            IProductAttributeParser productAttributeParser)
        {
            if (product == null)
                throw new ArgumentNullException("product");
            if (pictureService == null)
                throw new ArgumentNullException("pictureService");
            if (productAttributeParser == null)
                throw new ArgumentNullException("productAttributeParser");

            Picture picture = null;

            //first, let's see whether we have some attribute values with custom pictures
            var attributeValues = productAttributeParser.ParseProductAttributeValues(attributesXml);
            foreach (var attributeValue in attributeValues)
            {
                var attributePicture = pictureService.GetPictureById(attributeValue.PictureId);
                if (attributePicture != null)
                {
                    picture = attributePicture;
                    break;
                }
            }

            //now let's load the default product picture
            if (picture == null)
            {
                picture = pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
            }

            //let's check whether this product has some parent "grouped" product
            if (picture == null && !product.VisibleIndividually && product.ParentGroupedProductId > 0)
            {
                picture = pictureService.GetPicturesByProductId(product.ParentGroupedProductId, 1).FirstOrDefault();
            }

            return picture;
        }
    }
}
