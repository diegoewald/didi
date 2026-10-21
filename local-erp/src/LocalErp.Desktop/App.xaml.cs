using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Account;
using LocalErp.Application.Categories;
using LocalErp.Application.Companies;
using LocalErp.Application.CostCenters;
using LocalErp.Application.Materials;
using LocalErp.Application.Products;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using LocalErp.Application.Suppliers;
using LocalErp.Application.Units;
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

        RegisterGlobalExceptionHandlers();

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
                services.AddTransient<SupplierService>();
                services.AddTransient<MaterialService>();
                services.AddTransient<ProductService>();
                services.AddTransient<MeasurementUnitService>();
                services.AddTransient<CategoryService>();
                services.AddTransient<CostCenterService>();
                services.AddTransient<PurchaseRequestService>();
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
                services.AddTransient<SuppliersCatalogViewModel>();
                services.AddTransient<MaterialsCatalogViewModel>();
                services.AddTransient<ProductsCatalogViewModel>();
                services.AddTransient<MeasurementUnitsCatalogViewModel>();
                services.AddTransient<CategoriesCatalogViewModel>();
                services.AddTransient<CostCentersCatalogViewModel>();
                services.AddTransient<PurchaseRequestsWorkspaceViewModel>();
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
            ShowFatalError(
                "O Local ERP não conseguiu iniciar. Consulte os logs para detalhes técnicos.",
                "Falha ao iniciar");
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

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += HandleDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += HandleDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
    }

    private void UnregisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException -= HandleDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= HandleDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= HandleUnobservedTaskException;
    }

    private void HandleDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        Log.Fatal(e.Exception, "Unhandled exception on the WPF dispatcher");
        e.Handled = true;

        ShowFatalError(
            "O Local ERP encontrou uma falha inesperada e será encerrado para proteger a consistência da sessão. Consulte os logs para detalhes técnicos.",
            "Falha inesperada");
        Shutdown(1);
    }

    private static void HandleDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(
                exception,
                "Unhandled exception escaped the application domain; terminating={IsTerminating}",
                e.IsTerminating);
        }
        else
        {
            Log.Fatal(
                "Unhandled non-Exception object escaped the application domain; terminating={IsTerminating}; type={ExceptionObjectType}",
                e.IsTerminating,
                e.ExceptionObject?.GetType().FullName ?? "null");
        }

        if (e.IsTerminating)
        {
            Log.CloseAndFlush();
        }
    }

    private static void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    private static void ShowFatalError(string message, string title)
    {
        try
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to display fatal error dialog");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        UnregisterGlobalExceptionHandlers();

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
