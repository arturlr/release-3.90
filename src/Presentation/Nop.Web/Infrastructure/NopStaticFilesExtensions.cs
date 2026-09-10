using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// Wires <see cref="NopStaticFileProvider"/> into the host and reproduces the parts of
    /// 3.90's <c>&lt;system.webServer&gt;&lt;staticContent&gt;</c> element that IIS no longer
    /// applies (task 7.4, runtime deferrals 40/7.1-5 and 33/14.33).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why <c>&lt;staticContent&gt;</c> had to move into code while
    /// <c>&lt;urlCompression&gt;</c> and the <c>X-Powered-By</c> removal stayed in
    /// <c>Web.config</c>.</b> Under the ASP.NET Core Module every request is forwarded to the
    /// application, so IIS's own static-file handler never runs and its
    /// <c>&lt;clientCache&gt;</c> / <c>&lt;mimeMap&gt;</c> settings are dead configuration. The
    /// application is now what serves static files, so those two settings belong here.
    /// <c>&lt;urlCompression&gt;</c> and <c>&lt;httpProtocol&gt;&lt;customHeaders&gt;</c> act on
    /// the response <i>after</i> ANCM hands it back, so IIS still honours them and they remain
    /// in <c>Web.config</c> where they always were.
    /// </para>
    /// </remarks>
    public static class NopStaticFilesExtensions
    {
        /// <summary>
        /// 3.90: <c>&lt;clientCache cacheControlMode="UseMaxAge"
        /// cacheControlMaxAge="7.00:00:00" /&gt;</c> — seven days, in seconds.
        /// </summary>
        public const int StaticFileMaxAgeSeconds = 7 * 24 * 60 * 60;

        /// <summary>
        /// Points <c>IWebHostEnvironment.WebRootFileProvider</c> at the content-root asset
        /// allow-list and returns the provider.
        /// </summary>
        /// <remarks>
        /// <para>
        /// MUST be called on the built application's environment (<c>app.Environment</c>) and
        /// BEFORE the first request, because two singletons capture the property:
        /// </para>
        /// <list type="bullet">
        /// <item>the <c>StaticFileOptions</c> post-configure step, which assigns
        /// <c>FileProvider ??= WebRootFileProvider</c> when the middleware is built; and</item>
        /// <item><c>DefaultFileVersionProvider</c>, whose constructor captures
        /// <c>hostingEnvironment.WebRootFileProvider</c> once. It is resolved lazily, the first
        /// time <c>PageHeadBuilder</c> is constructed during a request — well after startup, so
        /// assigning here is in time. This is the half of the wiring that closes
        /// <b>deferral 33</b>: without it, <c>AddFileVersionToPath</c> looks the file up in a
        /// <c>NullFileProvider</c>, silently returns the URL unversioned, and cache busting
        /// reverts to 3.90-with-bundling-off behaviour with no error anywhere.</item>
        /// </list>
        /// <para>
        /// <see cref="IWebHostEnvironment.WebRootPath"/> is deliberately left alone. Only the
        /// <i>provider</i> needs to change, and repointing <c>WebRootPath</c> at the content
        /// root would hand any <c>Path.Combine(env.WebRootPath, …)</c> caller a writable handle
        /// on the whole application directory. nopCommerce resolves its own physical paths
        /// through <c>CommonHelper.MapPath</c>, never through <c>WebRootPath</c> (verified: zero
        /// call sites in the solution).
        /// </para>
        /// </remarks>
        /// <param name="environment">The built application's web host environment</param>
        /// <returns>The provider that was installed</returns>
        public static IFileProvider UseNopStaticFileProvider(this IWebHostEnvironment environment)
        {
            if (environment == null)
                throw new ArgumentNullException("environment");

            var existing = environment.WebRootFileProvider;
            var nopProvider = new NopStaticFileProvider(environment.ContentRootPath);

            //if a wwwroot actually exists, keep it in front so anything placed there (or
            //contributed by a Razor class library / plugin static web asset) still wins.
            //PhysicalFileProvider is what the host installs when the directory is present;
            //a NullFileProvider means there is no wwwroot and there is nothing to compose.
            IFileProvider provider;
            if (existing is PhysicalFileProvider)
                provider = new CompositeFileProvider(existing, nopProvider);
            else
                provider = nopProvider;

            environment.WebRootFileProvider = provider;

            return provider;
        }

        /// <summary>
        /// Builds the <see cref="StaticFileOptions"/> that reproduce 3.90's
        /// <c>&lt;staticContent&gt;</c> element.
        /// </summary>
        /// <param name="fileProvider">
        /// The provider returned by <see cref="UseNopStaticFileProvider"/>. Passed explicitly
        /// rather than relying on the <c>WebRootFileProvider</c> fallback so that serving does
        /// not depend on option post-configuration ordering.
        /// </param>
        public static StaticFileOptions CreateNopStaticFileOptions(IFileProvider fileProvider)
        {
            if (fileProvider == null)
                throw new ArgumentNullException("fileProvider");

            //--------------------------------------------------------------------------
            // <staticContent> <mimeMap> entries
            //
            // 3.90 added five, because IIS's default map lacked them. ASP.NET Core's
            // FileExtensionContentTypeProvider ALREADY maps .json, .woff and .woff2, so only
            // .otf is genuinely missing - but all four are set explicitly so the map does not
            // depend on a framework default.
            //
            // DELIBERATE DEVIATION, recorded: 3.90 used the pre-IANA strings
            // application/x-font-opentype, application/font-woff and application/font-woff2.
            // The registered types are font/otf, font/woff and font/woff2, which is what the
            // framework map already contains and what is used here. Browsers ignore the
            // Content-Type for @font-face entirely (the format is sniffed), so this is
            // observationally identical while not re-introducing three deprecated strings.
            //
            // DELIBERATE OMISSION, security-relevant (deferral 39): 3.90 also mapped
            //     <mimeMap fileExtension=".bak" mimeType="application/octet-stream" />
            // with the comment "Allow database backup (.bak) file loading", which made
            // Administration/db_backups/*.bak downloadable over HTTP. That is NOT reproduced.
            // .bak is additionally in NopStaticFileProvider.DeniedExtensions, and
            // Administration/ is outside the allow-list altogether, so a backup is refused
            // three times over. Nop.Admin's backup download (task 8.x) goes through a
            // controller action that streams the file, so nothing depends on the mapping.
            //--------------------------------------------------------------------------
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".otf"] = "font/otf";
            contentTypeProvider.Mappings[".woff"] = "font/woff";
            contentTypeProvider.Mappings[".woff2"] = "font/woff2";
            contentTypeProvider.Mappings[".json"] = "application/json";

            return new StaticFileOptions
            {
                FileProvider = fileProvider,
                ContentTypeProvider = contentTypeProvider,

                //left at its false default ON PURPOSE. It is what makes an extension outside
                //the content-type map 404, which is how 3.90's DenyAccessToPluginDLLs
                //HttpForbiddenHandler for *.dll is reproduced, and it keeps .cs/.cshtml/
                //.config unreachable even if the allow-list were widened.
                ServeUnknownFileTypes = false,

                //<clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + StaticFileMaxAgeSeconds.ToString();
                }
            };
        }

        /// <summary>
        /// Convenience wrapper: installs the provider and registers the static-file middleware
        /// with 3.90's <c>&lt;staticContent&gt;</c> semantics.
        /// </summary>
        /// <remarks>
        /// Replaces task 7.2's bare <c>app.UseStaticFiles()</c> at exactly the same position in
        /// the pipeline — after the error handlers and before
        /// <c>UseNopPipeline()</c>/<c>UseRouting()</c> — so the mandated middleware order in
        /// <c>Program.cs</c> is unchanged.
        /// </remarks>
        public static IApplicationBuilder UseNopStaticFiles(this WebApplication app)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            var provider = app.Environment.UseNopStaticFileProvider();

            return app.UseStaticFiles(CreateNopStaticFileOptions(provider));
        }
    }
}
