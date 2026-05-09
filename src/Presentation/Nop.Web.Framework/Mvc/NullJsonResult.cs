using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Web.Framework.Mvc
{
    public class NullJsonResult : JsonResult
    {
        public NullJsonResult() : base(null)
        {
            ContentType = MimeTypes.ApplicationJson;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var response = context.HttpContext.Response;
            response.ContentType = !String.IsNullOrEmpty(ContentType) ? ContentType : MimeTypes.ApplicationJson;

            var serializedObject = JsonConvert.SerializeObject(null, Formatting.Indented);
            await response.WriteAsync(serializedObject);
        }
    }
}
