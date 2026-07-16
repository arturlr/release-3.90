using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Static localization helper for Razor views.
    /// Call as NopLocalize.T("key") or just T("key") via @using static Nop.Web.Framework.Localization.NopLocalize
    /// </summary>
    public static class NopLocalize
    {
        public static LocalizedString T(string format, params object[] args)
        {
            try
            {
                if (DataSettingsHelper.DatabaseIsInstalled())
                {
                    var locService = EngineContext.Current.Resolve<ILocalizationService>();
                    var resFormat = locService.GetResource(format);
                    if (!string.IsNullOrEmpty(resFormat))
                        return new LocalizedString((args == null || args.Length == 0) ? resFormat : string.Format(resFormat, args));
                }
            }
            catch { }
            return new LocalizedString(format);
        }
    }
}
