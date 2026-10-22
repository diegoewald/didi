using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class RequestForQuotationsWorkspaceView : UserControl
{
    private Guid? _creatingQuotationSupplierId;
    private Guid? _editingQuotationLineId;
    private bool _isBusy;
    private RequestForQuotationComparisonService? _comparisonService;
    private RequestForQuotationAwardService? _awardService;

    public RequestForQuotationsWorkspaceView()
    {
        InitializeComponent();
    }

    public void Configure(
        RequestForQuotationComparisonService comparisonService,
        RequestForQuotationAwardService awardService)
    {
        _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
        _awardService = awardService ?? throw new ArgumentNullException(nameof(awardService));
    }

    private RequestForQuotationsWorkspaceViewModel? ViewModel => DataContext as RequestForQuotationsWorkspaceViewModel;

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null) return;
        CloseTransientPanels();
        HideOperationMessages();
        await ViewModel.LoadAsync();
    }

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isBusy || ViewModel is not { HasLoaded: true } viewModel) return;
        CloseTransientPanels();
        HideOperationMessages();
        await viewModel.LoadAsync();
    }

    private async void OpenCreate_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null) return;
        HideOperationMessages();
        SetBusy(true);
        try
        {
            await ViewModel.EnsureCreationSourcesAsync();
            if (ViewModel.CreationSources.Count == 0)
            {
                ShowOperationError("Não há Solicitação de Compra enviada disponível para iniciar uma cotação.");
                return;
            }

            CreationSourceComboBox.SelectedIndex = 0;
            CreationPanel.Visibility = Visibility.Visible;
            CreationSourceComboBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowOperationError("Você não tem permissão para criar cotações/RFQs.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to prepare RFQ creation");
            ShowOperationError("Não foi possível preparar a criação da cotação. Consulte os logs para detalhes técnicos.");
        }
        finally { SetBusy(false); }
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || CreationSourceComboBox.SelectedItem is not RequestForQuotationSourceRequestListItemViewModel source) return;
        await ExecuteMutationAsync(
            async () =>
            {
                await ViewModel.CreateDraftAsync(source.Id);
                CreationPanel.Visibility = Visibility.Collapsed;
            },
            "Rascunho de cotação criado. Selecione linhas e fornecedores antes de enviar.",
            "criar a cotação");
    }

    private void CloseCreate_Click(object sender, RoutedEventArgs e) => CreationPanel.Visibility = Visibility.Collapsed;

    private async void OpenRfq_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null || sender is not Button { DataContext: RequestForQuotationListItemViewModel item }) return;
        HideOperationMessages();
        CloseTransientPanels();
        SetBusy(true);
        try { await ViewModel.SelectAsync(item.Id); }
        catch (AccessDeniedException) { ShowOperationError("Você não tem permissão para consultar esta cotação."); }
        catch (KeyNotFoundException) { ShowOperationError("A cotação ou sua origem não foi encontrada. Atualize e tente novamente."); }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to open RFQ from workspace");
            ShowOperationError("Não foi possível abrir a cotação. Consulte os logs para detalhes técnicos.");
        }
        finally { SetBusy(false); }
    }

    private void OpenComparison_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel?.CurrentRequestForQuotation is not RequestForQuotationResult current)
        {
            return;
        }

        if (_comparisonService is null)
        {
            ShowOperationError("A comparação de cotações não está disponível nesta sessão.");
            return;
        }

        HideOperationMessages();
        var window = new QuotationComparisonWindow(_comparisonService, current.Id)
        {
            Owner = Window.GetWindow(this),
        };
        window.ShowDialog();
    }

    private void OpenAward_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel?.CurrentRequestForQuotation is not RequestForQuotationResult current)
        {
            return;
        }

        if (current.Status != LocalErp.Domain.Purchasing.RequestForQuotationStatus.Closed)
        {
            ShowOperationError("A adjudicação só está disponível para RFQs encerrados.");
            return;
        }

        if (_comparisonService is null || _awardService is null)
        {
            ShowOperationError("A adjudicação não está disponível nesta sessão.");
            return;
        }

        HideOperationMessages();
        var window = new RequestForQuotationAwardWindow(
            _comparisonService,
            _awardService,
            current.Id)
        {
            Owner = Window.GetWindow(this),
        };
        window.ShowDialog();
    }

    private async void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        await ExecuteMutationAsync(async () => await ViewModel.UpdateNotesAsync(NotesTextBox.Text), "Observação salva.", "salvar a observação");
    }

    private async void AddSourceLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || AvailableSourceLineComboBox.SelectedItem is not RequestForQuotationSourceLineListItemViewModel line) return;
        await ExecuteMutationAsync(async () => await ViewModel.AddSourceLineAsync(line.PurchaseRequestLineId), "Linha adicionada ao RFQ.", "adicionar a linha");
    }

    private async void RemoveSourceLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button { DataContext: RequestForQuotationSourceLineListItemViewModel line } || line.RequestForQuotationLineId is not Guid lineId) return;
        if (!Confirm($"Remover a linha {line.LineNumber} — '{line.Description}' do RFQ?", "Confirmar remoção")) return;
        await ExecuteMutationAsync(async () => await ViewModel.RemoveSourceLineAsync(lineId), "Linha removida do RFQ.", "remover a linha");
    }

    private async void OpenSupplierPicker_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || ViewModel is null) return;
        HideOperationMessages();
        SetBusy(true);
        try
        {
            await ViewModel.EnsureActiveSuppliersAsync();
            if (ViewModel.AvailableSuppliers.Count == 0)
            {
                ShowOperationError("Não há fornecedor ativo disponível para convite, ou todos já foram incluídos.");
                return;
            }

            AvailableSupplierComboBox.SelectedIndex = 0;
            SupplierPickerPanel.Visibility = Visibility.Visible;
        }
        catch (AccessDeniedException) { ShowOperationError("Você não tem permissão para adicionar fornecedores ao RFQ."); }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to prepare RFQ supplier picker");
            ShowOperationError("Não foi possível carregar fornecedores ativos. Consulte os logs para detalhes técnicos.");
        }
        finally { SetBusy(false); }
    }

    private async void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || AvailableSupplierComboBox.SelectedItem is not RequestForQuotationSupplierOption supplier) return;
        await ExecuteMutationAsync(
            async () =>
            {
                await ViewModel.AddSupplierAsync(supplier.Id);
                SupplierPickerPanel.Visibility = Visibility.Collapsed;
            },
            "Fornecedor convidado para o RFQ.",
            "adicionar o fornecedor");
    }

    private void CloseSupplierPicker_Click(object sender, RoutedEventArgs e) => SupplierPickerPanel.Visibility = Visibility.Collapsed;

    private async void RemoveSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button { DataContext: RequestForQuotationSupplierListItemViewModel supplier }) return;
        if (!Confirm($"Remover o fornecedor '{supplier.Name}' deste RFQ?", "Confirmar remoção")) return;
        await ExecuteMutationAsync(async () => await ViewModel.RemoveSupplierAsync(supplier.SupplierId), "Fornecedor removido do RFQ.", "remover o fornecedor");
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !Confirm("Enviar este RFQ? Linhas e fornecedores ficarão imutáveis após o envio.", "Confirmar envio")) return;
        await ExecuteMutationAsync(async () => await ViewModel.SendAsync(), "RFQ enviado. Respostas de fornecedores já podem ser registradas.", "enviar o RFQ");
    }

    private async void CloseRfq_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !Confirm("Encerrar este RFQ? É necessária ao menos uma resposta finalizada e o documento ficará somente leitura.", "Confirmar encerramento")) return;
        await ExecuteMutationAsync(async () => await ViewModel.CloseAsync(), "RFQ encerrado com sucesso.", "encerrar o RFQ");
    }

    private void OpenCreateQuotation_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { DataContext: RequestForQuotationSupplierListItemViewModel supplier }) return;
        HideOperationMessages();
        CloseQuotationEditor();
        _creatingQuotationSupplierId = supplier.SupplierId;
        NewQuotationTitle.Text = $"Registrar resposta — {supplier.Name}";
        NewQuotationCurrencyTextBox.Clear();
        NewQuotationReferenceTextBox.Clear();
        NewQuotationValidUntilDatePicker.SelectedDate = null;
        NewQuotationNotesTextBox.Clear();
        NewQuotationPanel.Visibility = Visibility.Visible;
        NewQuotationCurrencyTextBox.Focus();
    }

    private async void CreateQuotation_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || _creatingQuotationSupplierId is not Guid supplierId) return;
        var validUntil = NewQuotationValidUntilDatePicker.SelectedDate is DateTime date ? DateOnly.FromDateTime(date) : (DateOnly?)null;
        await ExecuteMutationAsync(
            async () =>
            {
                await ViewModel.CreateSupplierQuotationAsync(
                    supplierId,
                    NewQuotationCurrencyTextBox.Text,
                    NewQuotationReferenceTextBox.Text,
                    validUntil,
                    NewQuotationNotesTextBox.Text);
                NewQuotationPanel.Visibility = Visibility.Collapsed;
                _creatingQuotationSupplierId = null;
                PopulateQuotationEditor();
            },
            "Resposta criada em rascunho.",
            "criar a resposta do fornecedor");
    }

    private void CloseNewQuotation_Click(object sender, RoutedEventArgs e)
    {
        _creatingQuotationSupplierId = null;
        NewQuotationPanel.Visibility = Visibility.Collapsed;
    }

    private void OpenQuotation_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button { DataContext: RequestForQuotationSupplierListItemViewModel supplier } || supplier.SupplierQuotationId is not Guid quotationId) return;
        HideOperationMessages();
        NewQuotationPanel.Visibility = Visibility.Collapsed;
        ViewModel.SelectSupplierQuotation(quotationId);
        PopulateQuotationEditor();
    }

    private async void SaveQuotation_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var validUntil = QuotationValidUntilDatePicker.SelectedDate is DateTime date ? DateOnly.FromDateTime(date) : (DateOnly?)null;
        await ExecuteMutationAsync(
            async () => await ViewModel.UpdateSupplierQuotationAsync(
                QuotationCurrencyTextBox.Text,
                QuotationReferenceTextBox.Text,
                validUntil,
                QuotationNotesTextBox.Text),
            "Cabeçalho da resposta salvo.",
            "salvar a resposta");
        PopulateQuotationEditor();
    }

    private async void FinalizeQuotation_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !Confirm("Finalizar esta resposta? Depois disso ela ficará imutável neste slice.", "Confirmar finalização")) return;
        await ExecuteMutationAsync(async () => await ViewModel.FinalizeSupplierQuotationAsync(), "Resposta finalizada.", "finalizar a resposta");
        PopulateQuotationEditor();
    }

    private void CloseQuotationEditor_Click(object sender, RoutedEventArgs e) => CloseQuotationEditor();

    private void OpenAddQuotationLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanEditCurrentSupplierQuotation) return;
        _editingQuotationLineId = null;
        QuotationLineEditorTitle.Text = "Adicionar linha cotada";
        QuotationLineSourceComboBox.IsEnabled = true;
        QuotationLineSourceComboBox.SelectedIndex = ViewModel.CurrentSourceLines.Count > 0 ? 0 : -1;
        QuotationLinePriceTextBox.Clear();
        QuotationLineDeliveryDatePicker.SelectedDate = null;
        QuotationLineNotesTextBox.Clear();
        QuotationLineEditorPanel.Visibility = Visibility.Visible;
    }

    private void EditQuotationLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanEditCurrentSupplierQuotation || sender is not Button { DataContext: RequestForQuotationSupplierQuotationLineListItemViewModel line }) return;
        _editingQuotationLineId = line.Id;
        QuotationLineEditorTitle.Text = $"Editar linha cotada #{line.SourceLineNumber}";
        QuotationLineSourceComboBox.SelectedItem = ViewModel.CurrentSourceLines.SingleOrDefault(item => item.RequestForQuotationLineId == line.RequestForQuotationLineId);
        QuotationLineSourceComboBox.IsEnabled = false;
        QuotationLinePriceTextBox.Text = line.UnitPrice.ToString("0.######", CultureInfo.CurrentCulture);
        QuotationLineDeliveryDatePicker.SelectedDate = line.PromisedDeliveryDate?.ToDateTime(TimeOnly.MinValue);
        QuotationLineNotesTextBox.Text = line.Notes ?? string.Empty;
        QuotationLineEditorPanel.Visibility = Visibility.Visible;
    }

    private async void SaveQuotationLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || QuotationLineSourceComboBox.SelectedItem is not RequestForQuotationSourceLineListItemViewModel source || source.RequestForQuotationLineId is not Guid rfqLineId) return;
        if (!TryParseDecimal(QuotationLinePriceTextBox.Text, out var price) || price < 0)
        {
            ShowOperationError("Informe um preço unitário numérico maior ou igual a zero, com no máximo seis casas decimais.");
            return;
        }

        var promised = QuotationLineDeliveryDatePicker.SelectedDate is DateTime date ? DateOnly.FromDateTime(date) : (DateOnly?)null;
        await ExecuteMutationAsync(
            async () =>
            {
                await ViewModel.UpsertSupplierQuotationLineAsync(rfqLineId, price, promised, QuotationLineNotesTextBox.Text);
                CloseQuotationLineEditor();
            },
            _editingQuotationLineId is null ? "Linha cotada adicionada." : "Linha cotada atualizada.",
            "salvar a linha cotada");
    }

    private async void RemoveQuotationLine_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button { DataContext: RequestForQuotationSupplierQuotationLineListItemViewModel line }) return;
        if (!Confirm($"Remover a cotação da linha #{line.SourceLineNumber} — '{line.Description}'?", "Confirmar remoção")) return;
        await ExecuteMutationAsync(async () => await ViewModel.RemoveSupplierQuotationLineAsync(line.Id), "Linha cotada removida.", "remover a linha cotada");
    }

    private void CloseQuotationLineEditor_Click(object sender, RoutedEventArgs e) => CloseQuotationLineEditor();

    private void OpenCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        HideOperationMessages();
        CancellationReasonTextBox.Clear();
        CancellationPanel.Visibility = Visibility.Visible;
        CancellationReasonTextBox.Focus();
    }

    private async void ConfirmCancel_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null) return;
        await ExecuteMutationAsync(
            async () =>
            {
                await ViewModel.CancelAsync(CancellationReasonTextBox.Text);
                CancellationPanel.Visibility = Visibility.Collapsed;
            },
            "RFQ cancelado; o histórico foi preservado.",
            "cancelar o RFQ");
    }

    private void CloseCancel_Click(object sender, RoutedEventArgs e) => CancellationPanel.Visibility = Visibility.Collapsed;

    private async Task ExecuteMutationAsync(Func<Task> action, string successMessage, string operationLabel)
    {
        if (_isBusy || ViewModel is null) return;
        HideOperationMessages();
        SetBusy(true);
        try
        {
            await action();
            ShowOperationSuccess(successMessage);
        }
        catch (AccessDeniedException) { ShowOperationError("Você não tem permissão para executar esta operação de cotação/RFQ."); }
        catch (KeyNotFoundException) { ShowOperationError("Um registro necessário não foi encontrado. Atualize os dados e tente novamente."); }
        catch (ArgumentException exception) { ShowOperationError($"Dados inválidos: {exception.Message}"); }
        catch (InvalidOperationException exception) { ShowOperationError($"Operação não permitida no estado atual: {exception.Message}"); }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to {OperationLabel} in RFQ workspace", operationLabel);
            ShowOperationError($"Não foi possível {operationLabel}. Consulte os logs para detalhes técnicos.");
        }
        finally { SetBusy(false); }
    }

    private void PopulateQuotationEditor()
    {
        if (ViewModel?.CurrentSupplierQuotation is not SupplierQuotationResult quotation) return;
        QuotationCurrencyTextBox.Text = quotation.CurrencyCode;
        QuotationReferenceTextBox.Text = quotation.SupplierReference ?? string.Empty;
        QuotationValidUntilDatePicker.SelectedDate = quotation.ValidUntilDate?.ToDateTime(TimeOnly.MinValue);
        QuotationNotesTextBox.Text = quotation.Notes ?? string.Empty;
        var editable = ViewModel.CanEditCurrentSupplierQuotation;
        QuotationCurrencyTextBox.IsReadOnly = !editable;
        QuotationReferenceTextBox.IsReadOnly = !editable;
        QuotationValidUntilDatePicker.IsEnabled = editable;
        QuotationNotesTextBox.IsReadOnly = !editable;
        QuotationEditorPanel.Visibility = Visibility.Visible;
        CloseQuotationLineEditor();
    }

    private void CloseQuotationEditor()
    {
        ViewModel?.ClearCurrentSupplierQuotation();
        QuotationEditorPanel.Visibility = Visibility.Collapsed;
        CloseQuotationLineEditor();
    }

    private void CloseQuotationLineEditor()
    {
        _editingQuotationLineId = null;
        QuotationLineSourceComboBox.IsEnabled = true;
        QuotationLineSourceComboBox.SelectedIndex = -1;
        QuotationLinePriceTextBox.Clear();
        QuotationLineDeliveryDatePicker.SelectedDate = null;
        QuotationLineNotesTextBox.Clear();
        QuotationLineEditorPanel.Visibility = Visibility.Collapsed;
    }

    private void CloseTransientPanels()
    {
        CreationPanel.Visibility = Visibility.Collapsed;
        SupplierPickerPanel.Visibility = Visibility.Collapsed;
        NewQuotationPanel.Visibility = Visibility.Collapsed;
        CancellationPanel.Visibility = Visibility.Collapsed;
        _creatingQuotationSupplierId = null;
        CloseQuotationEditor();
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
        WorkspaceContent.IsEnabled = !value;
    }

    private static bool Confirm(string message, string title) => MessageBox.Show(
        message,
        title,
        MessageBoxButton.YesNo,
        MessageBoxImage.Question,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

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