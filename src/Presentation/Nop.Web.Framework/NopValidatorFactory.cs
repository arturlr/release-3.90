using System;
using FluentValidation;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Validator factory for resolving FluentValidation validators via DI
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class NopValidatorFactory : ValidatorFactoryBase
    {
        public override IValidator CreateInstance(Type validatorType)
        {
            try
            {
                var instance = EngineContext.Current.Resolve<IServiceProvider>();
                var validator = instance.GetService(validatorType);
                return validator as IValidator;
            }
            catch
            {
                return null;
            }
        }
    }
#pragma warning restore CS0618
}
