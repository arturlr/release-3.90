using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    public class NullJsonResult : ActionResult
    {
        public string ContentType { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var response = context.HttpContext.Response;
            response.ContentType = !string.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            var serializedObject = JsonConvert.SerializeObject(null, Formatting.Indented);
            await response.WriteAsync(serializedObject);
        }
    }
}
