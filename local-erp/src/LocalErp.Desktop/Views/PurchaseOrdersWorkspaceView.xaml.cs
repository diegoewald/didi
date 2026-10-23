using System.Windows;
using System.Windows.Controls;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class PurchaseOrdersWorkspaceView : UserControl
{
    private bool _isBusy;

    public PurchaseOrdersWorkspaceView()
    {
        InitializeComponent();
    }

    private PurchaseOrdersWorkspaceViewModel? ViewModel => DataContext as PurchaseOrdersWorkspaceViewModel;

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null)
        {
            return;
        }

        CloseCreationPanel();
        CloseCancellationPanel();
        HideOperationMessages();
        await ViewModel.LoadAsync();
    }

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBusy || ViewModel is not { HasLoaded: true } viewModel)
        {
            return;
        }

        CloseCreationPanel();
        CloseCancellationPanel();
        HideOperationMessages();
        await viewModel.LoadAsync();
    }

    private async void OpenCreation_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null)
        {
            return;
        }

        HideOperationMessages();
        CloseCancellationPanel();
        SetBusy(true);
        try
        {
            await ViewModel.RefreshCreationCandidatesAsync();
            CreationEmptyText.Visibility = ViewModel.CreationCandidates.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            CreationGridBorder.Visibility = ViewModel.CreationCandidates.Count == 0
                ? Visibility.Collapsed
                : Visibility.Visible;
            CreationPanel.Visibility = Visibility.Visible;
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para criar pedidos de compra.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to load purchase order creation candidates");
            ShowOperationError("Não foi possível localizar adjudicações disponíveis. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void CreateCandidate_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null ||
            sender is not Button button ||
            button.DataContext is not PurchaseOrderCreationCandidateViewModel candidate)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            $"Criar um pedido de compra para '{candidate.SupplierName}' com {candidate.AwardedLineCount} linha(s) adjudicada(s)?\n\nOs termos serão reconstruídos diretamente da adjudicação finalizada.",
            "Criar pedido de compra",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        HideOperationMessages();
        SetBusy(true);
        try
        {
            var created = await ViewModel.CreateDraftAsync(
                candidate.RequestForQuotationAwardId,
                candidate.SupplierId);
            CloseCreationPanel();
            ShowOperationSuccess($"{PurchaseOrdersWorkspaceViewModel.FormatNumber(created.SequenceNumber)} criado em rascunho. Revise a observação e emita quando estiver pronto.");
            NotesTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para criar pedidos de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A adjudicação, o fornecedor ou algum dado de origem não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning(exception, "Purchase order creation candidate became invalid before creation");
            ShowOperationError("Esta origem não está mais disponível para criação. Ela pode já ter sido convertida ou o fornecedor pode ter sido inativado. Atualize a lista.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create purchase order from finalized award");
            ShowOperationError("Não foi possível criar o pedido de compra. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OpenOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null ||
            sender is not Button button ||
            button.DataContext is not PurchaseOrderListItemViewModel item)
        {
            return;
        }

        HideOperationMessages();
        CloseCreationPanel();
        CloseCancellationPanel();
        SetBusy(true);
        try
        {
            await ViewModel.SelectAsync(item.Id);
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para consultar este pedido de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("O pedido não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to open purchase order from workspace");
            ShowOperationError("Não foi possível abrir o pedido de compra. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.IsDraft)
        {
            return;
        }

        HideOperationMessages();
        SetBusy(true);
        try
        {
            await ViewModel.UpdateNotesAsync(NotesTextBox.Text);
            ShowOperationSuccess("Observação salva com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para editar pedidos de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("O pedido não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowOperationError("A observação informada não é válida. O limite é de 1000 caracteres.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("A observação só pode ser alterada enquanto o pedido está em rascunho.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to update purchase order notes from workspace");
            ShowOperationError("Não foi possível salvar a observação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Issue_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.CanIssue)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            "Emitir este pedido de compra? Após a emissão, os dados ficarão somente leitura.\n\nEsta ação NÃO envia e-mail, EDI, API nem comunicação eletrônica ao fornecedor.",
            "Confirmar emissão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        HideOperationMessages();
        CloseCancellationPanel();
        SetBusy(true);
        try
        {
            var issued = await ViewModel.IssueAsync();
            ShowOperationSuccess($"{PurchaseOrdersWorkspaceViewModel.FormatNumber(issued.SequenceNumber)} marcado como emitido internamente. Nenhum envio eletrônico foi realizado.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para emitir pedidos de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("O pedido ou fornecedor não foi encontrado. Atualize os dados e tente novamente.");
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning(exception, "Purchase order could not be issued in its current state");
            ShowOperationError("O pedido não pode ser emitido no estado atual ou o fornecedor está inativo.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to issue purchase order from workspace");
            ShowOperationError("Não foi possível emitir o pedido de compra. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OpenCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.CanCancel)
        {
            return;
        }

        HideOperationMessages();
        CloseCreationPanel();
        CancellationReasonTextBox.Clear();
        CancellationPanel.Visibility = Visibility.Visible;
        CancellationReasonTextBox.Focus();
    }

    private async void ConfirmCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.CanCancel)
        {
            return;
        }

        HideOperationMessages();
        SetBusy(true);
        try
        {
            var cancelled = await ViewModel.CancelAsync(CancellationReasonTextBox.Text);
            CloseCancellationPanel();
            ShowOperationSuccess($"{PurchaseOrdersWorkspaceViewModel.FormatNumber(cancelled.SequenceNumber)} cancelado com histórico preservado.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para cancelar pedidos de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("O pedido não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowOperationError("Informe uma justificativa de cancelamento válida com até 500 caracteres.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("Este pedido não pode mais ser cancelado no estado atual.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to cancel purchase order from workspace");
            ShowOperationError("Não foi possível cancelar o pedido de compra. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CloseCreation_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            CloseCreationPanel();
        }
    }

    private void CloseCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            CloseCancellationPanel();
        }
    }

    private void CloseCreationPanel()
    {
        CreationPanel.Visibility = Visibility.Collapsed;
        CreationEmptyText.Visibility = Visibility.Collapsed;
        CreationGridBorder.Visibility = Visibility.Visible;
    }

    private void CloseCancellationPanel()
    {
        CancellationReasonTextBox.Clear();
        CancellationPanel.Visibility = Visibility.Collapsed;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        WorkspaceContent.IsEnabled = !isBusy;
    }

    private void HideOperationMessages()
    {
        OperationErrorPanel.Visibility = Visibility.Collapsed;
        OperationSuccessPanel.Visibility = Visibility.Collapsed;
        OperationErrorText.Text = string.Empty;
        OperationSuccessText.Text = string.Empty;
    }

    private void ShowOperationError(string message)
    {
        OperationSuccessPanel.Visibility = Visibility.Collapsed;
        OperationErrorText.Text = message;
        OperationErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowOperationSuccess(string message)
    {
        OperationErrorPanel.Visibility = Visibility.Collapsed;
        OperationSuccessText.Text = message;
        OperationSuccessPanel.Visibility = Visibility.Visible;
    }
}
