namespace LocalErp.Infrastructure.Paths;

public sealed record AppDataPaths(
    string RootDirectory,
    string DataDirectory,
    string LogsDirectory,
    string BackupsDirectory,
    string AttachmentsDirectory)
{
    public const string RootOverrideEnvironmentVariable = "LOCALERP_DATA_ROOT";

    public static AppDataPaths CreateDefault()
    {
        var configuredRoot = Environment.GetEnvironmentVariable(RootOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return FromRoot(configuredRoot);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = Path.Combine(localAppData, "LocalErp");

        return FromRoot(root);
    }

    public static AppDataPaths FromRoot(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        var fullRoot = Path.GetFullPath(rootDirectory);
        return new AppDataPaths(
            fullRoot,
            Path.Combine(fullRoot, "Data"),
            Path.Combine(fullRoot, "Logs"),
            Path.Combine(fullRoot, "Backups"),
            Path.Combine(fullRoot, "Attachments"));
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(AttachmentsDirectory);
    }
}
