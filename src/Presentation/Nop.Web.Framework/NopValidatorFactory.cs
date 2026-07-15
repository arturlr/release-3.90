using System;
using FluentValidation;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
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

            // Look for a validator registered for this type
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
