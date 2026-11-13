namespace Brasil57.Domain;
public static class CartQuantityPolicy
{
    public static bool CanAdd(int current, int added, int available) =>
        current >= 0 && added is >= 1 and <= 99 && current + added <= 99 && current + added <= available;
}
