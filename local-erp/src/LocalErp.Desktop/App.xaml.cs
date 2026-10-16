using System.Globalization;
using System.IO;
using System.Windows;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Companies;
using LocalErp.Application.Security;
using LocalErp.Application.Roles;
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
                services.AddSingleton<IUserSession, InMemoryUserSession>();
                services.AddLocalErpPersistence(Path.Combine(paths.DataDirectory, databaseFileName));
                services.AddTransient<CompanyService>();
                services.AddTransient<UserService>();
                services.AddTransient<AuthenticationService>();
                services.AddTransient<AdministratorBootstrapService>();
                services.AddTransient<StartupFlowService>();
                services.AddTransient<IAuthorizationService, AuthorizationService>();
                services.AddTransient<RoleService>();
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

            var environment = _host.Services.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                var seeder = _host.Services.GetRequiredService<DevelopmentDataSeeder>();
                var insertedRecords = seeder.SeedAsync().GetAwaiter().GetResult();
                Log.Information(
                    "Development sample data verified; {InsertedRecords} records inserted",
                    insertedRecords);
            }

            var startupFlow = _host.Services.GetRequiredService<StartupFlowService>();
            var route = startupFlow.DetermineRouteAsync().GetAwaiter().GetResult();
            Log.Information("Startup access route resolved as {StartupRoute}", route);

            switch (route)
            {
                case StartupRoute.AdministratorBootstrapRequired:
                    MessageBox.Show(
                        "Este banco ainda não possui um administrador. A criação do primeiro administrador é obrigatória antes de acessar o sistema. A interface de configuração inicial será habilitada no próximo marco.",
                        "Configuração inicial necessária",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown(2);
                    return;

                case StartupRoute.SignInRequired:
                    MessageBox.Show(
                        "É necessário autenticar-se para acessar o Local ERP. A interface de login será habilitada no próximo marco.",
                        "Autenticação necessária",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Shutdown(3);
                    return;

                case StartupRoute.Main:
                    var window = _host.Services.GetRequiredService<MainWindow>();
                    window.Show();
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported startup route '{route}'.");
            }
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
