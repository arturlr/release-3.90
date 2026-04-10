using FluentAssertions;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using Xunit;

namespace Nop.Services.Tests.Security;

public class EncryptionServiceTests
{
    // 24-char key: first 16 bytes for AES key, bytes 8-24 for IV
    private const string TestKey = "0123456789abcdef01234567";
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        _sut = new EncryptionService(new SecuritySettings { EncryptionKey = TestKey });
    }

    [Fact]
    public void CreateSaltKey_ReturnsBase64OfRequestedSize()
    {
        var salt = _sut.CreateSaltKey(8);
        salt.Should().NotBeNullOrEmpty();
        // Base64 of 8 bytes = 12 chars
        Convert.FromBase64String(salt).Should().HaveCount(8);
    }

    [Fact]
    public void CreatePasswordHash_IsDeterministic()
    {
        var hash1 = _sut.CreatePasswordHash("password", "salt");
        var hash2 = _sut.CreatePasswordHash("password", "salt");
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CreatePasswordHash_DifferentSalts_DifferentHashes()
    {
        var hash1 = _sut.CreatePasswordHash("password", "salt1");
        var hash2 = _sut.CreatePasswordHash("password", "salt2");
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData("SHA1")]
    [InlineData("SHA256")]
    [InlineData("SHA512")]
    [InlineData("MD5")]
    public void CreateHash_SupportedAlgorithms(string algorithm)
    {
        var hash = _sut.CreateHash("test"u8.ToArray(), algorithm);
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateHash_UnknownAlgorithm_Throws()
    {
        var act = () => _sut.CreateHash("test"u8.ToArray(), "UNKNOWN");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EncryptText_DecryptText_Roundtrip()
    {
        var plainText = "Hello, World!";
        var encrypted = _sut.EncryptText(plainText);
        var decrypted = _sut.DecryptText(encrypted);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void EncryptText_EmptyString_ReturnsEmpty()
    {
        _sut.EncryptText("").Should().BeEmpty();
    }

    [Fact]
    public void DecryptText_EmptyString_ReturnsEmpty()
    {
        _sut.DecryptText("").Should().BeEmpty();
    }

    [Fact]
    public void EncryptText_WithExplicitKey_Roundtrip()
    {
        var key = "abcdefghijklmnopqrstuvwx";
        var encrypted = _sut.EncryptText("secret", key);
        var decrypted = _sut.DecryptText(encrypted, key);
        decrypted.Should().Be("secret");
    }
}
