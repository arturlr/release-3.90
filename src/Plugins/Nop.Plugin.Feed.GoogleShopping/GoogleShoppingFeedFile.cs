using System.IO;
using Nop.Core;

namespace Nop.Plugin.Feed.GoogleShopping
{
    /// <summary>
    /// Where a generated Google Shopping feed file lives, and the URL it is served at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 11.2 — ONE 3.90 DEFECT FIXED, AND ONE PIECE OF DUPLICATION REMOVED SO IT CANNOT
    /// COME BACK.</b>
    /// </para>
    /// <para>
    /// 3.90 built this path in <b>two</b> places with the same expression —
    /// <c>GoogleShoppingService.GenerateStaticFile</c> (the WRITE) and
    /// <c>FeedGoogleShoppingController.Configure</c> (the <c>File.Exists</c> PROBE and the URL
    /// shown to the administrator) — and the expression was:
    /// </para>
    /// <code>
    /// Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport",
    ///              store.Id + "-" + _googleShoppingSettings.StaticFileName)
    /// </code>
    /// <para>
    /// <b>Two things are wrong with it on net10.0, and both are silent.</b>
    /// </para>
    /// <list type="number">
    /// <item><c>System.Web.HttpRuntime.AppDomainAppPath</c> does not exist. Replaced by
    /// <c>CommonHelper.MapPath("~/…")</c>, which task 2.4 re-based on
    /// <c>CommonHelper.BaseDirectory</c> — pointed at the host's content root by
    /// <c>UseNopHostingEnvironment</c> (runtime deferral 1.5) — and which normalises separators
    /// platform-neutrally.</item>
    /// <item><b><c>"content\\files\\exportimport"</c> IS A SINGLE DIRECTORY NAME ON LINUX.</b>
    /// <c>Path.Combine</c> does not translate <c>\</c>, so the write would have targeted a
    /// directory literally called <c>content\files\exportimport</c>, which does not exist —
    /// <c>DirectoryNotFoundException</c> on every "Generate feed" — and the <c>File.Exists</c>
    /// probe would have silently returned false forever, so the administrator would never see a
    /// feed URL even after a successful generation. The <b>casing</b> is wrong too: the directory
    /// on disk is <c>Content/files/ExportImport</c>, and <c>CommonHelper.MapPath</c> resolves
    /// against a case-sensitive filesystem. This is the same defect and the same fix task 7.7
    /// applied to <c>PdfService.PrintOrderToPdf</c> and task 8.x to
    /// <c>Nop.Admin</c>'s <c>CommonController</c>.</item>
    /// </list>
    /// <para>
    /// <b>AND THE URL HAD THE SAME CASING BUG, which the physical fix alone does not cure.</b> The
    /// controller emitted <c>{store}content/files/exportimport/{id}-{name}.xml</c>. Task 7.4's
    /// <c>NopStaticFileProvider</c> decides ALLOW/DENY case-insensitively — its comment explicitly
    /// keeps <c>~/Content/files/ExportImport</c> served because "narrowing it here would break that
    /// feed URL" — but the actual lookup is done by <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider"/>,
    /// which <b>is</b> case-sensitive on Linux. So the URL would have been allowed and then 404'd.
    /// <see cref="RelativeDirectory"/> is therefore used for the URL as well as the path, which is
    /// the only way the two cannot drift apart again.
    /// </para>
    /// <para>
    /// <b>The generated files are deliberately NOT in a publish</b> — deferral 39's publish shaping
    /// excludes them, while the directory itself stays present (<c>Content/files/Index.htm</c>).
    /// That is unchanged by this class.
    /// </para>
    /// </remarks>
    public static class GoogleShoppingFeedFile
    {
        /// <summary>
        /// The feed directory relative to the application root, spelled to match the directory on
        /// disk EXACTLY and with forward slashes. Used for both the physical path and the URL.
        /// </summary>
        public const string RelativeDirectory = "Content/files/ExportImport";

        /// <summary>
        /// The file name a store's feed is written under: <c>{storeId}-{StaticFileName}</c>, 3.90's
        /// convention unchanged. <c>StaticFileName</c> is seeded at install as
        /// <c>googleshopping_{10 random digits}.xml</c> so the URL is not guessable.
        /// </summary>
        public static string FileName(int storeId, string staticFileName)
        {
            return storeId + "-" + staticFileName;
        }

        /// <summary>
        /// The absolute path a store's feed file is written to and probed at.
        /// </summary>
        public static string PhysicalPath(int storeId, string staticFileName)
        {
            return Path.Combine(CommonHelper.MapPath("~/" + RelativeDirectory),
                FileName(storeId, staticFileName));
        }

        /// <summary>
        /// The absolute URL the feed file is served at, for the administrator to hand to Google
        /// Merchant Center.
        /// </summary>
        /// <param name="webHelper">Web helper</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="staticFileName">The configured static file name</param>
        /// <remarks>
        /// <c>GetStoreLocation(false)</c> — i.e. do not force https — is 3.90's argument, kept:
        /// the URL is copied into an external service's configuration and forcing a scheme the
        /// store may not serve would break it.
        /// </remarks>
        public static string Url(IWebHelper webHelper, int storeId, string staticFileName)
        {
            return string.Format("{0}{1}/{2}", webHelper.GetStoreLocation(false), RelativeDirectory,
                FileName(storeId, staticFileName));
        }
    }
}
