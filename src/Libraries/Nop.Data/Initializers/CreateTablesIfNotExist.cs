using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// Database initializer that creates tables if they do not exist.
    /// Adapted for EF Core - uses EnsureCreated and raw SQL for validation.
    /// </summary>
    public class CreateTablesIfNotExist<TContext> where TContext : DbContext
    {
        private readonly string[] _tablesToValidate;
        private readonly string[] _customCommands;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="tablesToValidate">A list of existing table names to validate; null to don't validate table names</param>
        /// <param name="customCommands">A list of custom commands to execute</param>
        public CreateTablesIfNotExist(string[] tablesToValidate, string[] customCommands)
        {
            this._tablesToValidate = tablesToValidate;
            this._customCommands = customCommands;
        }

        public void InitializeDatabase(TContext context)
        {
            bool dbExists = context.Database.CanConnect();

            if (dbExists)
            {
                bool createTables;
                if (_tablesToValidate != null && _tablesToValidate.Length > 0)
                {
                    //we have some table names to validate
                    var existingTableNames = new List<string>(
                        context.Database.SqlQueryRaw<string>(
                            "SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE'"));
                    createTables = !existingTableNames.Intersect(_tablesToValidate, StringComparer.InvariantCultureIgnoreCase).Any();
                }
                else
                {
                    //check whether tables are already created
                    int numberOfTables = 0;
                    foreach (var t1 in context.Database.SqlQueryRaw<int>(
                        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE'"))
                        numberOfTables = t1;

                    createTables = numberOfTables == 0;
                }

                if (createTables)
                {
                    //create all tables
                    var dbCreationScript = context.Database.GenerateCreateScript();
                    context.Database.ExecuteSqlRaw(dbCreationScript);

                    context.SaveChanges();

                    if (_customCommands != null && _customCommands.Length > 0)
                    {
                        foreach (var command in _customCommands)
                            context.Database.ExecuteSqlRaw(command);
                    }
                }
            }
            else
            {
                throw new ApplicationException("No database instance");
            }
        }
    }
}
