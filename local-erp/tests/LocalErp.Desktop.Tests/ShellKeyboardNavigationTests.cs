using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Desktop.Tests;

[TestClass]
public sealed class ShellKeyboardNavigationTests
{
    [TestMethod]
    [DataRow(Key.D1, 0)]
    [DataRow(Key.NumPad1, 0)]
    [DataRow(Key.D3, 2)]
    [DataRow(Key.NumPad8, 7)]
    public void TryGetNavigationIndexMapsSupportedSectionKeys(Key key, int expectedIndex)
    {
        var matched = ShellKeyboardNavigation.TryGetNavigationIndex(key, out var index);

        Assert.IsTrue(matched);
        Assert.AreEqual(expectedIndex, index);
    }

    [TestMethod]
    public void TryGetNavigationIndexRejectsUnsupportedKey()
    {
        var matched = ShellKeyboardNavigation.TryGetNavigationIndex(Key.D9, out var index);

        Assert.IsFalse(matched);
        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    [DataRow(0, 3, 1, 1)]
    [DataRow(2, 3, 1, 0)]
    [DataRow(0, 3, -1, 2)]
    [DataRow(-1, 3, 1, 1)]
    public void GetNextTabIndexCyclesWithinTabSet(int currentIndex, int itemCount, int direction, int expected)
    {
        var next = ShellKeyboardNavigation.GetNextTabIndex(currentIndex, itemCount, direction);

        Assert.AreEqual(expected, next);
    }

    [TestMethod]
    public void GetNextTabIndexRejectsInvalidDirection()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ShellKeyboardNavigation.GetNextTabIndex(0, 3, 0));
    }

    [TestMethod]
    public void GetNextTabIndexRejectsEmptyTabSet()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ShellKeyboardNavigation.GetNextTabIndex(0, 0, 1));
    }

    [TestMethod]
    [DataRow(ShellFocusRegion.Navigation, ShellFocusRegion.Content)]
    [DataRow(ShellFocusRegion.Content, ShellFocusRegion.Account)]
    [DataRow(ShellFocusRegion.Account, ShellFocusRegion.Navigation)]
    public void GetNextFocusRegionCyclesShellRegions(ShellFocusRegion current, ShellFocusRegion expected)
    {
        var next = ShellKeyboardNavigation.GetNextFocusRegion(current);

        Assert.AreEqual(expected, next);
    }
}
