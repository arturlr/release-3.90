using System;
using FluentValidation;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Validator factory that resolves validators from the DI container.
    /// FluentValidation 11.x removed AttributedValidatorFactory and ValidatorAttribute.
    /// This implementation uses the IoC container to resolve validators by convention.
    /// </summary>
    public class NopValidatorFactory : IValidatorFactory
    {
        public IValidator<T> GetValidator<T>()
        {
            return (IValidator<T>)GetValidator(typeof(T));
        }

        public IValidator GetValidator(Type type)
        {
            if (type == null)
                return null;

            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            try
            {
                var instance = EngineContext.Current.ContainerManager.ResolveUnregistered(validatorType);
                return instance as IValidator;
            }
            catch
            {
                return null;
            }
        }
    }
}
