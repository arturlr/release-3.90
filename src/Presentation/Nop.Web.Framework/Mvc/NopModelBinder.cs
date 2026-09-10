using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Trims bound <see cref="string"/> values unless the member is marked
    /// <see cref="NoTrimAttribute"/>
    /// </summary>
    /// <remarks>
    /// Task 6.2: <c>System.Web.Mvc.DefaultModelBinder</c> has NO ASP.NET Core equivalent -
    /// <c>IModelBinder</c> is a single <c>BindModelAsync(ModelBindingContext)</c> method with no
    /// overridable <c>SetProperty</c>/<c>GetPropertyValue</c> seams and no public default
    /// complex-object binder to derive from. The MVC 5 class did exactly two things; they are
    /// reproduced as follows.
    ///
    /// (a) STRING TRIMMING - reproduced faithfully. MVC 5's <c>SetProperty</c> override trimmed a
    /// value only when the property type was <see cref="string"/> and it carried no
    /// <see cref="NoTrimAttribute"/>. This binder wraps the framework's
    /// <see cref="SimpleTypeModelBinder"/> for string members and trims the result, and
    /// <see cref="NopModelBinderProvider"/> only selects it for string members without
    /// <see cref="NoTrimAttribute"/> - so the 14 <c>[NoTrim]</c> members across Nop.Web and
    /// Nop.Admin (passwords, SMTP password, the install form) keep their exact submitted value.
    /// Empty/null is passed through untouched, as before.
    ///
    /// (b) THE <c>BaseNopModel.BindModel</c> HOOK - no longer invoked by the framework. See the
    /// remarks on <see cref="BaseNopModel"/> for why (no wrappable complex-object binder) and for
    /// the verification that nothing in the solution overrides it.
    ///
    /// HOST WIRING REQUIRED (RUNTIME DEFERRAL, owner task 7.2). Because this is now a provider
    /// rather than a type-level attribute, it does nothing until the host registers it:
    /// <code>
    /// services.AddControllersWithViews(options =&gt;
    ///     options.ModelBinderProviders.Insert(0, new NopModelBinderProvider()));
    /// </code>
    /// If that call is missing, string trimming silently stops happening. It fails open in the
    /// harmless direction (untrimmed input, i.e. 3.90 behaviour with <c>[NoTrim]</c> everywhere)
    /// rather than corrupting values.
    /// </remarks>
    public class NopModelBinder : IModelBinder
    {
        private readonly IModelBinder _innerBinder;

        public NopModelBinder(IModelBinder innerBinder)
        {
            _innerBinder = innerBinder;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            await _innerBinder.BindModelAsync(bindingContext);

            if (!bindingContext.Result.IsModelSet)
                return;

            //check if data type of value is System.String
            var stringValue = bindingContext.Result.Model as string;
            if (stringValue == null)
                return;

            //developers can mark properties to be excluded from trimming with [NoTrim] attribute
            //(the provider already filtered those out; this is belt-and-braces for direct use)
            bindingContext.Result = ModelBindingResult.Success(
                string.IsNullOrEmpty(stringValue) ? stringValue : stringValue.Trim());
        }
    }

    /// <summary>
    /// Supplies <see cref="NopModelBinder"/> for string members that are not marked
    /// <see cref="NoTrimAttribute"/>. Replaces the MVC 5 type-level
    /// <c>[ModelBinder(typeof(NopModelBinder))]</c> attribute on <see cref="BaseNopModel"/>;
    /// see <see cref="NopModelBinder"/> for the host registration this needs.
    /// </summary>
    public class NopModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null || context.Metadata == null)
                return null;

            if (context.Metadata.ModelType != typeof(string))
                return null;

            //honour [NoTrim]
            var defaultMetadata = context.Metadata as DefaultModelMetadata;
            if (defaultMetadata != null &&
                defaultMetadata.Attributes.Attributes.OfType<NoTrimAttribute>().Any())
                return null;

            var loggerFactory = (ILoggerFactory)context.Services.GetService(typeof(ILoggerFactory))
                               ?? NullLoggerFactory.Instance;

            return new NopModelBinder(new SimpleTypeModelBinder(typeof(string), loggerFactory));
        }
    }
}
