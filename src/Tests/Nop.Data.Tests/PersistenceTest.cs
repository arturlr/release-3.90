using Microsoft.EntityFrameworkCore;
using Nop.Core;
using NUnit.Framework;

namespace Nop.Data.Tests
{
    [TestFixture]
    public abstract class PersistenceTest
    {
        protected NopObjectContext context;

        [SetUp]
        public virtual void SetUp()
        {
            var options = new DbContextOptionsBuilder<NopObjectContext>()
                .UseInMemoryDatabase(databaseName: "NopTestDb_" + System.Guid.NewGuid().ToString("N"))
                .Options;
            context = new NopObjectContext(options);
        }

        [TearDown]
        public virtual void TearDown()
        {
            context?.Dispose();
        }

        /// <summary>
        /// Persistence test helper
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="disposeContext">A value indicating whether to dispose context</param>
        protected T SaveAndLoadEntity<T>(T entity, bool disposeContext = true) where T : BaseEntity
        {
            context.Set<T>().Add(entity);
            context.SaveChanges();

            object id = entity.Id;

            if (disposeContext)
            {
                context.Dispose();
                var options = new DbContextOptionsBuilder<NopObjectContext>()
                    .UseInMemoryDatabase(databaseName: "NopTestDb_Reload")
                    .Options;
                context = new NopObjectContext(options);
            }

            var fromDb = context.Set<T>().Find(id);
            return fromDb;
        }
    }
}
