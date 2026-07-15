using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    public class ConverterJsonResult : ActionResult
    {
        private readonly JsonConverter[] _converters;

        public ConverterJsonResult(params JsonConverter[] converters)
        {
            _converters = converters;
        }

        public object Data { get; set; }
        public string ContentType { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;
            response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            if (Data != null)
            {
                var json = JsonConvert.SerializeObject(Data, _converters);
                return response.WriteAsync(json);
            }

            return Task.CompletedTask;
        }
    }
}
