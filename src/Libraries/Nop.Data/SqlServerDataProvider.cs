using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Data.Initializers;

namespace Nop.Data
{
    /// <summary>
    /// SQL Server data provider, re-based on EF Core (task 3.2).
    /// </summary>
    /// <remarks>
    /// EF6 -> EF Core substitutions:
    ///   * <c>System.Data.Entity.Infrastructure.SqlConnectionFactory</c> +
    ///     <c>Database.DefaultConnectionFactory</c> - removed. EF Core has no ambient connection
    ///     factory; the provider and connection string are supplied per-context through
    ///     <c>DbContextOptionsBuilder.UseSqlServer(...)</c>, which
    ///     <see cref="NopObjectContext.OnConfiguring"/> now does.
    ///   * <c>Database.SetInitializer(initializer)</c> - removed. EF Core has no initializer
    ///     pipeline, so the configured initializer is published on
    ///     <see cref="DatabaseInitializer"/> for the installation path to invoke explicitly.
    ///   * <c>System.Data.SqlClient.SqlParameter</c> -> <c>Microsoft.Data.SqlClient.SqlParameter</c>
    ///     (shipped by Microsoft.EntityFrameworkCore.SqlServer). Both derive from
    ///     <see cref="DbParameter"/>, which is the declared return type of
    ///     <c>IDataProvider.GetParameter()</c>, so the public contract is unchanged.
    /// </remarks>
    public class SqlServerDataProvider : IDataProvider
    {
        #region Properties

        /// <summary>
        /// The schema initializer configured by <see cref="SetDatabaseInitializer"/>.
        /// </summary>
        /// <remarks>
        /// This replaces EF6's global <c>Database.SetInitializer</c> registration. Nothing in EF
        /// Core consults it, so <see cref="InitDatabase"/> invokes it explicitly (task 7.7 —
        /// see the remarks there for why doing it at host startup instead does not work).
        /// </remarks>
        public static INopDatabaseInitializer<NopObjectContext> DatabaseInitializer { get; private set; }

        #endregion

        #region Utilities

        protected virtual string[] ParseCommands(string filePath, bool throwExceptionIfNonExists)
        {
            if (!File.Exists(filePath))
            {
                if (throwExceptionIfNonExists)
                    throw new ArgumentException(string.Format("Specified file doesn't exist - {0}", filePath));
                
                return new string[0];
            }


            var statements = new List<string>();
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream))
            {
                string statement;
                while ((statement = ReadNextStatementFromStream(reader)) != null)
                {
                    statements.Add(statement);
                }
            }

            return statements.ToArray();
        }

        protected virtual string ReadNextStatementFromStream(StreamReader reader)
        {
            var sb = new StringBuilder();

            while (true)
            {
                var lineOfText = reader.ReadLine();
                if (lineOfText == null)
                {
                    if (sb.Length > 0)
                        return sb.ToString();
                    
                    return null;
                }

                if (lineOfText.TrimEnd().ToUpper() == "GO")
                    break;

                sb.Append(lineOfText + Environment.NewLine);
            }

            return sb.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize connection factory
        /// </summary>
        /// <remarks>
        /// No-op under EF Core. EF6 installed a process-wide
        /// <c>Database.DefaultConnectionFactory</c> so that a context constructed from a bare
        /// database name could resolve a provider. EF Core requires the provider to be named
        /// explicitly per context, which <see cref="NopObjectContext.OnConfiguring"/> does via
        /// <c>UseSqlServer</c>. The method is retained because it is part of
        /// <see cref="IDataProvider"/> (owned by Nop.Core) and is called from
        /// Nop.Web.Framework's DependencyRegistrar.
        /// </remarks>
        public virtual void InitConnectionFactory()
        {
        }

        /// <summary>
        /// Initialize database — configures the schema initializer and <b>runs</b> it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Runtime deferral 4.8, re-opened and closed properly by task 7.7.</b> This method is
        /// called from exactly one place: <c>InstallController.Index(InstallModel)</c>, immediately
        /// after the installer writes <c>App_Data/Settings.txt</c> and immediately before
        /// <c>IInstallationService.InstallData(...)</c>. It is therefore <i>the</i> point at which
        /// a fresh database must acquire its schema.
        /// </para>
        /// <para>
        /// Under EF6 that happened implicitly: <c>SetDatabaseInitializer()</c> called
        /// <c>Database.SetInitializer(initializer)</c>, a process-wide <b>lazily fired</b> hook, and
        /// EF6 ran the initializer the first time any <c>NopObjectContext</c> was used — which was
        /// moments later, inside <c>InstallData</c>. EF Core deleted the initializer concept, so
        /// <c>SetDatabaseInitializer()</c> now only <i>publishes</i> the object on
        /// <see cref="DatabaseInitializer"/>, and until this task <b>nothing called it on the
        /// installation path</b>.
        /// </para>
        /// <para>
        /// Task 7.2 added <c>Nop.Web/Program.InitializeDatabaseSchema()</c> and deferral 4.8 was
        /// recorded as resolved on that basis. It is not: that method opens with
        /// <c>if (!DataSettingsHelper.DatabaseIsInstalled()) return;</c> and runs once during host
        /// startup, so for the only case the deferral is about — installing onto an empty database —
        /// it early-returns, because the store does not become "installed" until the installer runs
        /// later in the process's life. On subsequent starts the store IS installed and the
        /// initializer short-circuits on its own table probe. The call was dead in both directions.
        /// </para>
        /// <para>
        /// <b>Measured, which is how this was found.</b> Task 7.7 stood up SQL Server 2022 in a
        /// container and POSTed the real installer form. Before this change the installer returned
        /// its own "Setup failed" view carrying:
        /// <c>Entity: Store State: Added … Invalid object name 'Store'.</c> — the exact symptom
        /// deferral 4.8 predicts, i.e. <b>nopCommerce could not be installed at all</b>. After this
        /// change the same POST installs successfully.
        /// </para>
        /// <para>
        /// The initializer is idempotent and cheap on an already-provisioned database: it probes
        /// <c>INFORMATION_SCHEMA.TABLES</c> for <c>Customer</c>/<c>Discount</c>/<c>Order</c>/
        /// <c>Product</c>/<c>ShoppingCartItem</c> and returns immediately when any exists, so
        /// calling it here cannot disturb an upgrade or a re-run.
        /// </para>
        /// <para>
        /// Exceptions are deliberately <b>not</b> swallowed. <c>InstallController</c> wraps this
        /// call in its own <c>try/catch</c>, which resets the data-settings file and surfaces the
        /// message through <c>SetupFailed</c> in the validation summary — the same treatment every
        /// other installation failure gets.
        /// </para>
        /// </remarks>
        public virtual void InitDatabase()
        {
            InitConnectionFactory();
            SetDatabaseInitializer();
            CreateDatabaseSchema();
        }

        /// <summary>
        /// Runs the published <see cref="DatabaseInitializer"/> against a freshly resolved
        /// <see cref="NopObjectContext"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>protected virtual</c> so a deployment that prefers EF Core migrations
        /// (<c>Database.Migrate()</c>) can substitute its own strategy, which is the alternative
        /// deferral 4.8 offered.
        /// </para>
        /// <para>
        /// The context is resolved through <see cref="EngineContext"/> rather than injected because
        /// <see cref="IDataProvider"/> (owned by Nop.Core) declares a parameterless
        /// <c>InitDatabase()</c> and <c>SqlServerDataProvider</c> is constructed by
        /// <c>EfDataProviderManager</c> with no arguments — changing either would be a breaking
        /// signature change across Nop.Core, Nop.Web.Framework and every plugin data provider, for
        /// no behavioural gain. <c>CommonHelper.MapPath</c> is already used a few lines above, so
        /// this class is not newly coupled to a static seam.
        /// </para>
        /// <para>
        /// A resolve is used rather than <c>new NopObjectContext(connectionString)</c> so the
        /// connection string comes from the same place every other consumer gets it. Note the
        /// registration that matters here is Nop.Web.Framework's <b>uninstalled</b> branch,
        /// <c>builder.Register&lt;IDbContext&gt;(c =&gt; new NopObjectContext(dataSettingsManager.LoadSettings().DataConnectionString))</c>,
        /// which re-reads <c>Settings.txt</c> on every resolve — so the context picks up the
        /// connection string the installer wrote seconds earlier.
        /// </para>
        /// </remarks>
        protected virtual void CreateDatabaseSchema()
        {
            var initializer = DatabaseInitializer;
            if (initializer == null)
                return;

            var context = EngineContext.Current.Resolve<IDbContext>() as NopObjectContext;
            if (context == null)
                return;

            initializer.InitializeDatabase(context);
        }

        /// <summary>
        /// Set database initializer
        /// </summary>
        public virtual void SetDatabaseInitializer()
        {
            //pass some table names to ensure that we have nopCommerce 2.X installed
            var tablesToValidate = new[] { "Customer", "Discount", "Order", "Product", "ShoppingCartItem" };

            //custom commands (stored procedures, indexes)

            var customCommands = new List<string>();
            customCommands.AddRange(ParseCommands(CommonHelper.MapPath("~/App_Data/Install/SqlServer.Indexes.sql"), false));
            customCommands.AddRange(ParseCommands(CommonHelper.MapPath("~/App_Data/Install/SqlServer.StoredProcedures.sql"), false));

            //EF6: Database.SetInitializer(initializer). EF Core has no equivalent hook, so the
            //initializer is published for explicit invocation instead.
            DatabaseInitializer = new CreateTablesIfNotExist<NopObjectContext>(tablesToValidate, customCommands.ToArray());
        }

        /// <summary>
        /// A value indicating whether this data provider supports stored procedures
        /// </summary>
        public virtual bool StoredProceduredSupported
        {
            get { return true; }
        }

        /// <summary>
        /// A value indicating whether this data provider supports backup
        /// </summary>
        public virtual bool BackupSupported
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a support database parameter object (used by stored procedures)
        /// </summary>
        /// <returns>Parameter</returns>
        public virtual DbParameter GetParameter()
        {
            //Microsoft.Data.SqlClient.SqlParameter (was System.Data.SqlClient.SqlParameter);
            //both are DbParameter, so IDataProvider.GetParameter() is unchanged.
            return new SqlParameter();
        }

        /// <summary>
        /// Maximum length of the data for HASHBYTES functions
        /// returns 0 if HASHBYTES function is not supported
        /// </summary>
        /// <returns>Length of the data for HASHBYTES functions</returns>
        public int SupportedLengthOfBinaryHash()
        {
            return 8000; //for SQL Server 2008 and above HASHBYTES function has a limit of 8000 characters.
        }

        #endregion
    }
}
