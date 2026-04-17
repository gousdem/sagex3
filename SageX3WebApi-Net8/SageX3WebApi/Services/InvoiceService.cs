using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SageX3WebApi.Models;
using SageX3WebApi.SageX3SoapClient;

namespace SageX3WebApi.Services;

public interface IInvoiceService
{
    Task<ApiResponse<GetInvoiceOk2PayResponse>> GetInvoiceOk2PayAsync(GetInvoiceOk2PayRequest request, CancellationToken ct = default);
    Task<ApiResponse<InsertInvoicePaymentResponse>> InsertInvoicePaymentAsync(InsertInvoicePaymentRequest request, CancellationToken ct = default);
}

public class InvoiceService : IInvoiceService
{
    private readonly ISageX3Client _sage;
    private readonly SageX3Options _options;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(ISageX3Client sage, IOptions<SageX3Options> options, ILogger<InvoiceService> logger)
    {
        _sage = sage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<GetInvoiceOk2PayResponse>> GetInvoiceOk2PayAsync(
        GetInvoiceOk2PayRequest request, CancellationToken ct = default)
    {
        var subProg = _options.SubPrograms.GetInvoiceOk2Pay ?? "YOK2PAY";

        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "CPY", request.Company);
        SagePayload.AppendField(sb, "FCY", request.Site);
        SagePayload.AppendField(sb, "BPR", request.SupplierCode);
        SagePayload.AppendField(sb, "NUM", request.InvoiceNumber);
        SagePayload.AppendField(sb, "CUR", request.Currency);
        SagePayload.AppendField(sb, "DUDDATFRO", SagePayload.FormatDate(request.DueDateFrom));
        SagePayload.AppendField(sb, "DUDDATTO",  SagePayload.FormatDate(request.DueDateTo));
        sb.Append("</PARAM>");

        try
        {
            var result = await _sage.RunAsync(subProg, sb.ToString(), ct);
            if (!result.IsSuccess)
                return ApiResponse<GetInvoiceOk2PayResponse>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var response = new GetInvoiceOk2PayResponse();

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                var arr = SagePayload.FindArray(doc.RootElement, "invoices", "INVOICES", "items", "data");
                if (arr is not null)
                {
                    foreach (var item in arr.Value.EnumerateArray())
                    {
                        response.Invoices.Add(MapInvoice(item));
                    }
                }
            }

            response.Count = response.Invoices.Count;
            return ApiResponse<GetInvoiceOk2PayResponse>.Ok(response, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInvoiceOk2Pay failed");
            return ApiResponse<GetInvoiceOk2PayResponse>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<InsertInvoicePaymentResponse>> InsertInvoicePaymentAsync(
        InsertInvoicePaymentRequest request, CancellationToken ct = default)
    {
        var subProg = _options.SubPrograms.InsertInvoicePayment ?? "YINVPAY";

        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "CPY", request.Company);
        SagePayload.AppendField(sb, "FCY", request.Site);
        SagePayload.AppendField(sb, "PAMTYP", request.PaymentEntryType);
        SagePayload.AppendField(sb, "BAN", request.BankCode);
        SagePayload.AppendField(sb, "PAMDAT", SagePayload.FormatDate(request.PaymentDate));
        SagePayload.AppendField(sb, "BPR", request.SupplierCode);
        SagePayload.AppendField(sb, "CUR", request.Currency);
        SagePayload.AppendField(sb, "AMTCUR", SagePayload.FormatDecimal(request.Amount));
        SagePayload.AppendField(sb, "REF", request.Reference);
        SagePayload.AppendField(sb, "DES", request.Description);

        if (request.Lines.Count > 0)
        {
            sb.Append("<GRP NAME=\"LINES\">");
            foreach (var line in request.Lines)
            {
                sb.Append("<LINE>");
                SagePayload.AppendField(sb, "NUM", line.InvoiceNumber);
                SagePayload.AppendField(sb, "AMTPAY", SagePayload.FormatDecimal(line.AmountPaid));
                if (line.DiscountTaken.HasValue)
                    SagePayload.AppendField(sb, "DISAMT", SagePayload.FormatDecimal(line.DiscountTaken.Value));
                sb.Append("</LINE>");
            }
            sb.Append("</GRP>");
        }
        sb.Append("</PARAM>");

        try
        {
            var result = await _sage.RunAsync(subProg, sb.ToString(), ct);
            if (!result.IsSuccess)
                return ApiResponse<InsertInvoicePaymentResponse>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var response = new InsertInvoicePaymentResponse
            {
                Status = "OK",
                Message = "Payment created successfully."
            };

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                response.PaymentNumber = SagePayload.Str(doc.RootElement, "paymentNumber", "PAMNUM", "NUM");
            }

            return ApiResponse<InsertInvoicePaymentResponse>.Ok(response, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertInvoicePayment failed");
            return ApiResponse<InsertInvoicePaymentResponse>.Fail(ex.Message);
        }
    }

    private static InvoiceOk2Pay MapInvoice(JsonElement item) => new()
    {
        InvoiceNumber = SagePayload.Str(item, "invoiceNumber", "NUM", "num"),
        Company       = SagePayload.Str(item, "company", "CPY"),
        Site          = SagePayload.Str(item, "site", "FCY"),
        SupplierCode  = SagePayload.Str(item, "supplierCode", "BPR", "BPRNUM"),
        SupplierName  = SagePayload.Str(item, "supplierName", "BPRNAM"),
        InvoiceDate   = SagePayload.Date(item, "invoiceDate", "ACCDAT"),
        DueDate       = SagePayload.Date(item, "dueDate", "DUDDAT"),
        AmountGross   = SagePayload.Dec(item, "amountGross", "AMTATIDOC", "AMTATI"),
        AmountOpen    = SagePayload.Dec(item, "amountOpen", "AMTDUE"),
        Currency      = SagePayload.Str(item, "currency", "CUR"),
        PaymentTerm   = SagePayload.Str(item, "paymentTerm", "PTE"),
        Ok2PayFlag    = SagePayload.Str(item, "ok2Pay", "OK2PAY"),
        Reference     = SagePayload.Str(item, "reference", "BPRVCR")
    };
}
