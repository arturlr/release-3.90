using System.Collections.Generic;

namespace Nop.Web.Framework.UI
{
    /// <summary>
    /// "As is" bundle orderer - preserves the original order of files.
    /// In ASP.NET Core, bundling is handled externally (e.g., BundlerMinifier, Webpack).
    /// This class is retained for compatibility with code that references it.
    /// </summary>
    public partial class AsIsBundleOrderer
    {
        public virtual IEnumerable<string> OrderFiles(IEnumerable<string> files)
        {
            return files;
        }
    }
}
