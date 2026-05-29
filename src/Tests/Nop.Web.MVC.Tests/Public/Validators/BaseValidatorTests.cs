using Nop.Web.MVC.Tests;
using Nop.Services.Localization;
using NUnit.Framework;
using NSubstitute;

namespace Nop.Web.MVC.Tests.Public.Validators
{
    [TestFixture]
    public abstract class BaseValidatorTests
    {
        protected ILocalizationService _localizationService;
        
        [SetUp]
        public void Setup()
        {
            //set up localization service used by almost all validators
            _localizationService = Substitute.For<ILocalizationService>();
            _localizationService.GetResource(Arg.Any<string>()).Returns("Invalid");
        }
    }
}
