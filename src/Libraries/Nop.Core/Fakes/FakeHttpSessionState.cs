using System.Collections;
using System.Collections.Generic;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// Fake HTTP session state for use in scenarios where a real session is not available.
    /// </summary>
    public class FakeHttpSessionState
    {
        private readonly Dictionary<string, object> _sessionItems;

        public FakeHttpSessionState(Dictionary<string, object> sessionItems)
        {
            _sessionItems = sessionItems ?? new Dictionary<string, object>();
        }

        public virtual int Count
        {
            get { return _sessionItems.Count; }
        }

        public virtual ICollection Keys
        {
            get { return _sessionItems.Keys; }
        }

        public virtual object this[string name]
        {
            get
            {
                _sessionItems.TryGetValue(name, out var value);
                return value;
            }
            set { _sessionItems[name] = value; }
        }

        public bool Exists(string key)
        {
            return _sessionItems.ContainsKey(key);
        }

        public virtual void Add(string name, object value)
        {
            _sessionItems[name] = value;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return _sessionItems.GetEnumerator();
        }

        public virtual void Remove(string name)
        {
            _sessionItems.Remove(name);
        }
    }
}
