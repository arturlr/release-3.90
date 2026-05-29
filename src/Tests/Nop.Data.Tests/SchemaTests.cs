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
                .UseInMemoryDatabase(databaseName: "SchemaTestDb")
                .Options;
            var ctx = new NopObjectContext(options);
            // In EF Core, we verify the model can be built
            var model = ctx.Model;
            model.ShouldNotBeNull();
        }
    }
}
