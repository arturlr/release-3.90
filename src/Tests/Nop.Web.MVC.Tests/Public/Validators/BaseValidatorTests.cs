using Moq;
using Nop.Services.Localization;
using NUnit.Framework;

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
            var localizationServiceMock = new Mock<ILocalizationService>();
            localizationServiceMock.Setup(l => l.GetResource(It.IsAny<string>())).Returns("Invalid");
            _localizationService = localizationServiceMock.Object;
        }
    }
}
