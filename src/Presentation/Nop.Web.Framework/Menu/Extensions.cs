using System.Linq;

namespace Nop.Web.Framework.Menu
{
    public static class Extensions
    {
        public static bool ContainsSystemName(this SiteMapNode node, string systemName)
        {
            if (node == null) return false;
            if (node.SystemName == systemName) return true;
            return node.ChildNodes.Any(child => ContainsSystemName(child, systemName));
        }
    }
}
