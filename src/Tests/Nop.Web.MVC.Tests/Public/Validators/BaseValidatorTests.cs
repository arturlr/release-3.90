using Nop.Services.Localization;
using NSubstitute;
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
            //set up localziation service used by almost all validators
            //Task 17.1: RhinoMocks -> NSubstitute. GenerateMock<T>() -> Substitute.For<T>();
            //.Expect(l => l.GetResource("")).Return("Invalid").IgnoreArguments() ->
            //GetResource(Arg.Any<string>()).Returns("Invalid"). IgnoreArguments() collapses into
            //the Arg.Any<string>() matcher, so any resource key returns "Invalid" as before.
            _localizationService = Substitute.For<ILocalizationService>();
            _localizationService.GetResource(Arg.Any<string>()).Returns("Invalid");
        }
    }
}
