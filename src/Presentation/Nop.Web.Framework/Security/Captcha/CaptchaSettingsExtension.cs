namespace Nop.Web.Framework.Security.Captcha
{
    public static class CaptchaSettingsExtension
    {
        public static bool CanShowInContactUsPage(this CaptchaSettings captchaSettings)
        {
            return captchaSettings.Enabled && captchaSettings.ShowOnContactUsPage;
        }
    }
}
