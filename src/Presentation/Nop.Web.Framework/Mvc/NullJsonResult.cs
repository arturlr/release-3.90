using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    public class NullJsonResult : ContentResult
    {
        public NullJsonResult()
        {
            Content = JsonConvert.SerializeObject(null, Formatting.Indented);
            ContentType = MimeTypes.ApplicationJson;
        }
    }
}
