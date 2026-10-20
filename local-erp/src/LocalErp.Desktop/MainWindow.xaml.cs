using System.Windows;
using LocalErp.Application.Account;
using LocalErp.Application.Companies;
using LocalErp.Application.Materials;
using LocalErp.Application.Products;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using LocalErp.Application.Suppliers;
using LocalErp.Application.Units;
using LocalErp.Application.Users;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthenticationService _authenticationService;
    private readonly AccountService _accountService;

    public MainWindow(
        MainWindowViewModel viewModel,
        AuthenticationService authenticationService,
        AccountService accountService,
        CompanyService companyService,
        CompanyBranchService companyBranchService,
        SupplierService supplierService,
        MaterialService materialService,
        ProductService productService,
        MeasurementUnitService measurementUnitService,
        UserService userService,
        RoleService roleService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        ArgumentNullException.ThrowIfNull(companyService);
        ArgumentNullException.ThrowIfNull(companyBranchService);
        ArgumentNullException.ThrowIfNull(supplierService);
        ArgumentNullException.ThrowIfNull(materialService);
        ArgumentNullException.ThrowIfNull(productService);
        ArgumentNullException.ThrowIfNull(measurementUnitService);
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(roleService);

        InitializeComponent();
        CompaniesCatalogPanel.Configure(companyService, companyBranchService);
        SuppliersCatalogPanel.Configure(supplierService);
        MaterialsCatalogPanel.Configure(materialService);
        ProductsCatalogPanel.Configure(productService);
        MeasurementUnitsCatalogPanel.Configure(measurementUnitService);
        AdministrationUsersPanel.Configure(userService, roleService);
        DataContext = viewModel;
    }

    public bool LogoutRequested { get; private set; }

    private async void CatalogsTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, CatalogsTabControl) || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (CatalogsTabControl.SelectedIndex == 1 && !viewModel.SuppliersCatalog.HasLoaded)
        {
            await viewModel.SuppliersCatalog.LoadAsync();
        }
        else if (CatalogsTabControl.SelectedIndex == 2 && !viewModel.MaterialsCatalog.HasLoaded)
        {
            await viewModel.MaterialsCatalog.LoadAsync();
        }
        else if (CatalogsTabControl.SelectedIndex == 3 && !viewModel.ProductsCatalog.HasLoaded)
        {
            await viewModel.ProductsCatalog.LoadAsync();
        }
        else if (CatalogsTabControl.SelectedIndex == 4 && !viewModel.MeasurementUnitsCatalog.HasLoaded)
        {
            await viewModel.MeasurementUnitsCatalog.LoadAsync();
        }
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var window = new ChangePasswordWindow(_accountService)
        {
            Owner = this,
        };
        window.ShowDialog();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _authenticationService.SignOutAsync();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Security audit write failed during sign-out");
        }
        finally
        {
            LogoutRequested = true;
            Close();
        }
    }
}
