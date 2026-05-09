using Microsoft.EntityFrameworkCore;
using Nop.Core;
using NUnit.Framework;

namespace Nop.Data.Tests
{
    [TestFixture]
    public abstract class PersistenceTest
    {
        protected NopObjectContext context;
        private string _databaseName;

        [SetUp]
        public virtual void SetUp()
        {
            _databaseName = "NopDataTests_" + System.Guid.NewGuid().ToString("N");

            var options = new DbContextOptionsBuilder<NopObjectContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            context = new NopObjectContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// Persistance test helper
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
                    .UseInMemoryDatabase(databaseName: _databaseName)
                    .Options;
                context = new NopObjectContext(options);
            }

            var fromDb = context.Set<T>().Find(id);
            return fromDb;
        }
    }
}
