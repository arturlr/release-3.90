using System.Collections.Specialized;
using System.Text;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// Fake HTTP response for use in scenarios where a real HttpResponse is not available.
    /// </summary>
    public class FakeHttpResponse
    {
        private readonly NameValueCollection _cookies;
        private readonly StringBuilder _outputString = new StringBuilder();

        public FakeHttpResponse()
        {
            _cookies = new NameValueCollection();
        }

        public string ResponseOutput
        {
            get { return _outputString.ToString(); }
        }

        public virtual int StatusCode { get; set; }

        public virtual string RedirectLocation { get; set; }

        public virtual void Write(string s)
        {
            _outputString.Append(s);
        }

        public virtual string ApplyAppPathModifier(string virtualPath)
        {
            return virtualPath;
        }

        public virtual NameValueCollection Cookies
        {
            get { return _cookies; }
        }
    }
}
