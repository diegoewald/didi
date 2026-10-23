using LocalErp.Infrastructure.Paths;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Infrastructure.Tests;

[TestClass]
public sealed class AppDataPathsTests
{
    [TestMethod]
    public void FromRootKeepsAllUserDataUnderConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "localerp-tests", Guid.NewGuid().ToString("N"));
        var paths = AppDataPaths.FromRoot(root);

        Assert.AreEqual(Path.GetFullPath(root), paths.RootDirectory);
        Assert.IsTrue(paths.DataDirectory.StartsWith(paths.RootDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(paths.LogsDirectory.StartsWith(paths.RootDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(paths.BackupsDirectory.StartsWith(paths.RootDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(paths.AttachmentsDirectory.StartsWith(paths.RootDirectory, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    [DoNotParallelize]
    public void CreateDefaultUsesExplicitRootOverrideWhenConfigured()
    {
        var root = Path.Combine(Path.GetTempPath(), "localerp-tests", Guid.NewGuid().ToString("N"));
        var original = Environment.GetEnvironmentVariable(AppDataPaths.RootOverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(AppDataPaths.RootOverrideEnvironmentVariable, root);

            var paths = AppDataPaths.CreateDefault();

            Assert.AreEqual(Path.GetFullPath(root), paths.RootDirectory);
            Assert.AreEqual(Path.Combine(Path.GetFullPath(root), "Data"), paths.DataDirectory);
            Assert.AreEqual(Path.Combine(Path.GetFullPath(root), "Logs"), paths.LogsDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppDataPaths.RootOverrideEnvironmentVariable, original);
        }
    }
}
