namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Typed options replacing the classic
    /// <c>&lt;system.web&gt;/&lt;authentication mode="Forms"&gt;/&lt;forms&gt;</c> element
    /// (task 6.5, Requirements 1.4, 5.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the only genuine settings surface <c>Nop.Web.Framework</c> owns. Task 6.4
    /// transcribed the four <c>&lt;forms&gt;</c> attribute values from
    /// <c>Nop.Web/Web.config</c> into constants on
    /// <see cref="NopServiceCollectionExtensions"/> plus a <c>requireSsl</c> method
    /// parameter, because there was no configuration model yet. Requirement 1.4 says that
    /// configuration must move to the .NET 10 configuration model, so those values are now
    /// bindable from any <c>IConfiguration</c> source:
    /// <code>
    /// // appsettings.json (authored by task 7.4)
    /// "Authentication": {
    ///   "CookieName":        "NOPCOMMERCE.AUTH",
    ///   "LoginPath":         "/login",
    ///   "TimeoutMinutes":    43200,
    ///   "RequireSsl":        false,
    ///   "SlidingExpiration": true,
    ///   "CookiePath":        "/"
    /// }
    /// </code>
    /// and consumed with the configuration-taking overload
    /// <c>services.AddNopFramework(builder.Configuration)</c>.
    /// </para>
    /// <para>
    /// <b>Why this is a new POCO and not part of <c>NopConfig</c>.</b> <c>NopConfig</c>
    /// (in <c>Nop.Core.Configuration</c>) is the migrated <c>&lt;NopConfig&gt;</c> custom
    /// section and is deliberately a byte-for-byte translation of it — adding members would
    /// change a committed, gated project and would blur which legacy element each setting
    /// came from. The <c>&lt;forms&gt;</c> element is a different legacy section
    /// (<c>system.web</c>) with a different owner, so it gets its own POCO in the project
    /// that consumes it. No configuration-reading machinery is duplicated: binding uses the
    /// same <c>IConfiguration</c>/<c>Bind</c> mechanism <c>NopConfig</c> uses, and the
    /// pre-container static seam remains <c>Nop.Core.Configuration.NopConfigurationManager</c>.
    /// </para>
    /// <para>
    /// Every default below reproduces 3.90's shipped <c>Nop.Web/Web.config</c> exactly, so a
    /// host that supplies no <c>Authentication</c> section behaves as 3.90 did. <b>Note the
    /// security caveat carried over from runtime deferral 7.13:</b> 3.90 shipped
    /// <c>requireSSL="false"</c>, so <see cref="RequireSsl"/> defaults to <c>false</c>. Any
    /// deployment whose own <c>Web.config</c> had it <c>true</c> MUST set it <c>true</c> here
    /// or the port is a security downgrade.
    /// </para>
    /// </remarks>
    public partial class NopAuthenticationConfig
    {
        /// <summary>
        /// The configuration section this options class binds from.
        /// </summary>
        public const string SectionName = "Authentication";

        /// <summary>
        /// Authentication cookie name — <c>&lt;forms name="NOPCOMMERCE.AUTH"&gt;</c>
        /// </summary>
        public string CookieName { get; set; }

        /// <summary>
        /// Login path — <c>&lt;forms loginUrl="~/login"&gt;</c>. Application-relative
        /// <c>~/</c> has no counterpart in ASP.NET Core's <c>PathString</c>, so the value is
        /// a rooted path.
        /// </summary>
        public string LoginPath { get; set; }

        /// <summary>
        /// Ticket lifetime in minutes — <c>&lt;forms timeout="43200"&gt;</c> (30 days)
        /// </summary>
        public int TimeoutMinutes { get; set; }

        /// <summary>
        /// Whether the cookie is restricted to HTTPS — <c>&lt;forms requireSSL="false"&gt;</c>
        /// </summary>
        public bool RequireSsl { get; set; }

        /// <summary>
        /// Whether the ticket lifetime is renewed on activity —
        /// <c>&lt;forms slidingExpiration="true"&gt;</c>
        /// </summary>
        public bool SlidingExpiration { get; set; }

        /// <summary>
        /// Cookie path — <c>&lt;forms path="/"&gt;</c>
        /// </summary>
        public string CookiePath { get; set; }

        /// <summary>
        /// Creates an instance carrying 3.90's shipped <c>&lt;forms&gt;</c> values
        /// </summary>
        public NopAuthenticationConfig()
        {
            CookieName = NopServiceCollectionExtensions.AuthenticationCookieName;
            LoginPath = NopServiceCollectionExtensions.DefaultLoginPath;
            TimeoutMinutes = NopServiceCollectionExtensions.DefaultExpireMinutes;
            RequireSsl = false;
            SlidingExpiration = true;
            CookiePath = "/";
        }
    }
}
