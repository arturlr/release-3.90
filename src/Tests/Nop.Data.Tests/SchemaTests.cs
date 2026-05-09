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
            var options = new DbContextOptionsBuilder<NopObjectContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=NopSchemaTest;Trusted_Connection=True;")
                .Options;

            var ctx = new NopObjectContext(options);
            string result = ctx.CreateDatabaseScript();
            result.ShouldNotBeNull();
        }
    }
}
