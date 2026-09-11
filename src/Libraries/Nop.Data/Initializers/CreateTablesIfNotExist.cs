using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nop.Core;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// Creates the nopCommerce schema in an existing (but empty) database.
    /// </summary>
    /// <remarks>
    /// EF6 -> EF Core port (task 3.2). Previously
    /// <c>IDatabaseInitializer&lt;TContext&gt;</c>, invoked implicitly by EF6 through
    /// <c>Database.SetInitializer</c>. EF Core has no initializer pipeline, so this is now a
    /// plain <see cref="INopDatabaseInitializer{TContext}"/> that must be called explicitly.
    ///
    /// API substitutions:
    ///   * <c>Database.Exists()</c>                       -> <c>Database.CanConnect()</c>
    ///   * <c>Database.SqlQuery&lt;T&gt;</c>              -> <c>Database.SqlQueryRaw&lt;T&gt;</c>
    ///                                                       (EF Core requires the single column
    ///                                                       to be aliased <c>Value</c>)
    ///   * <c>ObjectContext.CreateDatabaseScript()</c>    -> <c>Database.GenerateCreateScript()</c>
    ///   * <c>Database.ExecuteSqlCommand(...)</c>         -> <c>Database.ExecuteSqlRaw(...)</c>
    ///
    /// The generated EF Core script is batch-separated with <c>GO</c>, which is a client
    /// directive rather than T-SQL, so it is split before execution.
    ///
    /// The <c>System.Transactions.TransactionScope(Suppress)</c> wrapper around the existence
    /// check is dropped: it existed to keep EF6's implicit initializer work out of an ambient
    /// transaction, and this initializer is no longer invoked implicitly.
    /// </remarks>
    public class CreateTablesIfNotExist<TContext> : INopDatabaseInitializer<TContext> where TContext : DbContext
    {
        private readonly string[] _tablesToValidate;
        private readonly string[] _customCommands;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="tablesToValidate">A list of existing table names to validate; null to don't validate table names</param>
        /// <param name="customCommands">A list of custom commands to execute</param>
        public CreateTablesIfNotExist(string[] tablesToValidate, string [] customCommands)
        {
            this._tablesToValidate = tablesToValidate;
            this._customCommands = customCommands;
        }

        public void InitializeDatabase(TContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (!context.Database.CanConnect())
                throw new NopException("No database instance");

            bool createTables;
            if (_tablesToValidate != null && _tablesToValidate.Length > 0)
            {
                //we have some table names to validate
                var existingTableNames = new List<string>(context.Database.SqlQueryRaw<string>(
                    "SELECT table_name AS Value FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE'"));
                createTables = !existingTableNames.Intersect(_tablesToValidate, StringComparer.InvariantCultureIgnoreCase).Any();
            }
            else
            {
                //check whether tables are already created
                int numberOfTables = 0;
                foreach (var t1 in context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' "))
                    numberOfTables = t1;

                createTables = numberOfTables == 0;
            }

            if (!createTables)
                return;

            //create all tables
            //TASK 11.2: the GO split moved to Nop.Data.DbContextExtensions.SplitSqlIntoBatches so
            //the four plugin object contexts (runtime deferral 4.10) share ONE implementation with
            //this initializer instead of each re-deriving it. Behaviour is unchanged.
            foreach (var batch in DbContextExtensions.SplitSqlIntoBatches(context.Database.GenerateCreateScript()))
                context.Database.ExecuteSqlRaw(batch);

            //Seed(context);
            context.SaveChanges();

            if (_customCommands != null && _customCommands.Length > 0)
            {
                foreach (var command in _customCommands)
                    context.Database.ExecuteSqlRaw(command);
            }
        }
    }
}
