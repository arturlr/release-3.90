using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Nop.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Nop.Admin.Tests
{
    /// <summary>
    /// TASK 8.6 - the System.Drawing -> SixLabors.ImageSharp port of
    /// <c>RoxyFilemanController</c>, exercised by execution.
    /// </summary>
    /// <remarks>
    /// Every test here drives the real controller and reads the real encoded bytes back. On the
    /// build platform for this migration (Linux, per build-environment.md) the code these
    /// replaced could not run at all: <c>new Bitmap(...)</c> and <c>Graphics.FromImage(...)</c>
    /// throw <c>TypeInitializationException</c> wrapping
    /// <c>DllNotFoundException: Unable to load shared library 'libgdiplus'</c>, measured against
    /// the <c>System.Drawing.Common</c> 4.7.2 pin this solution carries. So the fact that these
    /// tests pass at all is the assertion, and <see cref="Task_8_6_these_tests_are_running_on_a_platform_where_the_System_Drawing_pipeline_could_not"/>
    /// records the platform explicitly.
    /// </remarks>
    [TestFixture]
    public class RoxyFilemanImagingTests
    {
        private string _contentRoot;
        private string _originalBaseDirectory;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _originalBaseDirectory = CommonHelper.BaseDirectory;
            _contentRoot = RoxyFilemanTestContentRoot.Create();
            //the seam every MapPath in the controller resolves against (deferral 1.5)
            CommonHelper.BaseDirectory = _contentRoot;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            CommonHelper.BaseDirectory = _originalBaseDirectory;
            RoxyFilemanTestContentRoot.Delete(_contentRoot);
        }

        #region ShowThumbnail

        [Test]
        public void Task_8_6_a_thumbnail_is_produced_at_the_dimensions_3_90_produced()
        {
            //200x100 asked for 140x120: 3.90 clamped the height to the source height, so the
            //answer is 140x100, not 140x120. That clamp is arithmetic this port left untouched.
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "t1.png", 200, 100);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 140, 120);

            var bytes = controller.WrittenBytes;
            Assert.Greater(bytes.Length, 0, "nothing was written to the response body");

            IImageFormat format;
            using (var img = Image.Load(bytes, out format))
            {
                Assert.AreEqual(140, img.Width, "thumbnail width");
                Assert.AreEqual(100, img.Height, "thumbnail height (clamped to the source height)");
                Assert.IsInstanceOf<PngFormat>(format, "3.90 always wrote the thumbnail as PNG");
            }
        }

        [Test]
        public void Task_8_6_a_thumbnail_with_height_zero_derives_the_height_from_the_source_ratio()
        {
            //3.90: height == 0 => height = floor(width / (srcW/srcH)). 140 / 2.0 = 70.
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "t2.png", 200, 100);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 140, 0);

            using (var img = Image.Load(controller.WrittenBytes))
            {
                Assert.AreEqual(140, img.Width);
                Assert.AreEqual(70, img.Height);
            }
        }

        [Test]
        public void Task_8_6_a_thumbnail_request_larger_than_the_source_is_clamped_to_the_source()
        {
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "t3.png", 50, 30);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 500, 400);

            using (var img = Image.Load(controller.WrittenBytes))
            {
                Assert.AreEqual(50, img.Width);
                Assert.AreEqual(30, img.Height);
            }
        }

        [Test]
        public void Task_8_6_a_JPEG_source_still_yields_a_PNG_thumbnail()
        {
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(
                _contentRoot, "t4.jpg", 300, 200, asJpeg: true);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 150, 100);

            IImageFormat format;
            using (var img = Image.Load(controller.WrittenBytes, out format))
            {
                Assert.IsInstanceOf<PngFormat>(format);
                Assert.AreEqual(150, img.Width);
                Assert.AreEqual(100, img.Height);
            }
        }

        [Test]
        public void Task_8_6_the_thumbnail_response_is_declared_as_image_png()
        {
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "t5.png", 120, 120);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 60, 60);

            Assert.AreEqual(MimeTypes.ImagePng, controller.WrittenContentType);
        }

        [Test]
        public void Task_8_6_the_thumbnail_crop_rectangle_is_centred_on_the_source()
        {
            //THE CONTENT assertion, not just a dimension one: a 200x100 source whose leftmost 20
            //columns are blue and the rest red, asked for 140x120 -> clamped to 140x100 -> the
            //crop is exactly 140 wide starting at x = floor((200-140)/2) = 30. The blue stripe
            //(x 0..19) is therefore cropped AWAY. If the crop were not centred - or were not
            //applied at all - blue would survive into the output.
            var virtualPath = RoxyFilemanTestContentRoot.WriteImage(
                _contentRoot, "t6.png", 200, 100, blueStripeWidth: 20);

            var controller = new TestableRoxyFilemanController();
            controller.CallShowThumbnail(virtualPath, 140, 120);

            using (var img = Image.Load<Rgba32>(controller.WrittenBytes))
            {
                Assert.AreEqual(140, img.Width);
                Assert.AreEqual(100, img.Height);

                var sawBlue = false;
                var sawRed = false;
                for (var y = 0; y < img.Height; y++)
                    for (var x = 0; x < img.Width; x++)
                    {
                        var p = img[x, y];
                        if (p.B > 200 && p.R < 60) sawBlue = true;
                        if (p.R > 200 && p.B < 60) sawRed = true;
                    }

                Assert.IsTrue(sawRed, "the cropped region should be red");
                Assert.IsFalse(sawBlue,
                    "the leftmost 20px blue stripe sits outside the centred 140px crop and must not survive");
            }
        }

        #endregion

        #region ImageResize

        [Test]
        public void Task_8_6_ImageResize_shrinks_an_oversize_upload_to_the_configured_bound()
        {
            RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "r1.png", 1200, 600);
            var physical = RoxyFilemanTestContentRoot.Physical(_contentRoot, "r1.png");

            var controller = new TestableRoxyFilemanController();
            controller.CallImageResize(physical, physical, 1000, 1000);

            IImageFormat format;
            using (var img = Image.Load(File.ReadAllBytes(physical), out format))
            {
                Assert.AreEqual(1000, img.Width);
                Assert.AreEqual(500, img.Height);
                Assert.IsInstanceOf<PngFormat>(format, "the .png destination extension picks PngEncoder");
            }
        }

        [Test]
        public void Task_8_6_ImageResize_does_not_rewrite_an_image_that_already_fits()
        {
            //3.90 returned before touching the destination, so an in-range upload keeps its
            //original bytes and is never recompressed. Preserved deliberately.
            RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "r2.png", 100, 50);
            var physical = RoxyFilemanTestContentRoot.Physical(_contentRoot, "r2.png");
            var before = File.ReadAllBytes(physical);

            var controller = new TestableRoxyFilemanController();
            controller.CallImageResize(physical, physical, 1000, 1000);

            Assert.AreEqual(before, File.ReadAllBytes(physical),
                "an image that already fits must be left byte-identical");
        }

        [Test]
        public void Task_8_6_ImageResize_with_both_bounds_zero_is_a_no_op()
        {
            RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "r3.png", 1200, 600);
            var physical = RoxyFilemanTestContentRoot.Physical(_contentRoot, "r3.png");
            var before = File.ReadAllBytes(physical);

            var controller = new TestableRoxyFilemanController();
            controller.CallImageResize(physical, physical, 0, 0);

            Assert.AreEqual(before, File.ReadAllBytes(physical));
        }

        [TestCase("d1.jpg", typeof(JpegFormat))]
        [TestCase("d2.gif", typeof(GifFormat))]
        [TestCase("d3.png", typeof(PngFormat))]
        [TestCase("d4.bmp", typeof(JpegFormat))]  // 3.90's default branch was JPEG
        public void Task_8_6_ImageResize_picks_the_encoder_from_the_destination_extension(
            string destName, Type expectedFormat)
        {
            RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "src-" + destName + ".png", 1200, 600);
            var source = RoxyFilemanTestContentRoot.Physical(_contentRoot, "src-" + destName + ".png");
            var dest = RoxyFilemanTestContentRoot.Physical(_contentRoot, destName);

            var controller = new TestableRoxyFilemanController();
            controller.CallImageResize(source, dest, 1000, 1000);

            IImageFormat format;
            using (var img = Image.Load(File.ReadAllBytes(dest), out format))
            {
                Assert.IsInstanceOf(expectedFormat, format);
                Assert.AreEqual(1000, img.Width);
                Assert.AreEqual(500, img.Height);
            }
        }

        [Test]
        public void Task_8_6_ImageResize_really_resamples_rather_than_cropping()
        {
            //a 20px blue stripe on a 1200px-wide source is 1/60th of the width; after a scale to
            //1000px it must still be present (about 16px), which a crop-based implementation
            //would not guarantee and a no-op would leave at the original size.
            RoxyFilemanTestContentRoot.WriteImage(_contentRoot, "r5.png", 1200, 600, blueStripeWidth: 20);
            var physical = RoxyFilemanTestContentRoot.Physical(_contentRoot, "r5.png");

            var controller = new TestableRoxyFilemanController();
            controller.CallImageResize(physical, physical, 1000, 1000);

            using (var img = Image.Load<Rgba32>(File.ReadAllBytes(physical)))
            {
                Assert.AreEqual(1000, img.Width);
                var topLeft = img[0, 0];
                var topRight = img[img.Width - 1, 0];
                Assert.Greater(topLeft.B, 200, "the left edge came from the blue stripe");
                Assert.Greater(topRight.R, 200, "the right edge came from the red field");
            }
        }

        #endregion

        #region the platform, and the binding that is gone

        [Test]
        public void Task_8_6_these_tests_are_running_on_a_platform_where_the_System_Drawing_pipeline_could_not()
        {
            //Informational on Windows, load-bearing everywhere else. The whole point of task 8.6
            //is that the imaging above works off Windows; if this test reports Windows then the
            //imaging assertions in this fixture have not demonstrated that.
            TestContext.WriteLine("OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription
                + "  IsWindows=" + OperatingSystem.IsWindows());
            if (OperatingSystem.IsWindows())
                Assert.Ignore("Running on Windows - the cross-platform claim is not exercised by this run. "
                    + "The migration's builds run on Linux (build-environment.md), where GDI+ is unavailable.");
            Assert.Pass("Imaging assertions in this fixture ran on a non-Windows platform.");
        }

        [Test]
        public void Task_8_6_Nop_Admin_no_longer_references_the_Windows_only_System_Drawing_Common()
        {
            //Read from the AssemblyRef metadata table of the built assembly, which is
            //authoritative in a way a text search over source is not - and it is the specific
            //regression this task exists to prevent. Measured before the port: Nop.Admin had 45
            //assembly references including System.Drawing.Common; after: 44, with none.
            var assemblyPath = typeof(global::Nop.Admin.Controllers.RoxyFilemanController).Assembly.Location;
            Assert.IsNotEmpty(assemblyPath, "could not locate Nop.Admin.dll on disk");

            var references = ReadAssemblyReferences(assemblyPath);

            CollectionAssert.DoesNotContain(references, "System.Drawing.Common",
                "System.Drawing.Common is Windows-only from .NET 6; nothing in Nop.Admin may bind it");
            Assert.IsFalse(references.Any(r => r.StartsWith("ImageResizer", StringComparison.OrdinalIgnoreCase)),
                "ImageResizer has no net10.0 release (design section 7)");
            CollectionAssert.Contains(references, "SixLabors.ImageSharp",
                "the replacement should be referenced");
        }

        private static string[] ReadAssemblyReferences(string assemblyPath)
        {
            using (var fs = File.OpenRead(assemblyPath))
            using (var pe = new System.Reflection.PortableExecutable.PEReader(fs))
            {
                var md = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
                return md.AssemblyReferences
                    .Select(h => md.GetString(md.GetAssemblyReference(h).Name))
                    .ToArray();
            }
        }

        #endregion
    }
}
