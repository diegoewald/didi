using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using Serilog;

namespace LocalErp.Desktop;

public partial class RequestForQuotationAwardWindow : Window
{
    private readonly RequestForQuotationComparisonService _comparisonService;
    private readonly RequestForQuotationAwardService _awardService;
    private readonly Guid _requestForQuotationId;
    private RequestForQuotationComparisonResult? _comparison;
    private RequestForQuotationAwardResult? _award;
    private bool _isBusy;
    private bool _isPopulating;

    public RequestForQuotationAwardWindow(
        RequestForQuotationComparisonService comparisonService,
        RequestForQuotationAwardService awardService,
        Guid requestForQuotationId)
    {
        _comparisonService = comparisonService
            ?? throw new ArgumentNullException(nameof(comparisonService));
        _awardService = awardService
            ?? throw new ArgumentNullException(nameof(awardService));
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        _requestForQuotationId = requestForQuotationId;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isBusy)
        {
            return;
        }

        HideFeedback();
        SetBusy(true);
        AwardContent.Visibility = Visibility.Collapsed;
        StatusPanel.Visibility = Visibility.Visible;
        StatusText.Text = "Carregando adjudicação...";

        try
        {
            _comparison = await _comparisonService.GetComparisonAsync(_requestForQuotationId);
            if (_comparison.Status != RequestForQuotationStatus.Closed)
            {
                throw new InvalidOperationException("A adjudicação só está disponível para RFQs encerrados.");
            }

            _award = await _awardService.GetByRequestForQuotationIdAsync(_requestForQuotationId);
            Populate();
            AwardContent.Visibility = Visibility.Visible;
        }
        catch (AccessDeniedException)
        {
            ShowLoadError("Você não tem permissão para consultar a adjudicação deste RFQ.");
        }
        catch (KeyNotFoundException)
        {
            ShowLoadError("Não foi possível montar a adjudicação porque um registro histórico necessário não foi encontrado.");
        }
        catch (InvalidOperationException exception)
        {
            ShowLoadError(exception.Message);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to load RFQ award decision for {RequestForQuotationId}", _requestForQuotationId);
            ShowLoadError("Não foi possível carregar a adjudicação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void StartAward_Click(object sender, RoutedEventArgs e)
    {
        if (_award is not null)
        {
            return;
        }

        await ExecuteMutationAsync(
            () => _awardService.CreateDraftAsync(_requestForQuotationId),
            "Decisão de adjudicação iniciada. Nenhuma oferta foi selecionada automaticamente.",
            "iniciar a adjudicação");
    }

    private async void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_award?.Status != RequestForQuotationAwardStatus.Draft)
        {
            return;
        }

        var awardId = _award.Id;
        await ExecuteMutationAsync(
            () => _awardService.UpdateDraftAsync(awardId, NotesTextBox.Text),
            "Observação da decisão salva.",
            "salvar a observação da adjudicação");
    }

    private async void AwardSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isPopulating || _isBusy || _award?.Status != RequestForQuotationAwardStatus.Draft)
        {
            return;
        }

        if (sender is not ComboBox
            {
                DataContext: RequestForQuotationAwardLineViewModel line,
                SelectedItem: RequestForQuotationAwardOptionViewModel option,
            })
        {
            return;
        }

        var currentSelection = _award.Lines.SingleOrDefault(
            item => item.RequestForQuotationLineId == line.RequestForQuotationLineId);

        if (option.SupplierQuotationLineId is Guid supplierQuotationLineId)
        {
            if (currentSelection?.SupplierQuotationLineId == supplierQuotationLineId)
            {
                return;
            }

            var awardId = _award.Id;
            await ExecuteMutationAsync(
                () => _awardService.SelectLineAsync(
                    awardId,
                    line.RequestForQuotationLineId,
                    supplierQuotationLineId),
                $"Adjudicação da linha #{line.LineNumber} atualizada.",
                "selecionar a oferta");
            return;
        }

        if (currentSelection is null)
        {
            return;
        }

        var currentAwardId = _award.Id;
        await ExecuteMutationAsync(
            () => _awardService.RemoveLineSelectionAsync(
                currentAwardId,
                line.RequestForQuotationLineId),
            $"Adjudicação da linha #{line.LineNumber} removida.",
            "remover a seleção da linha");
    }

    private async void Finalize_Click(object sender, RoutedEventArgs e)
    {
        if (_award?.Status != RequestForQuotationAwardStatus.Draft)
        {
            return;
        }

        if (_award.Lines.Count == 0)
        {
            ShowOperationError("Selecione ao menos uma oferta antes de finalizar a decisão.");
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            "Finalizar esta decisão de adjudicação? Depois disso as seleções e a observação ficarão somente leitura neste slice.",
            "Confirmar adjudicação",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        var awardId = _award.Id;
        await ExecuteMutationAsync(
            () => _awardService.FinalizeAsync(awardId),
            "Decisão de adjudicação finalizada. Nenhum Pedido de Compra foi criado.",
            "finalizar a adjudicação");
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task ExecuteMutationAsync(
        Func<Task<RequestForQuotationAwardResult>> action,
        string successMessage,
        string operationLabel)
    {
        if (_isBusy)
        {
            return;
        }

        HideFeedback();
        SetBusy(true);
        try
        {
            _award = await action();
            Populate();
            ShowSuccess(successMessage);
        }
        catch (AccessDeniedException)
        {
            Populate();
            ShowOperationError("Você não tem permissão para alterar a decisão de adjudicação.");
        }
        catch (KeyNotFoundException)
        {
            Populate();
            ShowOperationError("Um registro necessário não foi encontrado. Feche a janela, atualize o RFQ e tente novamente.");
        }
        catch (ArgumentException exception)
        {
            Populate();
            ShowOperationError($"Dados inválidos: {exception.Message}");
        }
        catch (InvalidOperationException exception)
        {
            Populate();
            ShowOperationError($"Operação não permitida: {exception.Message}");
        }
        catch (Exception exception)
        {
            Populate();
            Log.Error(
                exception,
                "Failed to {OperationLabel} for RFQ {RequestForQuotationId}",
                operationLabel,
                _requestForQuotationId);
            ShowOperationError($"Não foi possível {operationLabel}. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Populate()
    {
        if (_comparison is null)
        {
            return;
        }

        _isPopulating = true;
        try
        {
            var canEdit = _award?.Status == RequestForQuotationAwardStatus.Draft;
            var selections = _award?.Lines.ToDictionary(
                line => line.RequestForQuotationLineId,
                line => line.SupplierQuotationLineId)
                ?? new Dictionary<Guid, Guid>();
            var supplierById = _comparison.Suppliers.ToDictionary(supplier => supplier.SupplierId);

            var rows = _comparison.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => BuildLine(line, supplierById, selections, canEdit))
                .ToArray();
            AwardLinesItemsControl.ItemsSource = rows;

            NotesTextBox.Text = _award?.Notes ?? string.Empty;
            NotesTextBox.IsReadOnly = !canEdit;
            SaveNotesButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            StartAwardButton.Visibility = _award is null ? Visibility.Visible : Visibility.Collapsed;
            FinalizeButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

            var selectedCount = _award?.Lines.Count ?? 0;
            DecisionStatusText.Text = _award?.Status switch
            {
                RequestForQuotationAwardStatus.Draft => "Decisão: Rascunho",
                RequestForQuotationAwardStatus.Finalized => "Decisão: Finalizada",
                null => "Decisão: Não iniciada",
                _ => "Decisão: Desconhecida",
            };
            DecisionSummaryText.Text = $"{selectedCount} de {_comparison.Lines.Count} linha(s) adjudicada(s)";

            var availableOffers = _comparison.Lines.Sum(
                line => line.Offers.Count(offer =>
                    offer.SupplierQuotationLineId is not null && offer.UnitPrice is not null));
            StatusText.Text = _award?.Status == RequestForQuotationAwardStatus.Finalized
                ? "Decisão finalizada e somente leitura. Este slice não gera Pedido de Compra."
                : $"{availableOffers} oferta(s) finalizada(s) disponível(is) para seleção manual.";
            StatusPanel.Visibility = Visibility.Visible;
        }
        finally
        {
            _isPopulating = false;
        }
    }

    private static RequestForQuotationAwardLineViewModel BuildLine(
        RequestForQuotationComparisonLineResult line,
        IReadOnlyDictionary<Guid, RequestForQuotationComparisonSupplierResult> supplierById,
        IReadOnlyDictionary<Guid, Guid> selections,
        bool canEdit)
    {
        var options = new List<RequestForQuotationAwardOptionViewModel>
        {
            new(null, "— Sem adjudicação —"),
        };

        foreach (var offer in line.Offers)
        {
            if (offer.SupplierQuotationLineId is not Guid supplierQuotationLineId
                || offer.UnitPrice is not decimal unitPrice)
            {
                continue;
            }

            var supplier = supplierById[offer.SupplierId];
            var delivery = offer.PromisedDeliveryDate is DateOnly promised
                ? promised.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)
                : "—";
            options.Add(new RequestForQuotationAwardOptionViewModel(
                supplierQuotationLineId,
                $"{supplier.SupplierName} — {supplier.CurrencyCode ?? "—"} {unitPrice.ToString("0.######", CultureInfo.CurrentCulture)} — entrega {delivery}"));
        }

        var selected = options[0];
        if (selections.TryGetValue(line.RequestForQuotationLineId, out var selectedQuotationLineId))
        {
            selected = options.SingleOrDefault(
                option => option.SupplierQuotationLineId == selectedQuotationLineId)
                ?? throw new KeyNotFoundException(
                    $"Awarded supplier quotation line '{selectedQuotationLineId}' is not available in the finalized RFQ comparison.");
        }

        return new RequestForQuotationAwardLineViewModel(
            line.RequestForQuotationLineId,
            line.LineNumber,
            $"#{line.LineNumber} — {line.Description}",
            $"{line.Quantity.ToString("0.####", CultureInfo.CurrentCulture)} {line.MeasurementUnitName}",
            line.NeededByDate is DateOnly neededBy
                ? $"Necessidade: {neededBy.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)}"
                : "Necessidade: —",
            options,
            selected,
            canEdit,
            options.Count == 1
                ? "Nenhuma oferta finalizada foi registrada para esta linha."
                : $"{options.Count - 1} oferta(s) finalizada(s) disponível(is)." );
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
        RootGrid.IsEnabled = !value;
    }

    private void HideFeedback()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        SuccessPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowLoadError(string message)
    {
        StatusPanel.Visibility = Visibility.Collapsed;
        AwardContent.Visibility = Visibility.Collapsed;
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowOperationError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
        SuccessPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowSuccess(string message)
    {
        SuccessText.Text = message;
        SuccessPanel.Visibility = Visibility.Visible;
        ErrorPanel.Visibility = Visibility.Collapsed;
    }
}

public sealed record RequestForQuotationAwardOptionViewModel(
    Guid? SupplierQuotationLineId,
    string Display);

public sealed class RequestForQuotationAwardLineViewModel(
    Guid requestForQuotationLineId,
    int lineNumber,
    string title,
    string quantityDisplay,
    string neededByDisplay,
    IReadOnlyList<RequestForQuotationAwardOptionViewModel> options,
    RequestForQuotationAwardOptionViewModel selectedOption,
    bool canEdit,
    string offerHint)
{
    public Guid RequestForQuotationLineId { get; } = requestForQuotationLineId;

    public int LineNumber { get; } = lineNumber;

    public string Title { get; } = title;

    public string QuantityDisplay { get; } = quantityDisplay;

    public string NeededByDisplay { get; } = neededByDisplay;

    public IReadOnlyList<RequestForQuotationAwardOptionViewModel> Options { get; } = options;

    public RequestForQuotationAwardOptionViewModel SelectedOption { get; set; } = selectedOption;

    public bool CanEdit { get; } = canEdit;

    public string OfferHint { get; } = offerHint;
}
