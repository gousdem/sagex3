using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SageX3WebApi.Models;
using SageX3WebApi.SageX3SoapClient;

namespace SageX3WebApi.Services;

public interface ILookupService
{
    Task<ApiResponse<GetLookupValuesResponse>> GetLookupValuesAsync(GetLookupValuesRequest request, CancellationToken ct = default);
}

public class LookupService : ILookupService
{
    private readonly ISageX3Client _sage;
    private readonly SageX3Options _options;
    private readonly ILogger<LookupService> _logger;

    public LookupService(ISageX3Client sage, IOptions<SageX3Options> options, ILogger<LookupService> logger)
    {
        _sage = sage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResponse<GetLookupValuesResponse>> GetLookupValuesAsync(
        GetLookupValuesRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.LookupName))
            return ApiResponse<GetLookupValuesResponse>.Fail("LookupName is required.");

        var subProg = _options.SubPrograms.GetLookupValues ?? "YLOOKUP";

        var sb = new StringBuilder();
        sb.Append("<PARAM>");
        SagePayload.AppendField(sb, "LKPNAM", request.LookupName);
        SagePayload.AppendField(sb, "LAN", request.LanguageCode ?? "ENG");
        SagePayload.AppendField(sb, "FLT", request.Filter);
        SagePayload.AppendField(sb, "MAXROW", request.MaxResults.ToString());
        sb.Append("</PARAM>");

        try
        {
            var result = await _sage.RunAsync(subProg, sb.ToString(), ct);
            if (!result.IsSuccess)
                return ApiResponse<GetLookupValuesResponse>.Fail(
                    result.GetFirstErrorMessage() ?? "Sage X3 call failed.", result.Messages);

            var response = new GetLookupValuesResponse { LookupName = request.LookupName };

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                using var doc = JsonDocument.Parse(result.ResultJson);
                var arr = SagePayload.FindArray(doc.RootElement, "values", "VALUES", "items", "DATA");
                if (arr is not null)
                {
                    foreach (var item in arr.Value.EnumerateArray())
                    {
                        var lv = new LookupValue
                        {
                            Code             = SagePayload.Str(item, "code", "CODE", "CODNUM"),
                            Description      = SagePayload.Str(item, "description", "DES", "DESAXX"),
                            ShortDescription = SagePayload.Str(item, "shortDescription", "SHO", "DESSHO")
                        };
                        var active = SagePayload.Str(item, "active", "ENAFLG");
                        lv.Active = string.IsNullOrEmpty(active) || active == "2" ||
                                    active.Equals("true", StringComparison.OrdinalIgnoreCase);

                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in item.EnumerateObject())
                            {
                                if (prop.Value.ValueKind is
                                        JsonValueKind.String or JsonValueKind.Number or
                                        JsonValueKind.True or JsonValueKind.False &&
                                    !lv.Attributes.ContainsKey(prop.Name))
                                {
                                    lv.Attributes[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                                        ? prop.Value.GetString() ?? string.Empty
                                        : prop.Value.ToString();
                                }
                            }
                        }

                        response.Values.Add(lv);
                    }
                }
            }

            response.Count = response.Values.Count;
            return ApiResponse<GetLookupValuesResponse>.Ok(response, result.Messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLookupValues failed for {Name}", request.LookupName);
            return ApiResponse<GetLookupValuesResponse>.Fail(ex.Message);
        }
    }
}
