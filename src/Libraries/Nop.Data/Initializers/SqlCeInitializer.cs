using System;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// Database initializer base class. In EF Core, database initialization is handled
    /// via migrations or EnsureCreated. This class is retained for compatibility.
    /// </summary>
    /// <typeparam name="T">The type of the context.</typeparam>
    public abstract class SqlCeInitializer<T> where T : DbContext
    {
        public abstract void InitializeDatabase(T context);
    }
}
