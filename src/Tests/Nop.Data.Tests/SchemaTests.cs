using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Data.Tests
{
    [TestFixture]
    public class SchemaTests
    {
        [Test]
        public void Can_generate_schema()
        {
            using (var connection = new SqliteConnection("DataSource=:memory:"))
            {
                connection.Open();

                var options = new DbContextOptionsBuilder<NopObjectContext>()
                    .UseSqlite(connection)
                    .Options;

                using (var ctx = new NopObjectContext(options))
                {
                    string result = ctx.CreateDatabaseScript();
                    result.ShouldNotBeNull();
                }
            }
        }
    }
}
