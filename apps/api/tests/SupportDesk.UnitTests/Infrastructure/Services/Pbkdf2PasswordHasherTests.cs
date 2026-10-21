using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_AcceptsTheOriginalPassword()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.True(_hasher.Verify(hash, "correct horse battery staple"));
    }

    [Fact]
    public void Verify_RejectsAnyOtherPassword()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.False(_hasher.Verify(hash, "Correct horse battery staple"));
        Assert.False(_hasher.Verify(hash, string.Empty));
    }

    [Fact]
    public void Hash_IsSaltedAndNeverContainsThePassword()
    {
        var first = _hasher.Hash("same-password");
        var second = _hasher.Hash("same-password");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("same-password", first);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2-sha256$1000$!!!$???")]
    public void Verify_ReturnsFalseForMissingOrMalformedHashes(string? storedHash)
    {
        Assert.False(_hasher.Verify(storedHash, "anything"));
    }
}
