using System;
using System.IO;
using NUnit.Framework;
using Nop.Core;
using SixLabors.ImageSharp;

namespace Nop.Admin.Tests
{
    /// <summary>
    /// Deliberately-failing assertions, one per mechanism the rest of this project relies on.
    /// </summary>
    /// <remarks>
    /// A green suite is only evidence if it has been shown to go red. This migration has twice
    /// been bitten by silently-swallowed failure (the Six Labors licence task failing under
    /// <c>ContinueOnError=true</c>, and Roslyn hiding a real <c>CS1929</c> across three completed
    /// tasks), and task 7.7 found two deferrals that had been marked RESOLVED without ever being
    /// executed. This fixture is the same guard <c>Nop.Web.SmokeTests.HarnessCanaryTests</c>
    /// provides there.
    ///
    /// <para><b>ALL FOUR MUST REPORT Failed:</b></para>
    /// <code>
    /// dotnet test src/Tests/Nop.Admin.Tests/Nop.Admin.Tests.csproj \
    ///   --filter "FullyQualifiedName~HarnessCanaryTests"
    /// </code>
    /// If any PASSES, the corresponding group of real assertions cannot be trusted.
    /// </remarks>
    [TestFixture]
    [Explicit("Deliberately-failing canaries. Run explicitly; all four must FAIL.")]
    public class HarnessCanaryTests
    {
        /// <summary>
        /// Proves the controller harness really captures response bytes. If
        /// <c>DefaultHttpContext.Response.Body</c> were left as <c>Stream.Null</c> every imaging
        /// assertion in <see cref="RoxyFilemanImagingTests"/> would be vacuous.
        /// </summary>
        [Test]
        public void CANARY_the_thumbnail_harness_really_captures_written_bytes()
        {
            var originalBase = CommonHelper.BaseDirectory;
            var root = RoxyFilemanTestContentRoot.Create();
            try
            {
                CommonHelper.BaseDirectory = root;
                var virtualPath = RoxyFilemanTestContentRoot.WriteImage(root, "canary.png", 200, 100);

                var controller = new TestableRoxyFilemanController();
                controller.CallShowThumbnail(virtualPath, 140, 120);

                using (var img = Image.Load(controller.WrittenBytes))
                {
                    //deliberately wrong - the real answer is 140x100
                    Assert.AreEqual(999, img.Width, "CANARY: expected to fail");
                }
            }
            finally
            {
                CommonHelper.BaseDirectory = originalBase;
                RoxyFilemanTestContentRoot.Delete(root);
            }
        }

        /// <summary>
        /// Proves the <c>MapPath</c> assertions are comparing against a real resolution and not
        /// against themselves.
        /// </summary>
        [Test]
        public void CANARY_MapPath_assertions_can_fail()
        {
            var originalBase = CommonHelper.BaseDirectory;
            try
            {
                CommonHelper.BaseDirectory = Path.Combine(Path.GetTempPath(), "nopadmin-8_6-canary");
                var controller = new TestableRoxyFilemanController();

                //deliberately wrong - relative paths resolve under Administration/Content/Roxy_Fileman
                Assert.AreEqual(Path.Combine(CommonHelper.BaseDirectory, "no", "such", "place"),
                    controller.CallMapPath("../lang/en.json"), "CANARY: expected to fail");
            }
            finally
            {
                CommonHelper.BaseDirectory = originalBase;
            }
        }

        /// <summary>
        /// Proves the colour-parity assertions discriminate, rather than passing for anything.
        /// </summary>
        [Test]
        public void CANARY_colour_parity_assertions_can_fail()
        {
            //deliberately wrong - "notacolour" does not parse, so ValidateHtmlColor returns a message
            Assert.IsNull(Nop.Admin.Helpers.HtmlColorHelper.ValidateHtmlColor("notacolour"),
                "CANARY: expected to fail");
        }

        /// <summary>
        /// Proves the AssemblyRef read really inspects the built assembly.
        /// </summary>
        [Test]
        public void CANARY_the_assembly_reference_scan_can_fail()
        {
            var assemblyPath = typeof(global::Nop.Admin.Controllers.RoxyFilemanController).Assembly.Location;
            using (var fs = File.OpenRead(assemblyPath))
            using (var pe = new System.Reflection.PortableExecutable.PEReader(fs))
            {
                var md = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
                var count = 0;
                foreach (var h in md.AssemblyReferences)
                {
                    //a name that is certainly NOT referenced
                    if (md.GetString(md.GetAssemblyReference(h).Name) == "Nop.Admin.DoesNotExist")
                        count++;
                }
                Assert.AreEqual(1, count, "CANARY: expected to fail");
            }
        }
    }
}
