using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;


namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Represents custom JsonResult with using Json converters
    /// </summary>
    public class ConverterJsonResult : JsonResult
    {
        #region Fields

        private readonly JsonConverter[] _converters;

        #endregion

        #region Ctor

        public ConverterJsonResult(params JsonConverter[] converters) : base(null)
        {
            _converters = converters;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Enables processing of the result of an action method
        /// </summary>
        /// <param name="context">The context within which the result is executed</param>
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.HttpContext == null || context.HttpContext.Response == null)
                return;

            context.HttpContext.Response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            //serialize data with any converters
            if (Value != null)
                await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(Value, _converters));
        }

        #endregion
    }
}
