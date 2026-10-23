using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        CategoryService categoryService,
        CostCenterService costCenterService,
        RequestForQuotationComparisonService requestForQuotationComparisonService,
        RequestForQuotationAwardService requestForQuotationAwardService,
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
        ArgumentNullException.ThrowIfNull(categoryService);
        ArgumentNullException.ThrowIfNull(costCenterService);
        ArgumentNullException.ThrowIfNull(requestForQuotationComparisonService);
        ArgumentNullException.ThrowIfNull(requestForQuotationAwardService);
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(roleService);

        InitializeComponent();
        CompaniesCatalogPanel.Configure(companyService, companyBranchService);
        SuppliersCatalogPanel.Configure(supplierService);
        MaterialsCatalogPanel.Configure(materialService);
        ProductsCatalogPanel.Configure(productService);
        MeasurementUnitsCatalogPanel.Configure(measurementUnitService);
        CategoriesCatalogPanel.Configure(categoryService);
        CostCentersCatalogPanel.Configure(costCenterService);
        RequestForQuotationsWorkspacePanel.Configure(
            requestForQuotationComparisonService,
            requestForQuotationAwardService);
        AdministrationUsersPanel.Configure(userService, roleService);
        DataContext = viewModel;
    }

    public bool LogoutRequested { get; private set; }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusNavigationRegion));
    }

    private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.None)
        {
            CycleShellFocus();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control &&
            ShellKeyboardNavigation.TryGetNavigationIndex(e.Key, out var navigationIndex))
        {
            e.Handled = true;
            await NavigateByShortcutAsync(navigationIndex);
            return;
        }

        if (e.Key == Key.Tab &&
            (Keyboard.Modifiers == ModifierKeys.Control ||
             Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
        {
            var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
            if (CycleActiveTab(direction))
            {
                e.Handled = true;
            }
        }
    }

    private async Task NavigateByShortcutAsync(int index)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            index < 0 ||
            index >= viewModel.NavigationItems.Count)
        {
            return;
        }

        await viewModel.NavigateCommand.ExecuteAsync(viewModel.NavigationItems[index]);
        FocusActiveContent();
    }

    private bool CycleActiveTab(int direction)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return false;
        }

        var tabControl = viewModel.IsCatalogsSelected
            ? CatalogsTabControl
            : viewModel.IsPurchasingSelected
                ? PurchasingTabControl
                : null;

        if (tabControl is null || tabControl.Items.Count == 0)
        {
            return false;
        }

        tabControl.SelectedIndex = ShellKeyboardNavigation.GetNextTabIndex(
            tabControl.SelectedIndex,
            tabControl.Items.Count,
            direction);
        FocusSelectedTab(tabControl);
        return true;
    }

    private void CycleShellFocus()
    {
        var currentRegion = NavigationItemsControl.IsKeyboardFocusWithin
            ? ShellFocusRegion.Navigation
            : ShellContentRegion.IsKeyboardFocusWithin
                ? ShellFocusRegion.Content
                : ShellFocusRegion.Account;

        switch (ShellKeyboardNavigation.GetNextFocusRegion(currentRegion))
        {
            case ShellFocusRegion.Navigation:
                FocusNavigationRegion();
                break;
            case ShellFocusRegion.Content:
                FocusActiveContent();
                break;
            case ShellFocusRegion.Account:
                ChangePasswordButton.Focus();
                break;
            default:
                throw new InvalidOperationException("Unknown shell focus region.");
        }
    }

    private void FocusActiveContent()
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.IsCatalogsSelected)
        {
            FocusSelectedTab(CatalogsTabControl);
            return;
        }

        if (viewModel.IsPurchasingSelected)
        {
            FocusSelectedTab(PurchasingTabControl);
            return;
        }

        if (viewModel.IsAdministrationSelected)
        {
            if (!FocusFirstFocusableDescendant(AdministrationUsersPanel))
            {
                AdministrationUsersPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
            return;
        }

        OverviewScrollViewer.Focus();
    }

    private void FocusNavigationRegion()
    {
        if (!FocusFirstFocusableDescendant(NavigationItemsControl))
        {
            NavigationItemsControl.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private static void FocusSelectedTab(TabControl tabControl)
    {
        if (tabControl.SelectedItem is TabItem selectedTab)
        {
            selectedTab.Focus();
        }
        else
        {
            tabControl.Focus();
        }
    }

    private static bool FocusFirstFocusableDescendant(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is UIElement element &&
                element.Focusable &&
                element.IsEnabled &&
                element.IsVisible &&
                element.Focus())
            {
                return true;
            }

            if (FocusFirstFocusableDescendant(child))
            {
                return true;
            }
        }

        return false;
    }

    private async void CatalogsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, CatalogsTabControl) || DataContext is not MainWindowViewModel viewModel) return;

        if (CatalogsTabControl.SelectedIndex == 1 && !viewModel.SuppliersCatalog.HasLoaded)
            await viewModel.SuppliersCatalog.LoadAsync();
        else if (CatalogsTabControl.SelectedIndex == 2 && !viewModel.MaterialsCatalog.HasLoaded)
            await viewModel.MaterialsCatalog.LoadAsync();
        else if (CatalogsTabControl.SelectedIndex == 3 && !viewModel.ProductsCatalog.HasLoaded)
            await viewModel.ProductsCatalog.LoadAsync();
        else if (CatalogsTabControl.SelectedIndex == 4 && !viewModel.MeasurementUnitsCatalog.HasLoaded)
            await viewModel.MeasurementUnitsCatalog.LoadAsync();
        else if (CatalogsTabControl.SelectedIndex == 5 && !viewModel.CategoriesCatalog.HasLoaded)
            await viewModel.CategoriesCatalog.LoadAsync();
        else if (CatalogsTabControl.SelectedIndex == 6 && !viewModel.CostCentersCatalog.HasLoaded)
            await viewModel.CostCentersCatalog.LoadAsync();
    }

    private async void PurchasingTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, PurchasingTabControl) || DataContext is not MainWindowViewModel viewModel) return;

        if (PurchasingTabControl.SelectedIndex == 0 && !viewModel.PurchaseRequestsWorkspace.HasLoaded)
            await viewModel.PurchaseRequestsWorkspace.LoadAsync();
        else if (PurchasingTabControl.SelectedIndex == 1 && !viewModel.RequestForQuotationsWorkspace.HasLoaded)
            await viewModel.RequestForQuotationsWorkspace.LoadAsync();
        else if (PurchasingTabControl.SelectedIndex == 2 && !viewModel.PurchaseOrdersWorkspace.HasLoaded)
            await viewModel.PurchaseOrdersWorkspace.LoadAsync();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var window = new ChangePasswordWindow(_accountService) { Owner = this };
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