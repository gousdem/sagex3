using System.Xml.Linq;

namespace SageX3WebApi.SageX3SoapClient;

/// <summary>
/// Parses a SOAP response from CAdxWebServiceXmlCC (run/save/query).
/// </summary>
public class SoapResponseParser
{
    public int Status { get; init; }
    public bool IsSuccess => Status == 1;
    public string? ResultXml { get; init; }
    public string? ResultJson { get; init; }
    public IReadOnlyList<SageMessage> Messages { get; init; } = Array.Empty<SageMessage>();
    public string? RawResponse { get; init; }

    public string? GetFirstErrorMessage() =>
        Messages.FirstOrDefault(m =>
            string.Equals(m.Type, "ERROR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.Type, "FAULT", StringComparison.OrdinalIgnoreCase))?.Message;

    public static SoapResponseParser Parse(string soapXml)
    {
        if (string.IsNullOrWhiteSpace(soapXml))
            return new SoapResponseParser { RawResponse = soapXml };

        var messages = new List<SageMessage>();

        try
        {
            var doc = XDocument.Parse(soapXml);

            // SOAP fault short-circuit
            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault is not null)
            {
                var faultString = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring");
                messages.Add(new SageMessage("FAULT", faultString?.Value ?? "Unknown SOAP fault"));
                return new SoapResponseParser
                {
                    Status = 0,
                    Messages = messages,
                    RawResponse = soapXml
                };
            }

            // status
            int status = 0;
            var statusEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "status");
            if (statusEl is not null && int.TryParse(statusEl.Value, out int s))
                status = s;

            // messages
            foreach (var m in doc.Descendants().Where(e => e.Name.LocalName == "messages"))
            {
                var typeEl = m.Elements().FirstOrDefault(x => x.Name.LocalName == "type");
                var msgEl  = m.Elements().FirstOrDefault(x => x.Name.LocalName == "message");
                messages.Add(new SageMessage(typeEl?.Value, msgEl?.Value));
            }

            // resultXml (contains JSON when adxwss.optreturn=JSON)
            string? resultXml = null;
            string? resultJson = null;
            var resultEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "resultXml");
            if (resultEl is not null)
            {
                var raw = resultEl.Value;
                if (!string.IsNullOrEmpty(raw))
                {
                    var trimmed = raw.TrimStart();
                    if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
                        resultJson = raw;
                    else
                        resultXml = raw;
                }
            }

            return new SoapResponseParser
            {
                Status = status,
                Messages = messages,
                ResultXml = resultXml,
                ResultJson = resultJson,
                RawResponse = soapXml
            };
        }
        catch (Exception ex)
        {
            messages.Add(new SageMessage("PARSE_ERROR", ex.Message));
            return new SoapResponseParser
            {
                Status = 0,
                Messages = messages,
                RawResponse = soapXml
            };
        }
    }
}

public record SageMessage(string? Type, string? Message);
