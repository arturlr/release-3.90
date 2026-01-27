using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Web.Framework.Validators
{
    /// <summary>
    /// Validator factory for FluentValidation
    /// </summary>
    public class NopValidatorFactory : IValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public NopValidatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IValidator<T>? GetValidator<T>()
        {
            return _serviceProvider.GetService<IValidator<T>>();
        }

        public IValidator? GetValidator(Type type)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            return _serviceProvider.GetService(validatorType) as IValidator;
        }
    }
}
