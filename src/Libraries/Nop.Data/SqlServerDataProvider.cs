using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Data;

namespace Nop.Data
{
    /// <summary>
    /// SQL Server data provider for EF Core
    /// </summary>
    public class SqlServerDataProvider : IDataProvider
    {
        #region Utilities

        protected virtual string[] ParseCommands(string filePath, bool throwExceptionIfNonExists)
        {
            if (!File.Exists(filePath))
            {
                if (throwExceptionIfNonExists)
                    throw new ArgumentException($"Specified file doesn't exist - {filePath}");

                return Array.Empty<string>();
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

                if (lineOfText.TrimEnd().Equals("GO", StringComparison.OrdinalIgnoreCase))
                    break;

                sb.Append(lineOfText + Environment.NewLine);
            }

            return sb.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize database
        /// </summary>
        public virtual void InitDatabase()
        {
            // In EF Core, database initialization is handled via migrations or EnsureCreated()
            // No connection factory or database initializer concept exists
        }

        /// <summary>
        /// Set database initializer
        /// </summary>
        public virtual void SetDatabaseInitializer()
        {
            // In EF Core, migrations handle schema initialization
            // This method is kept for interface compatibility but is a no-op
            // Use context.Database.Migrate() or context.Database.EnsureCreated() at startup
        }

        /// <summary>
        /// Initialize connection factory (no-op in EF Core - connection is configured via DbContextOptions)
        /// </summary>
        public virtual void InitConnectionFactory()
        {
            // No-op: In EF Core, the connection is configured in DbContextOptions
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
            return new SqlParameter();
        }

        /// <summary>
        /// Maximum length of the data for HASHBYTES functions
        /// returns 0 if HASHBYTES function is not supported
        /// </summary>
        /// <returns>Length of the data for HASHBYTES functions</returns>
        public int SupportedLengthOfBinaryHash()
        {
            return 8000; // for SQL Server 2008 and above
        }

        #endregion
    }
}
