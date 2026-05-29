namespace Nop.Web.Framework.Localization
{
    public class LocalizedString
    {
        private readonly string _localized;

        public LocalizedString(string localized)
        {
            _localized = localized;
        }

        public string Text => _localized;

        public static implicit operator string(LocalizedString obj)
        {
            return obj.Text;
        }

        public override string ToString()
        {
            return _localized;
        }
    }
}
