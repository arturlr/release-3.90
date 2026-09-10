using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// An allow-listed <see cref="IFileProvider"/> over the <b>content root</b>, which is where
    /// 3.90 kept its static assets and where they remain (task 7.4, runtime deferrals 40/7.1-5
    /// and 33/14.33).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The problem.</b> In 3.90 <c>System.Web</c>'s handler pipeline served
    /// <c>~/Content</c>, <c>~/Scripts</c>, <c>~/Themes/&lt;theme&gt;/Content</c> and
    /// <c>~/favicon.ico</c> straight out of the application root; there was no <c>wwwroot</c>.
    /// ASP.NET Core's static-file middleware serves
    /// <c>IWebHostEnvironment.WebRootFileProvider</c>, i.e. <c>wwwroot/</c> — a directory this
    /// application does not have. Task 7.2 registered <c>UseStaticFiles()</c>, so until this
    /// class existed the provider was a <c>NullFileProvider</c> and <b>no static asset served
    /// at all</b>.
    /// </para>
    /// <para>
    /// <b>Why the assets were NOT relocated under <c>wwwroot/</c>.</b> The deferral gave two
    /// options and left the choice here. Relocation loses, on three independent counts, all of
    /// which were checked in the source rather than assumed:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <c>Nop.Web.Framework.Themes.ThemeProvider</c> enumerates
    /// <c>CommonHelper.MapPath("~/Themes/")</c> looking for <c>theme.config</c>, and
    /// <c>ThemeConfiguration</c> keeps the resolved <c>DirectoryInfo.FullName</c>. Moving
    /// <c>Themes/DefaultClean/Content</c> to <c>wwwroot</c> would split a theme across two
    /// roots while its <c>Views/</c> and <c>theme.config</c> stayed put — the theme model
    /// assumes one directory.
    /// </item>
    /// <item>
    /// <c>Nop.Web.Factories.CommonModelFactory.PrepareFaviconModel</c> probes
    /// <c>CommonHelper.MapPath("~/favicon-{storeId}.ico")</c> and then
    /// <c>MapPath("~/favicon.ico")</c> — i.e. the <b>content root</b> — and only then emits a
    /// root-relative URL. A relocated <c>favicon.ico</c> would be served but never
    /// <i>found</i>, so no favicon link would ever be rendered.
    /// </item>
    /// <item>
    /// <c>Nop.Services.Media.PictureService</c> writes generated thumbnails to
    /// <c>~/Content/Images/Thumbs</c> through <c>CommonHelper.MapPath</c> as well. The write
    /// path and the read path must be the same directory.
    /// </item>
    /// </list>
    /// <para>
    /// So the assets stay where they are and the <i>provider</i> moves instead. Task 7.3
    /// confirmed this is safe from the view side: all 193 views reference assets through
    /// <c>AppendCssFileParts</c>/<c>AddScriptParts</c> with <c>~/</c>-rooted virtual paths or
    /// through <c>Url.Content</c>, so every emitted URL is unchanged either way.
    /// </para>
    /// <para>
    /// <b>Why this is an allow-list and not a bare <c>PhysicalFileProvider</c> over the content
    /// root.</b> That is the direct tension the deferral flags against deferral 39: the content
    /// root also holds <c>App_Data/Settings.txt</c> (the <b>database connection string</b>),
    /// <c>App_Data/*.sql</c>, <c>Web.config</c>, <c>appsettings.json</c>, every <c>.cshtml</c>,
    /// and — once deployed — <c>Administration/db_backups/*.bak</c>. <c>System.Web</c> blocked
    /// <c>App_Data</c> implicitly; ASP.NET Core does not. Widening the static root to the
    /// content root would publish all of it. This provider therefore returns
    /// <see cref="NotFoundFileInfo"/> for everything except the paths 3.90 actually served.
    /// </para>
    /// <para>
    /// <b>It also closes deferral 33/14.33 (cache busting) for free.</b>
    /// <c>PageHeadBuilder.GetAssetUrl</c> runs every emitted asset URL through
    /// <c>IFileVersionProvider.AddFileVersionToPath</c>, and
    /// <c>DefaultFileVersionProvider</c> resolves against
    /// <c>IWebHostEnvironment.WebRootFileProvider</c> — returning the path <b>unchanged and
    /// with no warning</b> when the file is not found. Because
    /// <see cref="NopStaticFilesExtensions.UseNopStaticFileProvider"/> assigns this instance to
    /// that very property, versioning and serving are guaranteed to see the same file set:
    /// they cannot drift apart.
    /// </para>
    /// <para>
    /// Static-file middleware never enumerates directories (<c>UseDirectoryBrowser</c> is not
    /// registered) and <c>IFileVersionProvider</c> only calls
    /// <see cref="GetFileInfo(string)"/>, so the conservative
    /// <see cref="GetDirectoryContents(string)"/> behaviour below costs nothing.
    /// </para>
    /// </remarks>
    public partial class NopStaticFileProvider : IFileProvider
    {
        #region Fields

        private readonly IFileProvider _contentRoot;

        /// <summary>
        /// Top-level directories served in full. These are exactly 3.90's static asset trees.
        /// </summary>
        private static readonly string[] AllowedRoots = { "Content", "Scripts" };

        /// <summary>
        /// Files at the very root of the application that 3.90 served.
        /// </summary>
        /// <remarks>
        /// <c>favicon.ico</c> is what <c>CommonModelFactory</c> falls back to;
        /// <c>ErrorPage.htm</c> is the file <c>&lt;customErrors defaultRedirect&gt;</c> named
        /// (task 7.2 sends it directly via <c>Response.SendFileAsync</c>, so serving it here is
        /// belt-and-braces); <c>FileNotFound.htm</c> is the file
        /// <c>&lt;error statusCode="404" redirect&gt;</c> named, which task 7.2 superseded with
        /// <c>UseStatusCodePagesWithReExecute("/page-not-found")</c> but which is kept
        /// reachable so an existing deployment's bookmarks/links do not break.
        /// Per-store favicons (<c>favicon-{storeId}.ico</c>) are matched by pattern.
        /// </remarks>
        private static readonly string[] AllowedRootFiles =
        {
            "favicon.ico",
            "ErrorPage.htm",
            "FileNotFound.htm"
        };

        /// <summary>
        /// Extensions refused even inside an allowed tree — defence in depth.
        /// </summary>
        /// <remarks>
        /// Mostly redundant: <c>StaticFileOptions.ServeUnknownFileTypes</c> is left at its
        /// <c>false</c> default, and none of these extensions is in the content-type map, so
        /// the middleware would 404 them anyway (that is also how 3.90's
        /// <c>DenyAccessToPluginDLLs</c> <c>HttpForbiddenHandler</c> for <c>*.dll</c> is
        /// reproduced). Listed explicitly so the refusal does not silently depend on a
        /// framework default that a future <c>ContentTypeProvider</c> edit could undo — and
        /// note <c>.bak</c> IS deliberately here: 3.90's <c>&lt;mimeMap&gt;</c> mapped it in
        /// order to serve database backups over HTTP, which this migration refuses to
        /// reproduce (see deferral 39 in Nop.Web.csproj).
        /// </remarks>
        private static readonly HashSet<string> DeniedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs", ".cshtml", ".config", ".csproj", ".sln", ".user",
                ".dll", ".pdb", ".exe",
                ".bak", ".mdf", ".ldf", ".sdf"
            };

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a provider over the given content root.
        /// </summary>
        /// <param name="contentRootPath">
        /// Absolute path of the content root, i.e. <c>IWebHostEnvironment.ContentRootPath</c> —
        /// the same directory <c>CommonHelper.BaseDirectory</c> is set to by
        /// <c>UseNopHostingEnvironment</c> (runtime deferral 1.5).
        /// </param>
        public NopStaticFileProvider(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
                throw new ArgumentException("A content root path is required", "contentRootPath");

            _contentRoot = new PhysicalFileProvider(contentRootPath);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Normalises a request subpath to <c>a/b/c</c> form, or returns null when it cannot be
        /// trusted.
        /// </summary>
        protected static string Normalize(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
                return string.Empty;

            var path = subpath.Replace('\\', '/').Trim('/');

            //PhysicalFileProvider already refuses these, but fail before touching the disk
            if (path.Contains("..") || path.Contains(":"))
                return null;

            return path;
        }

        /// <summary>
        /// Whether the given normalised subpath may be served.
        /// </summary>
        protected virtual bool IsAllowedFile(string path)
        {
            if (path == null || path.Length == 0)
                return false;

            var extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension) && DeniedExtensions.Contains(extension))
                return false;

            var segments = path.Split('/');

            //---- a file at the application root: ~/favicon.ico, ~/ErrorPage.htm, ...
            if (segments.Length == 1)
            {
                foreach (var allowed in AllowedRootFiles)
                {
                    if (string.Equals(segments[0], allowed, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                //per-store favicons - CommonModelFactory probes "favicon-{storeId}.ico"
                return segments[0].StartsWith("favicon-", StringComparison.OrdinalIgnoreCase)
                    && segments[0].EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
            }

            //---- ~/Content/** and ~/Scripts/**
            //Served in full, which is 3.90 parity. NOTE this includes
            //~/Content/files/ExportImport, where the export/feed files that
            //Nop.Plugin.Feed.GoogleShopping links to are generated - 3.90's static handler
            //served that directory too, and narrowing it here would break that feed URL.
            //Publish shaping (deferral 39) is what keeps generated files out of a deployment.
            foreach (var root in AllowedRoots)
            {
                if (string.Equals(segments[0], root, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            //---- ~/Themes/<theme>/Content/** and ~/Themes/<theme>/preview.jpg
            //Deliberately NOT the whole theme directory: ~/Themes/<theme>/Views/** holds
            //.cshtml plus a Web.config, and ~/Themes/<theme>/theme.config is configuration.
            //(Both extensions are in DeniedExtensions as well, so this is doubly closed.)
            if (string.Equals(segments[0], "Themes", StringComparison.OrdinalIgnoreCase)
                && segments.Length >= 3)
            {
                if (string.Equals(segments[2], "Content", StringComparison.OrdinalIgnoreCase)
                    && segments.Length >= 4)
                    return true;

                //the theme thumbnail shown by the admin theme picker
                if (segments.Length == 3
                    && string.Equals(segments[2], "preview.jpg", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether the given normalised subpath may be enumerated.
        /// </summary>
        protected virtual bool IsAllowedDirectory(string path)
        {
            //never expose the content root itself
            if (string.IsNullOrEmpty(path))
                return false;

            var segments = path.Split('/');

            foreach (var root in AllowedRoots)
            {
                if (string.Equals(segments[0], root, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return string.Equals(segments[0], "Themes", StringComparison.OrdinalIgnoreCase)
                && segments.Length >= 3
                && string.Equals(segments[2], "Content", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Locates a file, or a <see cref="NotFoundFileInfo"/> when it is outside the allow-list
        /// </summary>
        public virtual IFileInfo GetFileInfo(string subpath)
        {
            var path = Normalize(subpath);
            if (!IsAllowedFile(path))
                return new NotFoundFileInfo(subpath ?? string.Empty);

            return _contentRoot.GetFileInfo(path);
        }

        /// <summary>
        /// Enumerates a directory, or <see cref="NotFoundDirectoryContents"/> when it is outside
        /// the allow-list
        /// </summary>
        public virtual IDirectoryContents GetDirectoryContents(string subpath)
        {
            var path = Normalize(subpath);
            if (path == null || !IsAllowedDirectory(path))
                return NotFoundDirectoryContents.Singleton;

            return _contentRoot.GetDirectoryContents(path);
        }

        /// <summary>
        /// Creates a change token for the given filter
        /// </summary>
        /// <remarks>
        /// Delegated unfiltered: a change token discloses nothing about file contents, and
        /// <c>DefaultFileVersionProvider</c> relies on it to evict a cached <c>?v=</c> hash when
        /// an asset is redeployed. Filtering here would silently make cache busting stale
        /// rather than absent, which is worse than the deferral it fixes.
        /// </remarks>
        public virtual IChangeToken Watch(string filter)
        {
            return _contentRoot.Watch(filter);
        }

        #endregion
    }
}
