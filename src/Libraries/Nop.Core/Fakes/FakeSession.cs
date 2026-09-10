using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// An in-memory <see cref="ISession"/> for use with <see cref="FakeHttpContext"/>
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 5): replaces the removed <c>FakeHttpSessionState</c>, which
    /// derived from <c>System.Web.HttpSessionStateBase</c> and stored arbitrary objects in a
    /// <c>SessionStateItemCollection</c>. ASP.NET Core's session contract is byte-oriented, so
    /// seed values supplied as objects are stored as their UTF-8 <c>ToString()</c>
    /// representation.
    /// </remarks>
    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _items =
            new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public FakeSession()
            : this(null)
        {
        }

        public FakeSession(IDictionary<string, object> sessionItems)
        {
            Id = Guid.NewGuid().ToString("N");

            if (sessionItems == null)
                return;

            foreach (var item in sessionItems.Where(i => !string.IsNullOrEmpty(i.Key)))
            {
                _items[item.Key] = item.Value == null
                    ? new byte[0]
                    : Encoding.UTF8.GetBytes(item.Value.ToString());
            }
        }

        public bool IsAvailable
        {
            get { return true; }
        }

        public string Id { get; private set; }

        public IEnumerable<string> Keys
        {
            get { return _items.Keys; }
        }

        public void Clear()
        {
            _items.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _items.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _items[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _items.TryGetValue(key, out value);
        }
    }
}
