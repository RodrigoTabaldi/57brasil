using Brasil57.Domain;
namespace Brasil57.UnitTests;
public class CartQuantityPolicyTests
{
    [Theory]
    [InlineData(0, 1, 1, true)]
    [InlineData(1, 1, 1, false)]
    [InlineData(98, 1, 100, true)]
    [InlineData(99, 1, 100, false)]
    [InlineData(0, 0, 10, false)]
    [InlineData(0, -1, 10, false)]
    public void RespectsStockAndCartLimit(int current, int added, int available, bool expected)
        => Assert.Equal(expected, CartQuantityPolicy.CanAdd(current, added, available));
}
