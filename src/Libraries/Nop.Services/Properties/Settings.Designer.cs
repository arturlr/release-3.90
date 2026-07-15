namespace Nop.Services.Properties
{
    /// <summary>
    /// Application settings (replaces auto-generated settings from .NET Framework)
    /// </summary>
    internal sealed partial class Settings
    {
        private static Settings defaultInstance = new Settings();

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        public string Nop_Services_EuropaCheckVatService_checkVatService
        {
            get
            {
                return "https://ec.europa.eu/taxation_customs/vies/services/checkVatService";
            }
        }
    }
}
