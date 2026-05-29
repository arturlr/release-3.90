using Newtonsoft.Json;

namespace Nop.Web.Framework.Security.Captcha
{
    public class GReCaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error-codes")]
        public string[] ErrorCodes { get; set; }
    }
}
