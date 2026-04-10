using FluentAssertions;
using Nop.Core;
using Xunit;

namespace Nop.Core.Tests;

public class CommonHelperTests
{
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("test.user@sub.domain.com", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("not-an-email", false)]
    [InlineData("@missing-local.com", false)]
    public void IsValidEmail_ShouldValidateCorrectly(string? email, bool expected)
    {
        CommonHelper.IsValidEmail(email).Should().Be(expected);
    }

    [Fact]
    public void EnsureSubscriberEmailOrThrow_ValidEmail_ReturnsTrimmed()
    {
        CommonHelper.EnsureSubscriberEmailOrThrow("  user@example.com  ")
            .Should().Be("user@example.com");
    }

    [Fact]
    public void EnsureSubscriberEmailOrThrow_InvalidEmail_Throws()
    {
        var act = () => CommonHelper.EnsureSubscriberEmailOrThrow("bad");
        act.Should().Throw<NopException>();
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("::1", true)]
    [InlineData("not-ip", false)]
    [InlineData(null, false)]
    public void IsValidIpAddress_ShouldValidateCorrectly(string? ip, bool expected)
    {
        CommonHelper.IsValidIpAddress(ip).Should().Be(expected);
    }

    [Fact]
    public void GenerateRandomDigitCode_ReturnsCorrectLength()
    {
        CommonHelper.GenerateRandomDigitCode(6).Should().HaveLength(6);
    }

    [Fact]
    public void GenerateRandomDigitCode_ContainsOnlyDigits()
    {
        CommonHelper.GenerateRandomDigitCode(10).Should().MatchRegex(@"^\d+$");
    }

    [Theory]
    [InlineData(null, 5, null, "")]
    [InlineData("abc", 5, null, "abc")]
    [InlineData("abcdef", 3, null, "abc")]
    [InlineData("abcdef", 5, "...", "ab...")]
    public void EnsureMaximumLength_ShouldTruncateCorrectly(string? input, int max, string? postfix, string expected)
    {
        CommonHelper.EnsureMaximumLength(input, max, postfix).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc123def", "123")]
    [InlineData("no digits", "")]
    [InlineData(null, "")]
    public void EnsureNumericOnly_ShouldExtractDigits(string? input, string expected)
    {
        CommonHelper.EnsureNumericOnly(input).Should().Be(expected);
    }

    [Fact]
    public void EnsureNotNull_NullReturnsEmpty()
    {
        CommonHelper.EnsureNotNull(null).Should().BeEmpty();
    }

    [Fact]
    public void AreNullOrEmpty_MixedInput()
    {
        CommonHelper.AreNullOrEmpty("a", null, "b").Should().BeTrue();
        CommonHelper.AreNullOrEmpty("a", "b").Should().BeFalse();
    }

    [Fact]
    public void ArraysEqual_ShouldCompareCorrectly()
    {
        CommonHelper.ArraysEqual(new[] { 1, 2 }, new[] { 1, 2 }).Should().BeTrue();
        CommonHelper.ArraysEqual(new[] { 1 }, new[] { 2 }).Should().BeFalse();
        CommonHelper.ArraysEqual<int>(null, null).Should().BeTrue();
        CommonHelper.ArraysEqual(new[] { 1 }, null).Should().BeFalse();
    }

    [Fact]
    public void To_ConvertsStringToInt()
    {
        CommonHelper.To<int>("42").Should().Be(42);
    }

    [Theory]
    [InlineData("MyProperty", "My Property")]
    [InlineData("ABC", "A B C")]
    [InlineData("", "")]
    public void ConvertEnum_InsertsSpaces(string input, string expected)
    {
        CommonHelper.ConvertEnum(input).Should().Be(expected);
    }

    [Fact]
    public void GetDifferenceInYears_CalculatesCorrectly()
    {
        var start = new DateTime(2000, 6, 15);
        var end = new DateTime(2025, 6, 14);
        CommonHelper.GetDifferenceInYears(start, end).Should().Be(24);

        CommonHelper.GetDifferenceInYears(start, new DateTime(2025, 6, 15)).Should().Be(25);
    }
}
