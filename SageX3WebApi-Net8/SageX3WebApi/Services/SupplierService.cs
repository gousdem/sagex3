using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SageX3WebApi.Models;
using SageX3WebApi.SageX3SoapClient;

namespace SageX3WebApi.Services;

public interface ISupplierService
{
    Task<ApiResponse<Supplier>> GetSupplierAsync(GetSupplierRequest request, CancellationToken ct = default);
    Task<ApiResponse<SupplierInformationSim>> GetSupplierInformationSimAsync(GetSupplierInformationSimRequest request, CancellationToken ct = default);
    Task<ApiResponse<InsertSupplierResponse>> InsertSupplierAsync(InsertSupplierRequest request, CancellationToken ct = default);
}

public class SupplierService : ISupplierService
{
    private readonly ISageX3Client _sage;
    private readonly SageX3Options _options;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(ISageX3Client sage, IOptions<SageX3Options> options, ILogger<SupplierService> logger)
    {
        _sage = sage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<Supplier>> GetSupplierAsync(GetSupplierRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.SupplierCode))
            return ApiResponse<Supplier>.Fail("SupplierCode is required.");

        try
        {
            SoapResponseParser result;
            var subProg = _options.SubPrograms.GetSupplier;

            if (!string.IsNullOrEmpty(subProg))
            {
                var sb = new StringBuilder();
                sb.Append("<PARAM>");
                SagePayload.AppendField(sb, "BPRNUM", request.SupplierCode);
                sb.Append("</PARAM>");
                result = await _sage.RunAsync(subProg, sb.ToString(), ct);
            }
            else
            {
                // Fall back to standard BPS object query.
                var keyXml = SoapEnvelopeBuilder.BuildParamKeyValue("BPRNUM", request.SupplierCode);
                result = await _sage.QueryAsync("BPS", keyXml, 1, ct);
            }

            if (!result.IsSuccess)
                return ApiResponse<Supplier>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var supplier = new Supplier { SupplierCode = request.SupplierCode };
            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                MapSupplier(doc.RootElement, supplier);
            }
            return ApiResponse<Supplier>.Ok(supplier, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSupplier failed for {Code}", request.SupplierCode);
            return ApiResponse<Supplier>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<SupplierInformationSim>> GetSupplierInformationSimAsync(
        GetSupplierInformationSimRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.SupplierCode))
            return ApiResponse<SupplierInformationSim>.Fail("SupplierCode is required.");

        var subProg = _options.SubPrograms.GetSupplierInformationSIM ?? "YSUPSIM";

        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "BPRNUM", request.SupplierCode);
        SagePayload.AppendField(sb, "INCINV", request.IncludeInvoices ? "2" : "1");
        SagePayload.AppendField(sb, "INCPAY", request.IncludePayments ? "2" : "1");
        SagePayload.AppendField(sb, "INCCNT", request.IncludeContacts ? "2" : "1");
        SagePayload.AppendField(sb, "INCBNK", request.IncludeBankDetails ? "2" : "1");
        sb.Append("</PARAM>");

        try
        {
            var result = await _sage.RunAsync(subProg, sb.ToString(), ct);
            if (!result.IsSuccess)
                return ApiResponse<SupplierInformationSim>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var sim = new SupplierInformationSim
            {
                Supplier = new Supplier { SupplierCode = request.SupplierCode }
            };

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                var root = doc.RootElement;

                var supplierEl =
                    (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("supplier", out var s1)) ? s1 :
                    (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("SUPPLIER", out var s2)) ? s2 :
                    root;
                MapSupplier(supplierEl, sim.Supplier);

                if (root.ValueKind == JsonValueKind.Object &&
                    (root.TryGetProperty("statistics", out var stats) ||
                     root.TryGetProperty("STATS", out stats)))
                {
                    sim.Statistics = new SupplierStatistics
                    {
                        YearToDatePurchases = SagePayload.Dec(stats, "ytdPurchases", "YTDPUR"),
                        LastYearPurchases   = SagePayload.Dec(stats, "lastYearPurchases", "LYPUR"),
                        OutstandingBalance  = SagePayload.Dec(stats, "outstandingBalance", "OUTBAL"),
                        OpenInvoiceCount    = (int)SagePayload.Dec(stats, "openInvoiceCount", "OPNINV"),
                        LastInvoiceDate     = SagePayload.Date(stats, "lastInvoiceDate", "LSTINVDAT"),
                        LastPaymentDate     = SagePayload.Date(stats, "lastPaymentDate", "LSTPAYDAT")
                    };
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    (root.TryGetProperty("compliance", out var compEl) ||
                     root.TryGetProperty("COMPLIANCE", out compEl)) &&
                    compEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in compEl.EnumerateArray())
                    {
                        sim.Compliance.Add(new SupplierComplianceItem
                        {
                            Code        = SagePayload.Str(c, "code", "CODE"),
                            Description = SagePayload.Str(c, "description", "DES"),
                            Status      = SagePayload.Str(c, "status", "STA"),
                            ExpiryDate  = SagePayload.Date(c, "expiryDate", "EXPDAT")
                        });
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    (root.TryGetProperty("certifications", out var certEl) ||
                     root.TryGetProperty("CERTIFICATIONS", out certEl)) &&
                    certEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in certEl.EnumerateArray())
                    {
                        sim.Certifications.Add(new SupplierCertification
                        {
                            Code        = SagePayload.Str(c, "code", "CODE"),
                            Description = SagePayload.Str(c, "description", "DES"),
                            IssueDate   = SagePayload.Date(c, "issueDate", "ISSDAT"),
                            ExpiryDate  = SagePayload.Date(c, "expiryDate", "EXPDAT")
                        });
                    }
                }
            }

            return ApiResponse<SupplierInformationSim>.Ok(sim, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSupplierInformationSIM failed for {Code}", request.SupplierCode);
            return ApiResponse<SupplierInformationSim>.Fail(ex.Message);
        }
    }

    public async Task<ApiResponse<InsertSupplierResponse>> InsertSupplierAsync(
        InsertSupplierRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.Name))
            return ApiResponse<InsertSupplierResponse>.Fail("Name is required.");

        try
        {
            SoapResponseParser result;
            var subProg = _options.SubPrograms.InsertSupplier;

            if (!string.IsNullOrEmpty(subProg))
            {
                result = await _sage.RunAsync(subProg, BuildInsertSupplierPayload(request), ct);
            }
            else
            {
                // Fall back to BPS save.
                result = await _sage.SaveAsync("BPS", BuildBpsObjectXml(request), ct);
            }

            if (!result.IsSuccess)
                return ApiResponse<InsertSupplierResponse>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var response = new InsertSupplierResponse
            {
                SupplierCode = request.SupplierCode,
                Status = "OK",
                Message = "Supplier created successfully."
            };

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                var code = SagePayload.Str(doc.RootElement, "supplierCode", "BPRNUM");
                if (!string.IsNullOrEmpty(code)) response.SupplierCode = code;
            }

            return ApiResponse<InsertSupplierResponse>.Ok(response, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertSupplier failed");
            return ApiResponse<InsertSupplierResponse>.Fail(ex.Message);
        }
    }

    // --------------------------------------------------------------------
    // Payload builders
    // --------------------------------------------------------------------

    private static string BuildInsertSupplierPayload(InsertSupplierRequest r)
    {
        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "BPRNUM", r.SupplierCode);
        SagePayload.AppendField(sb, "BPRNAM", r.Name);
        SagePayload.AppendField(sb, "BPRSHO", r.ShortName);
        SagePayload.AppendField(sb, "BCGCOD", r.Category);
        SagePayload.AppendField(sb, "CUR", r.Currency);
        SagePayload.AppendField(sb, "LAN", r.Language);
        SagePayload.AppendField(sb, "CRN", r.TaxId);
        SagePayload.AppendField(sb, "PTE", r.PaymentTerm);
        SagePayload.AppendField(sb, "ACC", r.AccountingCode);

        if (r.Address is not null)
        {
            SagePayload.AppendField(sb, "BPAADDLIG_1", r.Address.Line1);
            SagePayload.AppendField(sb, "BPAADDLIG_2", r.Address.Line2);
            SagePayload.AppendField(sb, "BPAADDLIG_3", r.Address.Line3);
            SagePayload.AppendField(sb, "CTY", r.Address.City);
            SagePayload.AppendField(sb, "SAT", r.Address.State);
            SagePayload.AppendField(sb, "POSCOD", r.Address.PostalCode);
            SagePayload.AppendField(sb, "CRY", r.Address.Country);
            SagePayload.AppendField(sb, "TEL", r.Address.Phone);
            SagePayload.AppendField(sb, "WEB", r.Address.Email);
        }

        if (r.Contacts.Count > 0)
        {
            sb.Append("<GRP NAME=\"CONTACTS\">");
            foreach (var c in r.Contacts)
            {
                sb.Append("<LINE>");
                SagePayload.AppendField(sb, "CNTFNA", c.FirstName);
                SagePayload.AppendField(sb, "CNTLNA", c.LastName);
                SagePayload.AppendField(sb, "CNTFNC", c.Title);
                SagePayload.AppendField(sb, "CNTTEL", c.Phone);
                SagePayload.AppendField(sb, "CNTEMA", c.Email);
                sb.Append("</LINE>");
            }
            sb.Append("</GRP>");
        }

        if (r.BankIds.Count > 0)
        {
            sb.Append("<GRP NAME=\"BANKIDS\">");
            foreach (var b in r.BankIds)
            {
                sb.Append("<LINE>");
                SagePayload.AppendField(sb, "BIDNUM", b.BankIdCode);
                SagePayload.AppendField(sb, "BANNUM", b.AccountNumber);
                SagePayload.AppendField(sb, "BANRIB", b.RoutingNumber);
                SagePayload.AppendField(sb, "IBAN", b.Iban);
                SagePayload.AppendField(sb, "BICCOD", b.Swift);
                SagePayload.AppendField(sb, "CUR", b.Currency);
                sb.Append("</LINE>");
            }
            sb.Append("</GRP>");
        }

        sb.Append("</PARAM>");
        return sb.ToString();
    }

    private static string BuildBpsObjectXml(InsertSupplierRequest r)
    {
        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        sb.Append("<GRP ID=\"BPS0\">");
        SagePayload.AppendField(sb, "BPRNUM", r.SupplierCode);
        SagePayload.AppendField(sb, "BPSNAM", r.Name);
        SagePayload.AppendField(sb, "BPSSHO", r.ShortName);
        SagePayload.AppendField(sb, "BCGCOD", r.Category);
        SagePayload.AppendField(sb, "CUR", r.Currency);
        SagePayload.AppendField(sb, "LAN", r.Language);
        SagePayload.AppendField(sb, "CRN", r.TaxId);
        SagePayload.AppendField(sb, "PTE", r.PaymentTerm);
        SagePayload.AppendField(sb, "ACC", r.AccountingCode);
        sb.Append("</GRP>");

        if (r.Address is not null)
        {
            sb.Append("<TAB ID=\"BPA1\">");
            sb.Append("<LIN>");
            SagePayload.AppendField(sb, "BPAADD", "MAIN");
            SagePayload.AppendField(sb, "BPADES", r.Address.Description ?? "Main");
            SagePayload.AppendField(sb, "CRY", r.Address.Country);
            SagePayload.AppendField(sb, "BPAADDLIG_1", r.Address.Line1);
            SagePayload.AppendField(sb, "BPAADDLIG_2", r.Address.Line2);
            SagePayload.AppendField(sb, "BPAADDLIG_3", r.Address.Line3);
            SagePayload.AppendField(sb, "CTY", r.Address.City);
            SagePayload.AppendField(sb, "SAT", r.Address.State);
            SagePayload.AppendField(sb, "POSCOD", r.Address.PostalCode);
            SagePayload.AppendField(sb, "TEL_1", r.Address.Phone);
            SagePayload.AppendField(sb, "WEB_1", r.Address.Email);
            SagePayload.AppendField(sb, "BPAPAY", "2");
            sb.Append("</LIN>");
            sb.Append("</TAB>");
        }

        if (r.Contacts.Count > 0)
        {
            sb.Append("<TAB ID=\"CNT1\">");
            foreach (var c in r.Contacts)
            {
                sb.Append("<LIN>");
                SagePayload.AppendField(sb, "CNTFNA", c.FirstName);
                SagePayload.AppendField(sb, "CNTLNA", c.LastName);
                SagePayload.AppendField(sb, "CNTFNC", c.Title);
                SagePayload.AppendField(sb, "CNTTEL", c.Phone);
                SagePayload.AppendField(sb, "CNTEMA", c.Email);
                sb.Append("</LIN>");
            }
            sb.Append("</TAB>");
        }

        if (r.BankIds.Count > 0)
        {
            sb.Append("<TAB ID=\"BID1\">");
            foreach (var b in r.BankIds)
            {
                sb.Append("<LIN>");
                SagePayload.AppendField(sb, "BIDNUM", b.BankIdCode);
                SagePayload.AppendField(sb, "BANNUM", b.AccountNumber);
                SagePayload.AppendField(sb, "BANRIB", b.RoutingNumber);
                SagePayload.AppendField(sb, "IBAN", b.Iban);
                SagePayload.AppendField(sb, "BICCOD", b.Swift);
                SagePayload.AppendField(sb, "CUR", b.Currency);
                sb.Append("</LIN>");
            }
            sb.Append("</TAB>");
        }

        sb.Append("</PARAM>");
        return sb.ToString();
    }

    // --------------------------------------------------------------------
    // JSON -> domain mapping
    // --------------------------------------------------------------------

    private static void MapSupplier(JsonElement t, Supplier s)
    {
        if (t.ValueKind != JsonValueKind.Object) return;

        s.SupplierCode   = SagePayload.Str(t, "supplierCode", "BPRNUM") ?? s.SupplierCode;
        s.Name           = SagePayload.Str(t, "name", "BPRNAM", "BPSNAM");
        s.ShortName      = SagePayload.Str(t, "shortName", "BPRSHO");
        s.Category       = SagePayload.Str(t, "category", "BCGCOD");
        s.Currency       = SagePayload.Str(t, "currency", "CUR");
        s.Language       = SagePayload.Str(t, "language", "LAN");
        s.TaxId          = SagePayload.Str(t, "taxId", "CRN");
        s.PaymentTerm    = SagePayload.Str(t, "paymentTerm", "PTE");
        s.AccountingCode = SagePayload.Str(t, "accountingCode", "ACC");
        var active = SagePayload.Str(t, "active", "ENAFLG");
        s.Active = string.IsNullOrEmpty(active) || active == "2" ||
                   active.Equals("true", StringComparison.OrdinalIgnoreCase);

        if ((t.TryGetProperty("addresses", out var addrEl) ||
             t.TryGetProperty("ADDRESSES", out addrEl)) &&
            addrEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in addrEl.EnumerateArray())
            {
                var addr = new SupplierAddress
                {
                    AddressCode = SagePayload.Str(a, "addressCode", "BPAADD"),
                    Description = SagePayload.Str(a, "description", "BPADES"),
                    Line1       = SagePayload.Str(a, "line1", "BPAADDLIG_1"),
                    Line2       = SagePayload.Str(a, "line2", "BPAADDLIG_2"),
                    Line3       = SagePayload.Str(a, "line3", "BPAADDLIG_3"),
                    City        = SagePayload.Str(a, "city", "CTY"),
                    State       = SagePayload.Str(a, "state", "SAT"),
                    PostalCode  = SagePayload.Str(a, "postalCode", "POSCOD"),
                    Country     = SagePayload.Str(a, "country", "CRY"),
                    Phone       = SagePayload.Str(a, "phone", "TEL_1"),
                    Email       = SagePayload.Str(a, "email", "WEB_1"),
                    IsDefault   = SagePayload.Str(a, "isDefault", "BPAPAY") == "2"
                };
                s.Addresses.Add(addr);
                if (addr.IsDefault && s.DefaultAddress is null) s.DefaultAddress = addr;
            }
            if (s.DefaultAddress is null && s.Addresses.Count > 0)
                s.DefaultAddress = s.Addresses[0];
        }

        if ((t.TryGetProperty("contacts", out var cntEl) ||
             t.TryGetProperty("CONTACTS", out cntEl)) &&
            cntEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in cntEl.EnumerateArray())
            {
                s.Contacts.Add(new SupplierContact
                {
                    ContactCode = SagePayload.Str(c, "contactCode", "CNTCOD"),
                    FirstName   = SagePayload.Str(c, "firstName", "CNTFNA"),
                    LastName    = SagePayload.Str(c, "lastName", "CNTLNA"),
                    Title       = SagePayload.Str(c, "title", "CNTFNC"),
                    Phone       = SagePayload.Str(c, "phone", "CNTTEL"),
                    Email       = SagePayload.Str(c, "email", "CNTEMA")
                });
            }
        }

        if ((t.TryGetProperty("bankIds", out var bidEl) ||
             t.TryGetProperty("BANKIDS", out bidEl)) &&
            bidEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var b in bidEl.EnumerateArray())
            {
                s.BankIds.Add(new SupplierBankId
                {
                    BankIdCode    = SagePayload.Str(b, "bankIdCode", "BIDNUM"),
                    AccountNumber = SagePayload.Str(b, "accountNumber", "BANNUM"),
                    RoutingNumber = SagePayload.Str(b, "routingNumber", "BANRIB"),
                    Iban          = SagePayload.Str(b, "iban", "IBAN"),
                    Swift         = SagePayload.Str(b, "swift", "BICCOD"),
                    Currency      = SagePayload.Str(b, "currency", "CUR"),
                    IsDefault     = SagePayload.Str(b, "isDefault", "BIDDFT") == "2"
                });
            }
        }
    }
}
