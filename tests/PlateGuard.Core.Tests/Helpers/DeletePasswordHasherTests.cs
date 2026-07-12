using PlateGuard.Core.Helpers;

namespace PlateGuard.Core.Tests.Helpers;

public sealed class DeletePasswordHasherTests
{
    private const string LegacyAdminHash = "8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918";

    [Fact]
    public void Hash_UsesSelfDescribingPbkdf2FormatWithRandomSalt()
    {
        var first = DeletePasswordHasher.Hash("admin");
        var second = DeletePasswordHasher.Hash("admin");

        Assert.StartsWith("PBKDF2$210000$", first);
        Assert.StartsWith("PBKDF2$210000$", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ReturnsTrueForMatchingPlainTextAndPbkdf2Hash()
    {
        var hash = DeletePasswordHasher.Hash("secret");

        var result = DeletePasswordHasher.Verify("secret", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalseForMissingHash()
    {
        var result = DeletePasswordHasher.Verify("secret", null);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalseForDifferentValue()
    {
        var hash = DeletePasswordHasher.Hash("secret");

        var result = DeletePasswordHasher.Verify("different", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsTrueForLegacySha256Hash()
    {
        var result = DeletePasswordHasher.Verify("admin", LegacyAdminHash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalseForMalformedPbkdf2WithoutThrowing()
    {
        var result = DeletePasswordHasher.Verify("admin", "PBKDF2$bad$salt$hash");

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPbkdf2Password()
    {
        var hash = DeletePasswordHasher.Hash("secret");

        var result = DeletePasswordHasher.Verify("wrong", hash);

        Assert.False(result);
    }

    [Fact]
    public void NeedsUpgrade_ReturnsTrueOnlyForLegacyStoredHashes()
    {
        Assert.True(DeletePasswordHasher.NeedsUpgrade(LegacyAdminHash));
        Assert.False(DeletePasswordHasher.NeedsUpgrade(DeletePasswordHasher.Hash("admin")));
        Assert.False(DeletePasswordHasher.NeedsUpgrade(null));
    }
}
