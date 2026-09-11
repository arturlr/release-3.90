using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Admin.Controllers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nop.Admin.Tests
{
    /// <summary>
    /// Exposes the <c>protected virtual</c> members of the real
    /// <see cref="RoxyFilemanController"/> that task 8.6 ported, and captures what it writes to
    /// the response.
    /// </summary>
    /// <remarks>
    /// Nothing is overridden and nothing is stubbed - the bodies under test are the shipping
    /// ones. <c>IPermissionService</c> is null because none of these members reads it (the
    /// permission check is in <c>ProcessRequest</c>, which is not exercised here).
    /// </remarks>
    internal sealed class TestableRoxyFilemanController : RoxyFilemanController
    {
        public TestableRoxyFilemanController()
            : base(null)
        {
            ResponseBody = new MemoryStream();
            var httpContext = new DefaultHttpContext();
            //DefaultHttpContext's response body is Stream.Null, which silently discards writes -
            //that would make every imaging assertion below vacuous.
            httpContext.Response.Body = ResponseBody;
            ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        public MemoryStream ResponseBody { get; private set; }

        public byte[] WrittenBytes
        {
            get { return ResponseBody.ToArray(); }
        }

        public string WrittenContentType
        {
            get { return Response.Headers["Content-Type"]; }
        }

        public string CallMapPath(string path)
        {
            return MapPath(path);
        }

        public void CallShowThumbnail(string path, int width, int height)
        {
            ShowThumbnail(path, width, height);
        }

        public void CallImageResize(string path, string dest, int width, int height)
        {
            ImageResize(path, dest, width, height);
        }
    }

    /// <summary>
    /// Builds a throwaway content root that mirrors the parts of the Nop.Web application tree
    /// the Roxy Fileman controller reads, so the tests exercise the real path arithmetic
    /// (<c>conf.json</c> lookup, <c>FILES_ROOT</c>, <c>CheckPath</c>) instead of bypassing it.
    /// </summary>
    internal static class RoxyFilemanTestContentRoot
    {
        public const string FilesRootVirtualPath = "/Content/Images/uploaded";

        /// <summary>The Roxy Fileman installation directory, relative to the content root.</summary>
        public static readonly string RoxyRootRelative =
            Path.Combine("Administration", "Content", "Roxy_Fileman");

        public static string Create()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "nopadmin-8_6-" + Guid.NewGuid().ToString("N"));

            var roxyRoot = Path.Combine(root, RoxyRootRelative);
            Directory.CreateDirectory(roxyRoot);
            Directory.CreateDirectory(Path.Combine(roxyRoot, "lang"));
            Directory.CreateDirectory(Path.Combine(root, "Content", "Images", "uploaded"));

            //the two settings the controller actually reads on these paths
            File.WriteAllText(Path.Combine(roxyRoot, "conf.json"),
                "{\n\"FILES_ROOT\": \"~" + FilesRootVirtualPath + "\",\n" +
                "\"SESSION_PATH_KEY\": \"\",\n" +
                "\"MAX_IMAGE_WIDTH\": \"1000\",\n" +
                "\"MAX_IMAGE_HEIGHT\": \"1000\",\n" +
                "\"LANG\": \"en\",\n" +
                "\"FORBIDDEN_UPLOADS\": \"exe\",\n" +
                "\"ALLOWED_UPLOADS\": \"\"\n}");

            File.WriteAllText(Path.Combine(roxyRoot, "lang", "en.json"),
                "{\n\"E_UploadNotAll\": \"Not all files were uploaded\"\n}");

            return root;
        }

        public static void Delete(string root)
        {
            try
            {
                if (root != null && Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            catch (IOException)
            {
                //a leftover temp directory is not worth failing a test run over
            }
        }

        /// <summary>
        /// Writes a test image under FILES_ROOT and returns the virtual path the controller
        /// expects (i.e. one that <c>FixPath</c>/<c>CheckPath</c> will accept).
        /// </summary>
        public static string WriteImage(string root, string fileName, int width, int height,
            bool asJpeg = false, int blueStripeWidth = 0)
        {
            var physical = Path.Combine(root, "Content", "Images", "uploaded", fileName);
            using (var img = new Image<Rgba32>(width, height))
            {
                var red = new Rgba32(255, 0, 0);
                var blue = new Rgba32(0, 0, 255);
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        img[x, y] = x < blueStripeWidth ? blue : red;

                if (asJpeg)
                    img.Save(physical, new JpegEncoder { Quality = 90 });
                else
                    img.Save(physical, new PngEncoder());
            }
            return FilesRootVirtualPath + "/" + fileName;
        }

        public static string Physical(string root, string fileName)
        {
            return Path.Combine(root, "Content", "Images", "uploaded", fileName);
        }
    }
}
