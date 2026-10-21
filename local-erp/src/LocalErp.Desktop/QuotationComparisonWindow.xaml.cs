using System.Globalization;
using System.Windows;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using Serilog;

namespace LocalErp.Desktop;

public partial class QuotationComparisonWindow : Window
{
    private readonly RequestForQuotationComparisonService _comparisonService;
    private readonly Guid _requestForQuotationId;
    private bool _isLoading;

    public QuotationComparisonWindow(
        RequestForQuotationComparisonService comparisonService,
        Guid requestForQuotationId)
    {
        _comparisonService = comparisonService
            ?? throw new ArgumentNullException(nameof(comparisonService));
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        _requestForQuotationId = requestForQuotationId;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        ErrorPanel.Visibility = Visibility.Collapsed;
        ComparisonContent.Visibility = Visibility.Collapsed;
        StatusPanel.Visibility = Visibility.Visible;
        StatusText.Text = "Carregando comparação...";

        try
        {
            var result = await _comparisonService.GetComparisonAsync(_requestForQuotationId);
            DataContext = QuotationComparisonWindowViewModel.FromResult(result);
            StatusText.Text = result.Suppliers.Count == 0
                ? "Este RFQ ainda não possui fornecedores convidados."
                : $"{result.Suppliers.Count} fornecedor(es) convidado(s); somente respostas finalizadas entram nos preços comparados.";
            ComparisonContent.Visibility = Visibility.Visible;
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para consultar a comparação deste RFQ.");
        }
        catch (KeyNotFoundException)
        {
            ShowError("Não foi possível montar a comparação porque um registro histórico necessário não foi encontrado.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to load quotation comparison for RFQ {RequestForQuotationId}", _requestForQuotationId);
            ShowError("Não foi possível carregar a comparação. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowError(string message)
    {
        StatusPanel.Visibility = Visibility.Collapsed;
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}

public sealed record QuotationComparisonSupplierViewModel(
    Guid SupplierId,
    string Name,
    string RegistrationStatusDisplay,
    string ResponseStatusDisplay,
    string CurrencyDisplay,
    string ReferenceDisplay,
    string ValidityDisplay,
    string FinalizedDisplay);

public sealed record QuotationComparisonCellViewModel(
    Guid SupplierId,
    string PriceDisplay,
    string AvailabilityDisplay,
    string DeliveryDisplay,
    string NotesDisplay);

public sealed record QuotationComparisonLineViewModel(
    Guid RequestForQuotationLineId,
    string Title,
    string QuantityDisplay,
    string NeededByDisplay,
    IReadOnlyList<QuotationComparisonCellViewModel> Cells);

public sealed record QuotationComparisonWindowViewModel(
    Guid RequestForQuotationId,
    string RfqStatusDisplay,
    string SummaryDisplay,
    IReadOnlyList<QuotationComparisonSupplierViewModel> Suppliers,
    IReadOnlyList<QuotationComparisonLineViewModel> Lines)
{
    public static QuotationComparisonWindowViewModel FromResult(RequestForQuotationComparisonResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var suppliers = result.Suppliers
            .Select(MapSupplier)
            .ToArray();
        var supplierReferences = result.Suppliers.ToDictionary(supplier => supplier.SupplierId);

        var lines = result.Lines
            .Select(line => new QuotationComparisonLineViewModel(
                line.RequestForQuotationLineId,
                $"#{line.LineNumber} — {line.Description}",
                $"{line.Quantity.ToString("0.####", CultureInfo.CurrentCulture)} {line.MeasurementUnitName}",
                line.NeededByDate is DateOnly neededBy
                    ? $"Necessidade: {neededBy.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)}"
                    : "Necessidade: —",
                line.Offers
                    .Select(offer => MapCell(offer, supplierReferences[offer.SupplierId]))
                    .ToArray()))
            .ToArray();

        var finalizedCount = result.Suppliers.Count(
            supplier => supplier.ResponseStatus == SupplierQuotationStatus.Finalized);

        return new QuotationComparisonWindowViewModel(
            result.RequestForQuotationId,
            $"Status: {RfqStatusLabel(result.Status)}",
            $"{result.Suppliers.Count} fornecedor(es) · {finalizedCount} resposta(s) finalizada(s) · {result.Lines.Count} linha(s)",
            suppliers,
            lines);
    }

    private static QuotationComparisonSupplierViewModel MapSupplier(
        RequestForQuotationComparisonSupplierResult supplier)
    {
        var responseStatus = supplier.ResponseStatus switch
        {
            SupplierQuotationStatus.Draft => "Resposta: Rascunho",
            SupplierQuotationStatus.Finalized => "Resposta: Finalizada",
            null => "Resposta: Sem resposta",
            _ => "Resposta: Desconhecida",
        };

        return new QuotationComparisonSupplierViewModel(
            supplier.SupplierId,
            supplier.SupplierName,
            supplier.SupplierIsActive ? "Cadastro: Ativo" : "Cadastro: Inativo",
            responseStatus,
            $"Moeda: {supplier.CurrencyCode ?? "—"}",
            $"Referência: {supplier.SupplierReference ?? "—"}",
            supplier.ValidUntilDate is DateOnly validUntil
                ? $"Validade: {validUntil.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)}"
                : "Validade: —",
            supplier.FinalizedAtUtc is DateTime finalizedAtUtc
                ? $"Finalizada: {finalizedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)}"
                : "Finalizada: —");
    }

    private static QuotationComparisonCellViewModel MapCell(
        RequestForQuotationComparisonOfferResult offer,
        RequestForQuotationComparisonSupplierResult supplier)
    {
        if (supplier.ResponseStatus != SupplierQuotationStatus.Finalized)
        {
            return new QuotationComparisonCellViewModel(
                supplier.SupplierId,
                "—",
                supplier.ResponseStatus == SupplierQuotationStatus.Draft
                    ? "Resposta em rascunho"
                    : "Sem resposta finalizada",
                "Entrega: —",
                "Observação: —");
        }

        if (offer.UnitPrice is not decimal unitPrice)
        {
            return new QuotationComparisonCellViewModel(
                supplier.SupplierId,
                "—",
                "Linha não cotada",
                "Entrega: —",
                "Observação: —");
        }

        return new QuotationComparisonCellViewModel(
            supplier.SupplierId,
            $"{supplier.CurrencyCode ?? "—"} {unitPrice.ToString("0.######", CultureInfo.CurrentCulture)}",
            "Cotada",
            offer.PromisedDeliveryDate is DateOnly promised
                ? $"Entrega: {promised.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture)}"
                : "Entrega: —",
            string.IsNullOrWhiteSpace(offer.Notes)
                ? "Observação: —"
                : $"Observação: {offer.Notes}");
    }

    private static string RfqStatusLabel(RequestForQuotationStatus status)
    {
        return status switch
        {
            RequestForQuotationStatus.Draft => "Rascunho",
            RequestForQuotationStatus.Sent => "Enviada",
            RequestForQuotationStatus.Closed => "Encerrada",
            RequestForQuotationStatus.Cancelled => "Cancelada",
            _ => status.ToString(),
        };
    }
}
