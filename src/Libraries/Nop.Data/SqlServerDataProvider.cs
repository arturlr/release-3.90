using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Nop.Core;
using Nop.Core.Data;
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
        /// Core consults it; the installation/startup path must call
        /// <c>DatabaseInitializer.InitializeDatabase(context)</c> itself. Tracked in the
        /// runtime-deferrals register.
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
        /// Initialize database
        /// </summary>
        public virtual void InitDatabase()
        {
            InitConnectionFactory();
            SetDatabaseInitializer();
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
