using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SageX3WebApi.Models;
using SageX3WebApi.SageX3SoapClient;

namespace SageX3WebApi.Services;

public interface IRemitToService
{
    Task<ApiResponse<InsertRemitToAddressResponse>> InsertRemitToAddressAsync(InsertRemitToAddressRequest request, CancellationToken ct = default);
}

public class RemitToService : IRemitToService
{
    private readonly ISageX3Client _sage;
    private readonly SageX3Options _options;
    private readonly ILogger<RemitToService> _logger;

    public RemitToService(ISageX3Client sage, IOptions<SageX3Options> options, ILogger<RemitToService> logger)
    {
        _sage = sage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<InsertRemitToAddressResponse>> InsertRemitToAddressAsync(
        InsertRemitToAddressRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.SupplierCode))
            return ApiResponse<InsertRemitToAddressResponse>.Fail("SupplierCode is required.");
        if (string.IsNullOrEmpty(request.AddressCode))
            return ApiResponse<InsertRemitToAddressResponse>.Fail("AddressCode is required.");

        try
        {
            SoapResponseParser result;
            var subProg = _options.SubPrograms.InsertRemitToAddress;

            if (!string.IsNullOrEmpty(subProg))
            {
                result = await _sage.RunAsync(subProg, BuildRemitToPayload(request), ct);
            }
            else
            {
                // Fall back to BPA save.
                result = await _sage.SaveAsync("BPA", BuildBpaObjectXml(request), ct);
            }

            if (!result.IsSuccess)
                return ApiResponse<InsertRemitToAddressResponse>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var response = new InsertRemitToAddressResponse
            {
                SupplierCode = request.SupplierCode,
                AddressCode  = request.AddressCode,
                BankIdCode   = request.BankIdCode,
                Status       = "OK",
                Message      = "Remit-to address created successfully."
            };

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                var bid = SagePayload.Str(doc.RootElement, "bankIdCode", "BIDNUM");
                if (!string.IsNullOrEmpty(bid)) response.BankIdCode = bid;
            }

            return ApiResponse<InsertRemitToAddressResponse>.Ok(response, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertRemitToAddress failed for {Code}", request.SupplierCode);
            return ApiResponse<InsertRemitToAddressResponse>.Fail(ex.Message);
        }
    }

    private static string BuildRemitToPayload(InsertRemitToAddressRequest r)
    {
        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "BPRNUM", r.SupplierCode);
        SagePayload.AppendField(sb, "BPAADD", r.AddressCode);
        SagePayload.AppendField(sb, "BPADES", r.Description);
        SagePayload.AppendField(sb, "CRY", r.Country);
        SagePayload.AppendField(sb, "BPAADDLIG_1", r.Line1);
        SagePayload.AppendField(sb, "BPAADDLIG_2", r.Line2);
        SagePayload.AppendField(sb, "BPAADDLIG_3", r.Line3);
        SagePayload.AppendField(sb, "CTY", r.City);
        SagePayload.AppendField(sb, "SAT", r.State);
        SagePayload.AppendField(sb, "POSCOD", r.PostalCode);
        SagePayload.AppendField(sb, "TEL_1", r.Phone);
        SagePayload.AppendField(sb, "FAX_1", r.Fax);
        SagePayload.AppendField(sb, "WEB_1", r.Email);
        SagePayload.AppendField(sb, "BPAPAY", r.IsDefaultPayAddress ? "2" : "1");

        if (!string.IsNullOrEmpty(r.BankIdCode) || !string.IsNullOrEmpty(r.AccountNumber))
        {
            SagePayload.AppendField(sb, "BIDNUM", r.BankIdCode);
            SagePayload.AppendField(sb, "BANNUM", r.AccountNumber);
            SagePayload.AppendField(sb, "BANRIB", r.RoutingNumber);
            SagePayload.AppendField(sb, "IBAN", r.Iban);
            SagePayload.AppendField(sb, "BICCOD", r.Swift);
            SagePayload.AppendField(sb, "CUR", r.Currency);
        }
        sb.Append("</PARAM>");
        return sb.ToString();
    }

    private static string BuildBpaObjectXml(InsertRemitToAddressRequest r)
    {
        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        sb.Append("<GRP ID=\"BPA0\">");
        SagePayload.AppendField(sb, "BPANUM", r.SupplierCode);
        SagePayload.AppendField(sb, "BPAADD", r.AddressCode);
        SagePayload.AppendField(sb, "BPADES", r.Description);
        SagePayload.AppendField(sb, "CRY", r.Country);
        SagePayload.AppendField(sb, "BPAADDLIG_1", r.Line1);
        SagePayload.AppendField(sb, "BPAADDLIG_2", r.Line2);
        SagePayload.AppendField(sb, "BPAADDLIG_3", r.Line3);
        SagePayload.AppendField(sb, "CTY", r.City);
        SagePayload.AppendField(sb, "SAT", r.State);
        SagePayload.AppendField(sb, "POSCOD", r.PostalCode);
        SagePayload.AppendField(sb, "TEL_1", r.Phone);
        SagePayload.AppendField(sb, "FAX_1", r.Fax);
        SagePayload.AppendField(sb, "WEB_1", r.Email);
        SagePayload.AppendField(sb, "BPAPAY", r.IsDefaultPayAddress ? "2" : "1");
        sb.Append("</GRP>");
        sb.Append("</PARAM>");
        return sb.ToString();
    }
}
