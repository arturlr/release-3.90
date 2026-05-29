using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Nop.Core.Configuration;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Nop.Core.Caching
{
    /// <summary>
    /// Redis connection wrapper implementation
    /// </summary>
    public class RedisConnectionWrapper : IRedisConnectionWrapper, IDisposable
    {
        #region Fields

        private readonly NopConfig _config;
        private readonly Lazy<string> _connectionString;

        private volatile ConnectionMultiplexer _connection;
        private volatile RedLockFactory _redisLockFactory;
        private readonly object _lock = new object();
        private bool _disposed;

        #endregion

        #region Ctor

        public RedisConnectionWrapper(NopConfig config)
        {
            _config = config;
            _connectionString = new Lazy<string>(GetConnectionString);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get connection string to Redis cache from configuration
        /// </summary>
        /// <returns>Connection string</returns>
        protected string GetConnectionString()
        {
            return _config.RedisCachingConnectionString;
        }

        /// <summary>
        /// Get connection to Redis servers
        /// </summary>
        /// <returns>ConnectionMultiplexer</returns>
        protected ConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected)
                return _connection;

            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected)
                    return _connection;

                if (_connection != null)
                {
                    // Connection disconnected. Disposing old connection...
                    _connection.Dispose();
                }

                // Creating new instance of Redis Connection
                _connection = ConnectionMultiplexer.Connect(_connectionString.Value);
            }

            return _connection;
        }

        /// <summary>
        /// Create instance of RedLockFactory (RedLock.net 2.x API)
        /// </summary>
        /// <returns>RedLockFactory</returns>
        protected RedLockFactory CreateRedisLockFactory()
        {
            // Use the existing connection multiplexer for distributed locking
            var multiplexers = new List<RedLockMultiplexer>
            {
                new RedLockMultiplexer(GetConnection())
            };

            return RedLockFactory.Create(multiplexers);
        }

        /// <summary>
        /// Gets the RedLockFactory (lazy initialization)
        /// </summary>
        protected RedLockFactory RedisLockFactory
        {
            get
            {
                if (_redisLockFactory == null)
                {
                    lock (_lock)
                    {
                        if (_redisLockFactory == null)
                        {
                            _redisLockFactory = CreateRedisLockFactory();
                        }
                    }
                }
                return _redisLockFactory;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Obtain an interactive connection to a database inside redis
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        /// <returns>Redis cache database</returns>
        public IDatabase GetDatabase(int? db = null)
        {
            return GetConnection().GetDatabase(db ?? -1);
        }

        /// <summary>
        /// Obtain a configuration API for an individual server
        /// </summary>
        /// <param name="endPoint">The network endpoint</param>
        /// <returns>Redis server</returns>
        public IServer GetServer(EndPoint endPoint)
        {
            return GetConnection().GetServer(endPoint);
        }

        /// <summary>
        /// Gets all endpoints defined on the server
        /// </summary>
        /// <returns>Array of endpoints</returns>
        public EndPoint[] GetEndPoints()
        {
            return GetConnection().GetEndPoints();
        }

        /// <summary>
        /// Delete all the keys of the database
        /// </summary>
        /// <param name="db">Database number; pass null to use the default value</param>
        public void FlushDatabase(int? db = null)
        {
            var endPoints = GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                GetServer(endPoint).FlushDatabase(db ?? -1);
            }
        }

        /// <summary>
        /// Perform some action with Redis distributed lock
        /// </summary>
        /// <param name="resource">The thing we are locking on</param>
        /// <param name="expirationTime">The time after which the lock will automatically be expired by Redis</param>
        /// <param name="action">Action to be performed with locking</param>
        /// <returns>True if lock was acquired and action was performed; otherwise false</returns>
        public bool PerformActionWithLock(string resource, TimeSpan expirationTime, Action action)
        {
            // Use RedLock.net 2.x API (IRedLock from CreateLockAsync/CreateLock)
            using (var redisLock = RedisLockFactory.CreateLock(resource, expirationTime))
            {
                // Ensure that lock is acquired
                if (!redisLock.IsAcquired)
                    return false;

                // Perform action
                action();
                return true;
            }
        }

        /// <summary>
        /// Release all resources associated with this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose ConnectionMultiplexer
                _connection?.Dispose();

                // Dispose RedLockFactory
                _redisLockFactory?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
