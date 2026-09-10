using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// The ASP.NET Core equivalent of 3.90's
    /// <c>DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false</c>
    /// (<c>Global.asax.cs</c> line 71, task 7.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// MVC 5 had a single static switch that stopped it from synthesising a <c>[Required]</c>
    /// validator for every non-nullable value-typed member. nopCommerce turned it off, so a blank
    /// <c>int</c>/<c>decimal</c>/<c>bool</c> field produced no validation error and FluentValidation
    /// was left as the single source of truth for what is mandatory.
    /// </para>
    /// <para>
    /// ASP.NET Core has no such switch. Its <c>DataAnnotationsMetadataProvider</c> sets
    /// <c>ValidationMetadata.IsRequired = true</c> for any member whose type is a non-nullable
    /// value type, which MVC then turns into an implicit required check. Left alone, that is a
    /// <b>behaviour regression against 3.90 across the whole storefront</b> — registration,
    /// checkout and every admin form would start rejecting input that 3.90 accepted, with messages
    /// no nopCommerce validator produced.
    /// </para>
    /// <para>
    /// The supported way to undo it is a later <see cref="IValidationMetadataProvider"/> that
    /// clears the flag. Providers run in list order, and <c>AddControllersWithViews(configure)</c>
    /// applies the caller's <c>MvcOptions</c> delegate <i>after</i> the framework's own option
    /// setups, so a provider added from <c>AddNopFramework(..., configureMvc: ...)</c> is
    /// guaranteed to run after <c>DataAnnotationsMetadataProvider</c> and therefore wins.
    /// </para>
    /// <para>
    /// <b>Explicit <c>[Required]</c> is respected.</b> The MVC 5 switch only suppressed the
    /// <i>implicit</i> attribute; a member that declares <c>[Required]</c> was still required.
    /// That is why this provider bails out when a <see cref="RequiredAttribute"/> is present.
    /// </para>
    /// </remarks>
    public class SuppressImplicitRequiredValueTypeMetadataProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            //only members, never the top-level model type
            if (context.Key.MetadataKind == ModelMetadataKind.Type)
                return;

            var modelType = context.Key.ModelType;
            if (modelType == null || !modelType.IsValueType || Nullable.GetUnderlyingType(modelType) != null)
                return;

            //an explicitly declared [Required] must keep working
            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata)
            {
                if (attribute is RequiredAttribute)
                    return;
            }

            context.ValidationMetadata.IsRequired = false;
        }
    }
}
