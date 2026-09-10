using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Represents custom JsonResult with using Json converters
    /// </summary>
    /// <remarks>
    /// Task 6.2: <c>System.Web.Mvc.JsonResult</c> -&gt;
    /// <see cref="Microsoft.AspNetCore.Mvc.JsonResult"/>.
    /// <list type="bullet">
    /// <item><c>ExecuteResult(ControllerContext)</c> -&gt;
    /// <c>ExecuteResultAsync(ActionContext)</c> - ASP.NET Core action results are async only.</item>
    /// <item><c>JsonResult.Data</c> -&gt; <see cref="JsonResult.Value"/>. ASP.NET Core's
    /// <see cref="JsonResult"/> has no parameterless constructor, hence <c>base(null)</c>.</item>
    /// <item><c>Response.Write(string)</c> -&gt; <c>Response.WriteAsync(string)</c>.</item>
    /// <item><c>JsonResult.ContentEncoding</c> was DROPPED - ASP.NET Core has no
    /// <c>Response.ContentEncoding</c>; response bodies are UTF-8 and any other charset must be
    /// declared through <see cref="JsonResult.ContentType"/>. This is a PUBLIC SIGNATURE CHANGE
    /// (an inherited settable property disappears); no in-tree call site set it.</item>
    /// <item>The explicit Newtonsoft serialization is DELIBERATELY RETAINED rather than delegated
    /// to <see cref="JsonResult.SerializerSettings"/>: the whole purpose of this type is to apply
    /// caller-supplied <see cref="JsonConverter"/> instances, and
    /// <see cref="JsonResult.SerializerSettings"/> is interpreted by whichever output formatter
    /// the host configured (<c>JsonSerializerOptions</c> for System.Text.Json,
    /// <c>JsonSerializerSettings</c> for Newtonsoft), so it would throw if the host's choice did
    /// not match. Serializing here keeps the emitted payload identical to 3.90 - in particular
    /// PascalCase property names - independently of host configuration.</item>
    /// </list>
    /// </remarks>
    public class ConverterJsonResult : JsonResult
    {
        #region Fields

        private readonly JsonConverter[] _converters;

        #endregion

        #region Ctor

        public ConverterJsonResult(params JsonConverter[] converters)
            : base(null)
        {
            _converters = converters;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Enables processing of the result of an action method
        /// </summary>
        /// <param name="context">The context within which the result is executed</param>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (context.HttpContext == null || context.HttpContext.Response == null)
                return Task.CompletedTask;

            context.HttpContext.Response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            //serialize data with any converters
            if (Value != null)
                return context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(Value, _converters));

            return Task.CompletedTask;
        }

        #endregion
    }
}
