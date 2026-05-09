using System.Collections.Generic;
using System.Linq;
using Moq;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Localization;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Localization
{
    [TestFixture]
    public class LanguageServiceTests : ServiceTest
    {
        private IRepository<Language> _languageRepo;
        private IStoreMappingService _storeMappingService;
        private ILanguageService _languageService;
        private ISettingService _settingService;
        private IEventPublisher _eventPublisher;
        private LocalizationSettings _localizationSettings;

        [SetUp]
        public new void SetUp()
        {
            var languageRepoMock = new Mock<IRepository<Language>>();
            var lang1 = new Language
            {
                Name = "English",
                LanguageCulture = "en-Us",
                FlagImageFileName = "us.png",
                Published = true,
                DisplayOrder = 1
            };
            var lang2 = new Language
            {
                Name = "Russian",
                LanguageCulture = "ru-Ru",
                FlagImageFileName = "ru.png",
                Published = true,
                DisplayOrder = 2
            };

            languageRepoMock.Setup(x => x.Table).Returns(new List<Language> { lang1, lang2 }.AsQueryable());
            _languageRepo = languageRepoMock.Object;

            _storeMappingService = new Mock<IStoreMappingService>().Object;

            var cacheManager = new NopNullCache();

            _settingService = new Mock<ISettingService>().Object;

            var eventPublisherMock = new Mock<IEventPublisher>();
            eventPublisherMock.Setup(x => x.Publish(It.IsAny<object>()));
            _eventPublisher = eventPublisherMock.Object;

            _localizationSettings = new LocalizationSettings();
            _languageService = new LanguageService(cacheManager, _languageRepo, _storeMappingService,
                _settingService, _localizationSettings, _eventPublisher);
        }

        [Test]
        public void Can_get_all_languages()
        {
            var languages = _languageService.GetAllLanguages();
            languages.ShouldNotBeNull();
            (languages.Any()).ShouldBeTrue();
        }
    }
}
