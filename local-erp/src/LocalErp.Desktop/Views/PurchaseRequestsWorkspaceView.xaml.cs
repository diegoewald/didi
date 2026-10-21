using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class PurchaseRequestsWorkspaceView : UserControl
{
    private Guid? _editingLineId;
    private bool _isBusy;

    public PurchaseRequestsWorkspaceView()
    {
        InitializeComponent();
    }

    private PurchaseRequestsWorkspaceViewModel? ViewModel => DataContext as PurchaseRequestsWorkspaceViewModel;

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null)
        {
            return;
        }

        CloseLineEditor();
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

        CloseLineEditor();
        CloseCancellationPanel();
        HideOperationMessages();
        await viewModel.LoadAsync();
    }

    private async void CreateDraft_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null)
        {
            return;
        }

        HideOperationMessages();
        SetBusy(true);
        try
        {
            await ViewModel.CreateDraftAsync();
            CloseLineEditor();
            CloseCancellationPanel();
            ShowOperationSuccess("Rascunho criado. Inclua as linhas necessárias e envie quando estiver pronto.");
            NotesTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para criar solicitações de compra.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create purchase request draft from workspace");
            ShowOperationError("Não foi possível criar o rascunho. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OpenRequest_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || sender is not Button button || button.DataContext is not PurchaseRequestListItemViewModel item)
        {
            return;
        }

        HideOperationMessages();
        CloseLineEditor();
        CloseCancellationPanel();
        SetBusy(true);
        try
        {
            await ViewModel.SelectAsync(item.Id);
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para consultar esta solicitação de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A solicitação não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to open purchase request from workspace");
            ShowOperationError("Não foi possível abrir a solicitação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null)
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
            ShowOperationError("Você não tem permissão para editar solicitações de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A solicitação não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowOperationError("A observação informada não é válida. O limite é de 1000 caracteres.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("A observação só pode ser alterada enquanto a solicitação está em rascunho.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to update purchase request notes from workspace");
            ShowOperationError("Não foi possível salvar a observação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OpenAddLine_Click(object sender, RoutedEventArgs e)
    {
        await OpenLineEditorAsync(null);
    }

    private async void EditLine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PurchaseRequestLineListItemViewModel line)
        {
            await OpenLineEditorAsync(line);
        }
    }

    private async Task OpenLineEditorAsync(PurchaseRequestLineListItemViewModel? line)
    {
        if (_isBusy || ViewModel is null || !ViewModel.IsDraft)
        {
            return;
        }

        HideOperationMessages();
        CloseCancellationPanel();
        SetBusy(true);
        try
        {
            await ViewModel.EnsureMeasurementUnitsAsync();
            if (ViewModel.MeasurementUnits.Count == 0)
            {
                ShowOperationError("Cadastre e mantenha ativa ao menos uma Unidade de Medida antes de incluir linhas.");
                return;
            }

            _editingLineId = line?.Id;
            LineEditorTitle.Text = line is null ? "Adicionar linha" : $"Editar linha {line.LineNumber}";
            SaveLineButton.Content = line is null ? "Adicionar linha" : "Salvar alteração";
            LineDescriptionTextBox.Text = line?.Description ?? string.Empty;
            LineQuantityTextBox.Text = line?.Quantity.ToString("0.####", CultureInfo.CurrentCulture) ?? string.Empty;
            LineNeededByDatePicker.SelectedDate = line?.NeededByDate is DateOnly neededBy
                ? neededBy.ToDateTime(TimeOnly.MinValue)
                : null;
            LineUnitComboBox.SelectedValue = line?.MeasurementUnitId ?? ViewModel.MeasurementUnits[0].Id;
            LineEditorPanel.Visibility = Visibility.Visible;
            LineDescriptionTextBox.Focus();
            LineDescriptionTextBox.SelectAll();
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para editar linhas da solicitação de compra.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to prepare purchase request line editor");
            ShowOperationError("Não foi possível preparar o editor de linhas. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SaveLine_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.IsDraft)
        {
            return;
        }

        HideOperationMessages();

        if (!TryParseQuantity(LineQuantityTextBox.Text, out var quantity) || quantity <= 0m)
        {
            ShowOperationError("Informe uma quantidade numérica maior que zero.");
            return;
        }

        if (LineUnitComboBox.SelectedValue is not Guid measurementUnitId)
        {
            ShowOperationError("Selecione uma Unidade de Medida ativa.");
            return;
        }

        var neededByDate = LineNeededByDatePicker.SelectedDate is DateTime selectedDate
            ? DateOnly.FromDateTime(selectedDate)
            : (DateOnly?)null;

        SetBusy(true);
        try
        {
            if (_editingLineId is Guid lineId)
            {
                await ViewModel.UpdateLineAsync(
                    lineId,
                    LineDescriptionTextBox.Text,
                    quantity,
                    measurementUnitId,
                    neededByDate);
                ShowOperationSuccess("Linha atualizada com sucesso.");
            }
            else
            {
                await ViewModel.AddLineAsync(
                    LineDescriptionTextBox.Text,
                    quantity,
                    measurementUnitId,
                    neededByDate);
                ShowOperationSuccess("Linha adicionada com sucesso.");
            }

            CloseLineEditor();
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para editar linhas da solicitação de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A solicitação, linha ou Unidade de Medida não foi encontrada. Atualize os dados e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowOperationError("Revise a descrição, a quantidade e a Unidade de Medida informadas.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("A linha só pode ser alterada em um rascunho e deve usar uma Unidade de Medida ativa.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to save purchase request line from workspace");
            ShowOperationError("Não foi possível salvar a linha. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CloseLineEditor_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            CloseLineEditor();
        }
    }

    private async void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.IsDraft || sender is not Button button || button.DataContext is not PurchaseRequestLineListItemViewModel line)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            $"Remover a linha {line.LineNumber} — '{line.Description}'?",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        HideOperationMessages();
        CloseLineEditor();
        SetBusy(true);
        try
        {
            await ViewModel.RemoveLineAsync(line.Id);
            ShowOperationSuccess("Linha removida com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para remover linhas da solicitação de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A linha não foi encontrada. Atualize a solicitação e tente novamente.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("Linhas só podem ser removidas enquanto a solicitação está em rascunho.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to remove purchase request line from workspace");
            ShowOperationError("Não foi possível remover a linha. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || !ViewModel.IsDraft)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            "Enviar esta solicitação para o processo de Compras? Após o envio, o conteúdo ficará somente leitura neste primeiro slice.",
            "Confirmar envio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        HideOperationMessages();
        CloseLineEditor();
        SetBusy(true);
        try
        {
            await ViewModel.SubmitAsync();
            ShowOperationSuccess("Solicitação enviada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para enviar solicitações de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A solicitação não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("Para enviar, a solicitação deve estar em rascunho e possuir ao menos uma linha válida.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to submit purchase request from workspace");
            ShowOperationError("Não foi possível enviar a solicitação. Consulte os logs para detalhes técnicos.");
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
        CloseLineEditor();
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
            await ViewModel.CancelAsync(CancellationReasonTextBox.Text);
            CloseCancellationPanel();
            ShowOperationSuccess("Solicitação cancelada. O registro foi preservado para rastreabilidade.");
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para cancelar solicitações de compra.");
        }
        catch (KeyNotFoundException)
        {
            ShowOperationError("A solicitação não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowOperationError("Informe uma justificativa de cancelamento válida com até 500 caracteres.");
        }
        catch (InvalidOperationException)
        {
            ShowOperationError("Esta solicitação não pode mais ser cancelada no estado atual.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to cancel purchase request from workspace");
            ShowOperationError("Não foi possível cancelar a solicitação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CloseCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            CloseCancellationPanel();
        }
    }

    private void CloseLineEditor()
    {
        _editingLineId = null;
        LineDescriptionTextBox.Clear();
        LineQuantityTextBox.Clear();
        LineUnitComboBox.SelectedIndex = -1;
        LineNeededByDatePicker.SelectedDate = null;
        LineEditorTitle.Text = "Adicionar linha";
        SaveLineButton.Content = "Adicionar linha";
        LineEditorPanel.Visibility = Visibility.Collapsed;
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

    private static bool TryParseQuantity(string text, out decimal quantity)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out quantity)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity);
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
