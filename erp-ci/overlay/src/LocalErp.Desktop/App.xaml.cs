using System.IO;
using System.Windows;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Desktop.ViewModels;
using LocalErp.Infrastructure.Paths;
using LocalErp.Infrastructure.Time;
using LocalErp.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LocalErp.Desktop;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        var paths = AppDataPaths.CreateDefault();
        paths.EnsureCreated();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "localerp-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddSingleton(paths);
                services.AddSingleton<IClock, SystemClock>();
                services.AddLocalErpPersistence(Path.Combine(paths.DataDirectory, "localerp.db"));
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host.Start();
            Log.Information("Local ERP starting");

            var databaseConfigurator = _host.Services.GetRequiredService<SqliteDatabaseRuntimeConfigurator>();
            databaseConfigurator.ConfigureAsync().GetAwaiter().GetResult();
            Log.Information("SQLite runtime configured with WAL and foreign key enforcement");

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Show();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Fatal error during application startup");
            MessageBox.Show(
                "O Local ERP não conseguiu iniciar. Consulte os logs para detalhes técnicos.",
                "Falha ao iniciar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Local ERP shutting down");
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            _host.Dispose();
        }
        finally
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
