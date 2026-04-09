using PlateGuard.Core.Helpers;

namespace PlateGuard.Core.Tests.Helpers;

public sealed class VehicleNumberNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("csa-4653", "CSA4653")]
    [InlineData(" CSA 4653 ", "CSA4653")]
    [InlineData("ab_12/cd", "AB12CD")]
    [InlineData("12-34-56", "123456")]
    [InlineData("ABC...123", "ABC123")]
    public void Normalize_ReturnsExpectedBusinessKey(string? input, string expected)
    {
        var result = VehicleNumberNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_PreservesLettersAndDigitsOnly()
    {
        var result = VehicleNumberNormalizer.Normalize(" a-b c_1/2.3 ");

        Assert.Equal("ABC123", result);
    }
}
