using PlateGuard.Core.Helpers;

namespace PlateGuard.Core.Tests.Helpers;

public sealed class DeletePasswordHasherTests
{
    [Fact]
    public void Hash_IsDeterministicForSameInput()
    {
        var first = DeletePasswordHasher.Hash("admin");
        var second = DeletePasswordHasher.Hash("admin");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Verify_ReturnsTrueForMatchingPlainTextAndHash()
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
}
