using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Task 6.2: <c>System.Web.Mvc.JsonResult</c> -&gt;
    /// <see cref="Microsoft.AspNetCore.Mvc.JsonResult"/> - same mappings as
    /// <see cref="ConverterJsonResult"/> (<c>ExecuteResult</c> -&gt; <c>ExecuteResultAsync</c>,
    /// <c>Data</c> -&gt; <see cref="JsonResult.Value"/>, <c>Response.Write</c> -&gt;
    /// <c>WriteAsync</c>, <c>ContentEncoding</c> dropped, explicit Newtonsoft serialization
    /// retained so the payload is byte-identical to 3.90).
    /// The emitted body is the four characters <c>null</c>, unchanged.
    /// </summary>
    public class NullJsonResult : JsonResult
    {
        public NullJsonResult()
            : base(null)
        {
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            //we do it as described here - http://stackoverflow.com/questions/15939944/jquery-post-json-fails-when-returning-null-from-asp-net-mvc

            var response = context.HttpContext.Response;
            response.ContentType = !String.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            this.Value = null;

            //If you need special handling, you can call another form of SerializeObject below
            var serializedObject = JsonConvert.SerializeObject(Value, Formatting.Indented);
            return response.WriteAsync(serializedObject);
        }
    }
}
