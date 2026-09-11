using System;
using System.IO;
using NUnit.Framework;
using Nop.Core;

namespace Nop.Admin.Tests
{
    /// <summary>
    /// TASK 8.6 - deferrals 8.1-3 and 8.3-3: <c>Server.MapPath</c> with a RELATIVE path.
    /// </summary>
    /// <remarks>
    /// Task 8.3 made <c>RoxyFilemanController.MapPath</c> throw <c>NopException</c> naming
    /// deferral 8.1-3 for a relative path rather than guess a translation, and recorded that 8.6
    /// must establish the <i>intended</i> target instead of mechanically translating the
    /// <c>..</c>. It is the Roxy Fileman installation directory,
    /// <c>~/Administration/Content/Roxy_Fileman/</c>; the evidence is in the method's own
    /// remarks. These tests pin that resolution, and pin that the throw is gone.
    /// </remarks>
    [TestFixture]
    public class RoxyFilemanMapPathTests
    {
        private string _originalBaseDirectory;

        [SetUp]
        public void SetUp()
        {
            _originalBaseDirectory = CommonHelper.BaseDirectory;
        }

        [TearDown]
        public void TearDown()
        {
            CommonHelper.BaseDirectory = _originalBaseDirectory;
        }

        private static string FakeRoot
        {
            get { return Path.Combine(Path.GetTempPath(), "nopadmin-8_6-mappath-root"); }
        }

        [Test]
        public void Task_8_6_deferral_8_3_3_a_relative_path_no_longer_throws()
        {
            CommonHelper.BaseDirectory = FakeRoot;
            var controller = new TestableRoxyFilemanController();

            //task 8.3's deliberate NopException is gone
            Assert.DoesNotThrow(() => controller.CallMapPath("../lang/en.json"));
            Assert.DoesNotThrow(() => controller.CallMapPath("../tmp/x.zip"));
            Assert.DoesNotThrow(() => controller.CallMapPath("../Uploads"));
        }

        [TestCase("../lang/en.json", "lang", "en.json")]
        [TestCase("../tmp/somedir.zip", "tmp", "somedir.zip")]
        public void Task_8_6_deferral_8_1_3_a_relative_path_resolves_inside_the_Roxy_Fileman_directory(
            string relative, string expectedDir, string expectedFile)
        {
            CommonHelper.BaseDirectory = FakeRoot;
            var controller = new TestableRoxyFilemanController();

            var resolved = controller.CallMapPath(relative);

            var expected = Path.GetFullPath(Path.Combine(FakeRoot,
                "Administration", "Content", "Roxy_Fileman", expectedDir, expectedFile));
            Assert.AreEqual(expected, resolved);
        }

        [Test]
        public void Task_8_6_a_resolved_relative_path_has_no_dot_dot_segments_left_in_it()
        {
            //CommonHelper.MapPath does not normalise "..", and CheckPath compares paths by
            //prefix - an unnormalised result would be a poor thing to hand it.
            CommonHelper.BaseDirectory = FakeRoot;
            var controller = new TestableRoxyFilemanController();

            var resolved = controller.CallMapPath("../lang/en.json");

            Assert.IsFalse(resolved.Contains(".." + Path.DirectorySeparatorChar),
                "the '..' segment should have been normalised away: " + resolved);
        }

        [Test]
        public void Task_8_6_a_tilde_rooted_path_still_resolves_exactly_as_CommonHelper_does()
        {
            CommonHelper.BaseDirectory = FakeRoot;
            var controller = new TestableRoxyFilemanController();

            Assert.AreEqual(CommonHelper.MapPath("~/Content/Images/uploaded/x.png"),
                controller.CallMapPath("~/Content/Images/uploaded/x.png"));
        }

        [Test]
        public void Task_8_6_a_relative_path_cannot_traverse_out_of_the_Roxy_Fileman_directory()
        {
            //not reachable by any in-tree caller - every relative literal is a compile-time
            //constant or comes from conf.json - but cheaper to be safe by construction than to
            //argue about it.
            CommonHelper.BaseDirectory = FakeRoot;
            var controller = new TestableRoxyFilemanController();

            Assert.Throws<Nop.Core.NopException>(
                () => controller.CallMapPath("lang/../../../../../../etc/passwd"));
        }

        [Test]
        public void Task_8_6_deferral_8_1_3_the_intended_target_is_evidenced_by_the_real_repository_tree()
        {
            //THE assertion that establishes the target rather than asserting my arithmetic
            //against itself: pointed at the real Nop.Web content root, "../lang/en.json" must
            //land on a file that EXISTS. Under System.Web the same call resolved to
            //<root>/Admin/lang/en.json - which does not exist - so 3.90's Roxy Fileman error
            //messages were resource keys rather than sentences. That is recorded as a
            //pre-existing 3.90 defect, deliberately not reproduced.
            var contentRoot = FindNopWebContentRoot();
            if (contentRoot == null)
                Assert.Ignore("could not locate the Nop.Web content root from the test output directory");

            CommonHelper.BaseDirectory = contentRoot;
            var controller = new TestableRoxyFilemanController();

            var langFile = controller.CallMapPath("../lang/en.json");
            Assert.IsTrue(File.Exists(langFile),
                "the intended relative-path base must contain the Roxy Fileman language files; resolved to " + langFile);

            var confFile = CommonHelper.MapPath("~/Administration/Content/Roxy_Fileman/conf.json");
            Assert.AreEqual(Path.GetDirectoryName(confFile), Path.GetDirectoryName(Path.GetDirectoryName(langFile)),
                "relative paths must resolve against the same directory that holds conf.json");
        }

        /// <summary>
        /// Walks up from the test output directory looking for the Nop.Web content root, so the
        /// test does not hard-code an absolute path.
        /// </summary>
        private static string FindNopWebContentRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "src", "Presentation", "Nop.Web");
                if (Directory.Exists(Path.Combine(candidate, "Administration", "Content", "Roxy_Fileman")))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
