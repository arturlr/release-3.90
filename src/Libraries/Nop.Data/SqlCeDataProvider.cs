using System.Data.Common;
using Nop.Core.Data;
using Microsoft.Data.SqlClient;


namespace Nop.Data
{
    /// <summary>
    /// SQL Server Compact data provider - deprecated, redirects to SQL Server.
    /// SQL Server Compact is not supported in .NET Core. This class is retained for interface 
    /// compatibility but uses SQL Server instead.
    /// </summary>
    public class SqlCeDataProvider : IDataProvider
    {
        /// <summary>
        /// Initialize connection factory
        /// </summary>
        public virtual void InitConnectionFactory()
        {
            // SQL Server Compact is not supported in .NET Core
            // Connection is configured via DbContextOptions in EF Core
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
            // In EF Core, database initialization is handled via migrations or EnsureCreated
            // SQL Server Compact is not supported in .NET Core
        }

        /// <summary>
        /// A value indicating whether this data provider supports stored procedures
        /// </summary>
        public virtual bool StoredProceduredSupported
        {
            get { return false; }
        }

        /// <summary>
        /// A value indicating whether this data provider supports backup
        /// </summary>
        public bool BackupSupported
        {
            get { return false; }
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
            return 0; //HASHBYTES functions is missing in SQL CE
        }
    }
}
