using FluentAssertions;
using Nop.Core.Caching;
using Xunit;

namespace Nop.Core.Tests;

public class CacheKeyTests
{
    [Fact]
    public void Constructor_SetsKeyAndPrefixes()
    {
        var key = new CacheKey("Nop.product.id-1", "Nop.product.");

        key.Key.Should().Be("Nop.product.id-1");
        key.Prefixes.Should().ContainSingle().Which.Should().Be("Nop.product.");
    }

    [Fact]
    public void CacheTime_DefaultsToZero()
    {
        var key = new CacheKey("test");
        key.CacheTime.Should().Be(0);
    }

    [Fact]
    public void CacheTime_CanBeSetViaInit()
    {
        var key = new CacheKey("test") { CacheTime = 30 };
        key.CacheTime.Should().Be(30);
    }

    [Fact]
    public void Prefixes_CanBeEmpty()
    {
        var key = new CacheKey("test");
        key.Prefixes.Should().BeEmpty();
    }

    [Fact]
    public void Prefixes_CanHaveMultiple()
    {
        var key = new CacheKey("test", "prefix1.", "prefix2.");
        key.Prefixes.Should().HaveCount(2);
    }
}
