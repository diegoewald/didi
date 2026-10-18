using System.Globalization;
using System.IO;
using System.Windows;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Account;
using LocalErp.Application.Companies;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Desktop.ViewModels;
using LocalErp.Infrastructure.Paths;
using LocalErp.Infrastructure.Security;
using LocalErp.Infrastructure.Time;
using LocalErp.Persistence;
using LocalErp.Persistence.Development;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LocalErp.Desktop;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var paths = AppDataPaths.CreateDefault();
        paths.EnsureCreated();
        var isDevelopment = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            Environments.Development,
            StringComparison.OrdinalIgnoreCase);
        var databaseFileName = isDevelopment ? "localerp-dev.db" : "localerp.db";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(paths.LogsDirectory, "localerp-.log"),
                formatProvider: CultureInfo.InvariantCulture,
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
                services.AddSingleton<IPasswordHashService, MicrosoftPasswordHashService>();
                services.AddSingleton<IAuthenticationTimingProtector, PasswordHashTimingProtector>();
                services.AddSingleton<IUserSession, InMemoryUserSession>();
                services.AddLocalErpPersistence(Path.Combine(paths.DataDirectory, databaseFileName));
                services.AddTransient<CompanyService>();
                services.AddTransient<CompanyBranchService>();
                services.AddTransient<UserService>();
                services.AddTransient<AccountService>();
                services.AddTransient<AuthenticationService>();
                services.AddTransient<AdministratorBootstrapService>();
                services.AddTransient<StartupFlowService>();
                services.AddTransient<IAuthorizationService, AuthorizationService>();
                services.AddTransient<RoleService>();
                services.AddTransient<AdministratorBootstrapWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<CompaniesCatalogViewModel>();
                services.AddTransient<UsersAdministrationViewModel>();
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<MainWindow>();
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

            var environment = _host.Services.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                var seeder = _host.Services.GetRequiredService<DevelopmentDataSeeder>();
                var insertedRecords = seeder.SeedAsync().GetAwaiter().GetResult();
                Log.Information(
                    "Development sample data verified; {InsertedRecords} records inserted",
                    insertedRecords);
            }

            RunInteractiveShell();
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

    private void RunInteractiveShell()
    {
        while (true)
        {
            var startupFlow = _host.Services.GetRequiredService<StartupFlowService>();
            var route = startupFlow.DetermineRouteAsync().GetAwaiter().GetResult();
            Log.Information("Startup access route resolved as {StartupRoute}", route);

            switch (route)
            {
                case StartupRoute.AdministratorBootstrapRequired:
                    var bootstrapWindow = _host.Services.GetRequiredService<AdministratorBootstrapWindow>();
                    if (bootstrapWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }

                    continue;

                case StartupRoute.SignInRequired:
                    var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
                    if (loginWindow.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }

                    continue;

                case StartupRoute.Main:
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    MainWindow = mainWindow;
                    mainWindow.ShowDialog();

                    if (mainWindow.LogoutRequested)
                    {
                        continue;
                    }

                    Shutdown();
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported startup route '{route}'.");
            }
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
