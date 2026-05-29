using System.Net.Http;
using Newtonsoft.Json;

namespace Nop.Web.Framework.Security.Captcha
{
    public class GReCaptchaValidator
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public string SecretKey { get; set; }
        public string Response { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrEmpty(SecretKey) || string.IsNullOrEmpty(Response))
                return false;

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("secret", SecretKey),
                    new System.Collections.Generic.KeyValuePair<string, string>("response", Response)
                });

                var result = client.PostAsync(VerifyUrl, content).GetAwaiter().GetResult();
                var json = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var response = JsonConvert.DeserializeObject<GReCaptchaResponse>(json);
                return response?.Success ?? false;
            }
        }
    }
}
