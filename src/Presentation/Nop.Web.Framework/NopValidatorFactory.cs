using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Resolves the FluentValidation validator declared by a model's
    /// <see cref="ValidatorAttribute"/> through the nopCommerce container
    /// </summary>
    /// <remarks>
    /// Task 6.2: THIS CLASS IS UNCHANGED - it compiles as-is on net10.0 and needed no edit.
    /// FluentValidation stays on 7.6.105 per the user decision recorded against task 6.1; it is
    /// not upgraded here.
    ///
    /// What did have to change is the MVC HOOK-UP. 3.90 wired FluentValidation into MVC 5 from
    /// <c>Nop.Web/Global.asax.cs</c> line 74:
    /// <code>
    /// ModelValidatorProviders.Providers.Add(
    ///     new FluentValidationModelValidatorProvider(new NopValidatorFactory()));
    /// </code>
    /// <c>FluentValidationModelValidatorProvider</c> comes from the <c>FluentValidation.Mvc</c>
    /// assembly, which is MVC5-only. FluentValidation 7.x has NO ASP.NET Core integration package
    /// (<c>FluentValidation.AspNetCore</c> starts at 8.x), and upgrading FluentValidation is
    /// explicitly out of scope. The integration is therefore hand-wired below as a plain
    /// <see cref="IModelValidatorProvider"/> over the framework's own validation contract - no new
    /// package, no FluentValidation version change.
    ///
    /// HOST WIRING REQUIRED (RUNTIME DEFERRAL, owner task 7.2, which deletes Global.asax.cs):
    /// <code>
    /// services.AddControllersWithViews(options =&gt;
    ///     options.ModelValidatorProviders.Add(
    ///         new NopFluentValidationModelValidatorProvider(new NopValidatorFactory())));
    /// </code>
    /// SECURITY-RELEVANT IF OMITTED: FluentValidation rules are nopCommerce's server-side input
    /// validation for login, registration, change-password, checkout and every admin form. If the
    /// provider is not registered, those validators never execute and <c>ModelState.IsValid</c>
    /// reports true for input they would have rejected. DataAnnotations validation still runs
    /// (the framework's own provider is unaffected), so this is a partial gap rather than a total
    /// one - but it MUST be closed at 7.2.
    /// </remarks>
    public class NopValidatorFactory : AttributedValidatorFactory
    {
        //private readonly InstanceCache _cache = new InstanceCache();
        public override IValidator GetValidator(Type type)
        {
            if (type != null)
            {
                var attribute = (ValidatorAttribute)Attribute.GetCustomAttribute(type, typeof(ValidatorAttribute));
                if ((attribute != null) && (attribute.ValidatorType != null))
                {
                    //validators can depend on some customer specific settings (such as working language)
                    //that's why we do not cache validators
                    //var instance = _cache.GetOrCreateInstance(attribute.ValidatorType,
                    //                           x => EngineContext.Current.ContainerManager.ResolveUnregistered(x));
                    var instance = EngineContext.Current.ContainerManager.ResolveUnregistered(attribute.ValidatorType);
                    return instance as IValidator;
                }
            }
            return null;

        }
    }

    /// <summary>
    /// Runs FluentValidation validators as part of ASP.NET Core model validation.
    /// Replaces <c>FluentValidation.Mvc.FluentValidationModelValidatorProvider</c> (MVC5 only).
    /// See <see cref="NopValidatorFactory"/> for the registration this needs.
    /// </summary>
    public class NopFluentValidationModelValidatorProvider : IModelValidatorProvider
    {
        private readonly IValidatorFactory _validatorFactory;

        public NopFluentValidationModelValidatorProvider(IValidatorFactory validatorFactory)
        {
            if (validatorFactory == null)
                throw new ArgumentNullException("validatorFactory");

            _validatorFactory = validatorFactory;
        }

        public void CreateValidators(ModelValidatorProviderContext context)
        {
            if (context == null || context.ModelMetadata == null)
                return;

            //FluentValidation validates a whole object, so attach only at the top level
            //(action parameter or model type), never per-property - that would re-run the whole
            //validator once per property and duplicate every error
            if (context.ModelMetadata.MetadataKind == ModelMetadataKind.Property)
                return;

            var modelType = context.ModelMetadata.ModelType;
            if (modelType == null)
                return;

            var validator = _validatorFactory.GetValidator(modelType);
            if (validator == null)
                return;

            context.Results.Add(new ValidatorItem
            {
                Validator = new NopFluentValidationModelValidator(validator),
                //NopValidatorFactory deliberately does not cache validators (they can depend on
                //the working language), so the wrapper must not be cached either
                IsReusable = false
            });
        }
    }

    /// <summary>
    /// Adapts a FluentValidation <see cref="IValidator"/> onto
    /// <see cref="IModelValidator"/>
    /// </summary>
    public class NopFluentValidationModelValidator : IModelValidator
    {
        private readonly IValidator _validator;

        public NopFluentValidationModelValidator(IValidator validator)
        {
            if (validator == null)
                throw new ArgumentNullException("validator");

            _validator = validator;
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            if (context == null || context.Model == null)
                return Enumerable.Empty<ModelValidationResult>();

            var result = _validator.Validate(context.Model);
            if (result == null || result.IsValid)
                return Enumerable.Empty<ModelValidationResult>();

            //an empty member name binds the error to the model itself, which is what
            //FluentValidation.Mvc did for object-level rules
            return result.Errors
                .Select(failure => new ModelValidationResult(failure.PropertyName ?? string.Empty,
                    failure.ErrorMessage))
                .ToList();
        }
    }
}
