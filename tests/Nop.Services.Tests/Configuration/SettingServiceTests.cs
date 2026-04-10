using FluentAssertions;
using Nop.Core.Configuration;
using Nop.Core.Domain.Configuration;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Tests;
using NSubstitute;
using Xunit;

namespace Nop.Services.Tests.Configuration;

public class SettingServiceTests
{
    private readonly SettingService _sut;
    private readonly FakeRepository<Setting> _repo = new();

    public SettingServiceTests()
    {
        _sut = new SettingService(_repo, new FakeCacheManager(), Substitute.For<IEventPublisher>());
    }

    [Fact]
    public async Task SetSetting_And_GetByKey()
    {
        await _sut.SetSettingAsync("test.key", "hello");

        var value = await _sut.GetSettingByKeyAsync<string>("test.key");
        value.Should().Be("hello");
    }

    [Fact]
    public async Task GetSettingByKey_NotFound_ReturnsDefault()
    {
        var value = await _sut.GetSettingByKeyAsync("missing.key", "fallback");
        value.Should().Be("fallback");
    }

    [Fact]
    public async Task SetSetting_OverwritesExisting()
    {
        await _sut.SetSettingAsync("key", "v1");
        await _sut.SetSettingAsync("key", "v2");

        var value = await _sut.GetSettingByKeyAsync<string>("key");
        value.Should().Be("v2");
    }

    [Fact]
    public async Task GetAllSettings_ReturnsAll()
    {
        await _sut.SetSettingAsync("a", "1");
        await _sut.SetSettingAsync("b", "2");

        var all = await _sut.GetAllSettingsAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteSetting_RemovesIt()
    {
        await _sut.SetSettingAsync("key", "val");
        var setting = await _sut.GetSettingAsync("key");
        setting.Should().NotBeNull();

        await _sut.DeleteSettingAsync(setting!);

        var result = await _sut.GetSettingByKeyAsync<string>("key");
        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task LoadSetting_ReturnsPopulatedObject()
    {
        await _sut.SetSettingAsync("testsettings.myvalue", "42");

        var settings = await _sut.LoadSettingAsync<TestSettings>();
        settings.MyValue.Should().Be("42");
    }

    [Fact]
    public async Task SaveSetting_PersistsAllProperties()
    {
        var settings = new TestSettings { MyValue = "saved" };
        await _sut.SaveSettingAsync(settings);

        var loaded = await _sut.LoadSettingAsync<TestSettings>();
        loaded.MyValue.Should().Be("saved");
    }

    private class TestSettings : ISettings
    {
        public string MyValue { get; set; } = "";
    }
}
