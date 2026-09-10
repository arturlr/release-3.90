using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Nop.Web.Extensions
{
    /// <summary>
    /// Stores and retrieves an object in <see cref="ISession"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Task 7.3.</b> <c>System.Web.HttpSessionState</c> had an <c>object</c> indexer and
    /// serialized transparently, so 3.90 could write
    /// <c>_httpContext.Session["OrderPaymentInfo"] = paymentInfo;</c> and read it back with a
    /// cast. ASP.NET Core's <see cref="ISession"/> is a <c>byte[]</c> store with no object
    /// indexer and no implicit serialization, so the conversion has to be explicit.
    /// </para>
    /// <para>
    /// This is the same treatment task 4.2 applied to
    /// <c>Nop.Services/Authentication/External/ExternalAuthorizerHelper</c> (UTF-8 JSON), and it
    /// is also what upstream nopCommerce 4.x does for exactly this call site.
    /// </para>
    /// <para>
    /// <b>Fails soft, like the 4.2 helper.</b> <see cref="HttpContext.Session"/> throws
    /// <c>InvalidOperationException</c> when the session feature is absent, and a payload written
    /// by an older build may no longer deserialize. Both are caught: <see cref="Get{T}"/> returns
    /// <c>null</c>/<c>default</c> and <see cref="Set{T}"/> is a no-op. A checkout that loses its
    /// stashed payment request restarts payment entry, which is the same outcome 3.90 produced
    /// when a session expired.
    /// </para>
    /// <para>
    /// <b>KNOWN FIDELITY LIMIT — recorded as deferral 7.3-2.</b> <c>ProcessPaymentRequest</c> —
    /// the only type stored here — carries
    /// <c>CustomValues</c> as a <c>Dictionary&lt;string, object&gt;</c>. JSON has no way to
    /// recover the original CLR type of an <c>object</c> value, so entries round-trip as
    /// <c>JsonElement</c> rather than as the type a payment plugin put in. Newtonsoft.Json
    /// behaves the same way (values come back as <c>JObject</c>/<c>JValue</c>) unless
    /// <c>TypeNameHandling</c> is enabled, which is a deserialization-gadget hazard that task
    /// 4.2 explicitly refused to introduce. Payment plugins that round-trip non-string
    /// <c>CustomValues</c> through the session must therefore be checked at tasks 12.1–12.5.
    /// </para>
    /// </remarks>
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            //3.90's session held live CLR objects, so property names never mattered. Keep the
            //declared names so a payload is readable and stable.
            PropertyNamingPolicy = null
        };

        /// <summary>
        /// Serialize a value into the session under <paramref name="key"/>. A <c>null</c> value
        /// removes the entry, reproducing 3.90's <c>Session["key"] = null</c>.
        /// </summary>
        public static void Set<T>(this ISession session, string key, T value)
        {
            if (session == null || string.IsNullOrEmpty(key))
                return;

            try
            {
                if (value == null)
                {
                    session.Remove(key);
                    return;
                }

                session.Set(key, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options)));
            }
            catch
            {
                //no session feature, or the value is not serializable - fail soft
            }
        }

        /// <summary>
        /// Deserialize the value stored under <paramref name="key"/>, or <c>default</c> when the
        /// entry is absent or unreadable.
        /// </summary>
        public static T Get<T>(this ISession session, string key)
        {
            if (session == null || string.IsNullOrEmpty(key))
                return default(T);

            try
            {
                byte[] bytes;
                if (!session.TryGetValue(key, out bytes) || bytes == null || bytes.Length == 0)
                    return default(T);

                return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes), Options);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
