using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Base nopCommerce model
    /// </summary>
    /// <remarks>
    /// Task 6.2 - TWO DELIBERATE CHANGES, both recorded in the task report:
    ///
    /// 1. The type-level <c>[ModelBinder(typeof(NopModelBinder))]</c> attribute was REMOVED.
    ///    <c>Microsoft.AspNetCore.Mvc.ModelBinderAttribute</c> exists and would still compile, but
    ///    its meaning is different and destructive here: in ASP.NET Core a type/parameter-level
    ///    <c>[ModelBinder]</c> makes MVC use <c>BinderTypeModelBinder</c>, which hands the WHOLE
    ///    model to the named binder and performs no property binding of its own. In MVC 5 the
    ///    attribute selected a <c>DefaultModelBinder</c> subclass that still did all the default
    ///    work. Keeping the attribute would therefore leave every nopCommerce model unbound.
    ///    The equivalent ASP.NET Core mechanism is an <see cref="IModelBinderProvider"/>, which
    ///    is what <c>NopModelBinderProvider</c> (in NopModelBinder.cs) now supplies; it has to be
    ///    registered by the host - see the deferral noted there.
    ///
    /// 2. <c>BindModel(ControllerContext, ModelBindingContext)</c> -&gt;
    ///    <see cref="BindModel(ModelBindingContext)"/>. <c>ControllerContext</c> is not available
    ///    during ASP.NET Core model binding (<see cref="ModelBindingContext"/> carries
    ///    <c>ActionContext</c> and <c>HttpContext</c> instead), and the types are not the MVC 5
    ///    ones. IMPORTANT BEHAVIOUR NOTE: MVC 5 invoked this hook from
    ///    <c>NopModelBinder.BindModel</c>, i.e. from inside the default complex-type binder.
    ///    ASP.NET Core exposes no seam that wraps the default complex-object binder
    ///    (<c>ComplexObjectModelBinder</c> is internal and <c>ComplexTypeModelBinder</c> is
    ///    obsolete), so THE FRAMEWORK NO LONGER CALLS THIS HOOK AUTOMATICALLY. It is retained as
    ///    an extension point. Verified before changing it: no type in the entire solution -
    ///    Nop.Web, Nop.Admin or any of the 20 plugins - overrides <c>BindModel</c>, so nothing
    ///    observable is lost today. Anything that needs it later should invoke it from an
    ///    action filter over <c>ActionExecutingContext.ActionArguments</c>.
    ///    <see cref="PostInitialize"/> is a constructor hook and is completely unaffected.
    /// </remarks>
    public partial class BaseNopModel
    {
        public BaseNopModel()
        {
            this.CustomProperties = new Dictionary<string, object>();
            PostInitialize();
        }

        public virtual void BindModel(ModelBindingContext bindingContext)
        {
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code to constructors
        /// </summary>
        protected virtual void PostInitialize()
        {
            
        }

        /// <summary>
        /// Use this property to store any custom value for your models. 
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; }
    }

    /// <summary>
    /// Base nopCommerce entity model
    /// </summary>
    public partial class BaseNopEntityModel : BaseNopModel
    {
        public virtual int Id { get; set; }
    }
}
