using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Framework.UI
{
    /// <summary>
    /// Page head builder
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3. Two deliberate constraints on this interface (design §8):
    /// <list type="number">
    /// <item>
    /// <c>UrlHelper</c> becomes <see cref="IUrlHelper"/> — the only type change on the whole
    /// interface.
    /// </item>
    /// <item>
    /// The <c>excludeFromBundle</c> parameters and the <c>bundleFiles</c> parameters are RETAINED
    /// even though bundling has been dropped, so that ZERO view call sites in Nop.Web or
    /// Nop.Admin change and the interface stays source-compatible for plugins. They are
    /// <b>inert</b>: recorded but never consulted when markup is generated.
    /// </item>
    /// </list>
    /// </remarks>
    public partial interface IPageHeadBuilder
    {
        void AddTitleParts(string part);
        void AppendTitleParts(string part);
        string GenerateTitle(bool addDefaultTitle);

        void AddMetaDescriptionParts(string part);
        void AppendMetaDescriptionParts(string part);
        string GenerateMetaDescription();

        void AddMetaKeywordParts(string part);
        void AppendMetaKeywordParts(string part);
        string GenerateMetaKeywords();

        /// <param name="location">A location of the script element</param>
        /// <param name="part">Script part</param>
        /// <param name="excludeFromBundle">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        /// <param name="isAync">A value indicating whether to add an attribute "async" or not for js files</param>
        void AddScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAync);
        /// <param name="location">A location of the script element</param>
        /// <param name="part">Script part</param>
        /// <param name="excludeFromBundle">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        /// <param name="isAsync">A value indicating whether to add an attribute "async" or not for js files</param>
        void AppendScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAsync);
        /// <param name="urlHelper">URL helper</param>
        /// <param name="location">A location of the script element</param>
        /// <param name="bundleFiles">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        string GenerateScripts(IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null);

        /// <param name="location">A location of the script element</param>
        /// <param name="part">CSS part</param>
        /// <param name="excludeFromBundle">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        void AddCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false);
        /// <param name="location">A location of the script element</param>
        /// <param name="part">CSS part</param>
        /// <param name="excludeFromBundle">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        void AppendCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false);
        /// <param name="urlHelper">URL helper</param>
        /// <param name="location">A location of the script element</param>
        /// <param name="bundleFiles">INERT since the .NET 10 port — bundling was dropped (design §8)</param>
        string GenerateCssFiles(IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null);

        void AddCanonicalUrlParts(string part);
        void AppendCanonicalUrlParts(string part);
        string GenerateCanonicalUrls();

        void AddHeadCustomParts(string part);
        void AppendHeadCustomParts(string part);
        string GenerateHeadCustom();
        
        void AddPageCssClassParts(string part);
        void AppendPageCssClassParts(string part);
        string GeneratePageCssClasses();

        /// <summary>
        /// Specify "edit page" URL
        /// </summary>
        /// <param name="url">URL</param>
        void AddEditPageUrl(string url);
        /// <summary>
        /// Get "edit page" URL
        /// </summary>
        /// <returns>URL</returns>
        string GetEditPageUrl();

        /// <summary>
        /// Specify system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <param name="systemName">System name</param>
        void SetActiveMenuItemSystemName(string systemName);
        /// <summary>
        /// Get system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <returns>System name</returns>
        string GetActiveMenuItemSystemName();
    }
}
