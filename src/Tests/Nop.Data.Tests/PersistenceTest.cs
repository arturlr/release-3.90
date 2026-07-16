using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using NUnit.Framework;

namespace Nop.Data.Tests
{
    [TestFixture]
    public abstract class PersistenceTest
    {
        protected NopObjectContext context;
        private SqliteConnection _connection;

        [SetUp]
        public virtual void SetUp()
        {
            // SQLite in-memory database requires keeping the connection open
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<NopObjectContext>()
                .UseSqlite(_connection)
                .Options;

            context = new NopObjectContext(options);
            context.Database.EnsureCreated();
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (context != null)
            {
                context.Dispose();
                context = null;
            }
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
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
                    .UseSqlite(_connection)
                    .Options;

                context = new NopObjectContext(options);
            }

            var fromDb = context.Set<T>().Find(id);
            return fromDb;
        }
    }
}
