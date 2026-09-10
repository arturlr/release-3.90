using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Nop.Core;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// This metadata provider adds some functionality on top of the default ASP.NET Core
    /// data-annotations metadata. It adds custom attributes (implementing
    /// <see cref="IModelAttribute"/>) to the <c>AdditionalValues</c> property of the model's
    /// metadata so that it can be retrieved later.
    /// </summary>
    /// <remarks>
    /// Task 6.2: <c>System.Web.Mvc.DataAnnotationsModelMetadataProvider</c> -&gt;
    /// <see cref="IDisplayMetadataProvider"/>. THIS IS A DIFFERENT CONTRACT, NOT A RENAME:
    /// <list type="bullet">
    /// <item>MVC 5 had a single provider class that CREATED the whole <c>ModelMetadata</c> object,
    /// and customisation meant subclassing it and overriding <c>CreateMetadata(...)</c>.
    /// ASP.NET Core splits metadata into three independent, additive provider interfaces -
    /// <see cref="IBindingMetadataProvider"/>, <see cref="IDisplayMetadataProvider"/> and
    /// <see cref="IValidationMetadataProvider"/> - each contributing to a metadata details object
    /// the framework owns. There is no base class and nothing to call <c>base</c> on; this type
    /// therefore implements an interface instead of deriving, and only contributes the
    /// <c>AdditionalValues</c> entries. The default data-annotations behaviour it used to inherit
    /// is now supplied by the framework's own <c>DataAnnotationsMetadataProvider</c>, which
    /// remains registered.</item>
    /// <item><c>CreateMetadata(IEnumerable&lt;Attribute&gt;, Type containerType,
    /// Func&lt;object&gt; modelAccessor, Type modelType, string propertyName)</c> -&gt;
    /// <c>CreateDisplayMetadata(DisplayMetadataProviderContext)</c>. The five loose parameters are
    /// folded into the context (<c>context.Attributes</c>, <c>context.Key</c>), and the
    /// <c>modelAccessor</c> concept is gone entirely - ASP.NET Core metadata is per-type/member
    /// and value-free, computed once and cached.</item>
    /// <item><c>ModelMetadata.AdditionalValues</c> was <c>IDictionary&lt;string, object&gt;</c>;
    /// <see cref="DisplayMetadata.AdditionalValues"/> is
    /// <c>IDictionary&lt;object, object&gt;</c> (the read side,
    /// <c>ModelMetadata.AdditionalValues</c>, is <c>IReadOnlyDictionary&lt;object, object&gt;</c>).
    /// The keys written here are still the same strings - <c>"NopResourceDisplayName"</c> and
    /// <c>"AdditionalInfo"</c> - so existing lookups by string key continue to resolve; only the
    /// declared key type widened.</item>
    /// <item>Because ASP.NET Core CACHES metadata per member, the duplicate-name
    /// <see cref="NopException"/> now fires once at first use of a member rather than on every
    /// metadata creation. Same condition, same message.</item>
    /// </list>
    /// HOST WIRING REQUIRED (RUNTIME DEFERRAL, owner task 7.2). 3.90 installed this from
    /// <c>Global.asax.cs</c> line 66 (<c>ModelMetadataProviders.Current = new NopMetadataProvider();</c>),
    /// which task 7.2 deletes. The ASP.NET Core equivalent is:
    /// <code>
    /// services.AddControllersWithViews(options =&gt;
    ///     options.ModelMetadataDetailsProviders.Add(new NopMetadataProvider()));
    /// </code>
    /// Until that is added, <c>AdditionalValues</c> stays empty, so any HTML helper or template
    /// that reads <c>NopResourceDisplayName</c>/<c>AdditionalInfo</c> out of the metadata falls
    /// back to its default. Note this is ADDITIVE - it no longer replaces the default provider,
    /// so ordinary display names and validation metadata work with or without the registration.
    /// </remarks>
    public class NopMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context == null)
                return;

            var additionalValues = context.Attributes.OfType<IModelAttribute>().ToList();
            foreach (var additionalValue in additionalValues)
            {
                if (context.DisplayMetadata.AdditionalValues.ContainsKey(additionalValue.Name))
                    throw new NopException("There is already an attribute with the name of \"" + additionalValue.Name +
                                           "\" on this model.");
                context.DisplayMetadata.AdditionalValues.Add(additionalValue.Name, additionalValue);
            }
        }
    }
}
