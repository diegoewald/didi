using System.Windows.Input;

namespace LocalErp.Desktop;

public enum ShellFocusRegion
{
    Navigation,
    Content,
    Account
}

public static class ShellKeyboardNavigation
{
    public static bool TryGetNavigationIndex(Key key, out int index)
    {
        index = key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            _ => -1
        };

        return index >= 0;
    }

    public static int GetNextTabIndex(int currentIndex, int itemCount, int direction)
    {
        if (itemCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount, "Tab count must be positive.");
        }

        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be -1 or 1.");
        }

        var normalizedCurrent = currentIndex >= 0 && currentIndex < itemCount ? currentIndex : 0;
        return (normalizedCurrent + direction + itemCount) % itemCount;
    }

    public static ShellFocusRegion GetNextFocusRegion(ShellFocusRegion currentRegion)
    {
        return currentRegion switch
        {
            ShellFocusRegion.Navigation => ShellFocusRegion.Content,
            ShellFocusRegion.Content => ShellFocusRegion.Account,
            ShellFocusRegion.Account => ShellFocusRegion.Navigation,
            _ => throw new ArgumentOutOfRangeException(nameof(currentRegion), currentRegion, "Unknown shell focus region.")
        };
    }
}
