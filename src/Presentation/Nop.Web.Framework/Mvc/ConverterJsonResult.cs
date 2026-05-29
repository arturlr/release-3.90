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
    public class ConverterJsonResult : ActionResult
    {
        private readonly JsonConverter[] _converters;

        public ConverterJsonResult(object data, params JsonConverter[] converters)
        {
            Data = data;
            _converters = converters;
            ContentType = MimeTypes.ApplicationJson;
        }

        public object Data { get; set; }
        public string ContentType { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var response = context.HttpContext.Response;
            response.ContentType = ContentType;

            if (Data != null)
            {
                var json = JsonConvert.SerializeObject(Data, _converters);
                await response.WriteAsync(json);
            }
        }
    }
}
