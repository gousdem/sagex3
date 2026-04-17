using System.Text;

namespace SageX3WebApi.SageX3SoapClient;

/// <summary>
/// Builds SOAP envelopes for the Sage X3 CAdxWebServiceXmlCC service.
/// Three operations are supported:
///   run   - invoke an ADX sub-program by publicName with an inputXml payload.
///   save  - create/update a standard business object (BPS, BPA, PIH, PAYO...).
///   query - read a business object by primary key(s).
/// </summary>
public static class SoapEnvelopeBuilder
{
    private const string WssNs     = "http://www.adonix.com/WSS";
    private const string SoapEnvNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string XsiNs     = "http://www.w3.org/2001/XMLSchema-instance";
    private const string XsdNs     = "http://www.w3.org/2001/XMLSchema";

    public static string BuildRunEnvelope(
        SageX3Options options, string publicName, string inputXml)
    {
        var sb = new StringBuilder(2048);
        AppendEnvelopeStart(sb, "run");
        AppendCallContext(sb, options);
        sb.Append("<publicName xsi:type=\"xsd:string\">").Append(XmlEscape(publicName)).Append("</publicName>");
        sb.Append("<inputXml xsi:type=\"xsd:string\">").Append(XmlEscape(inputXml)).Append("</inputXml>");
        AppendEnvelopeEnd(sb, "run");
        return sb.ToString();
    }

    public static string BuildSaveEnvelope(
        SageX3Options options, string publicName, string objectXml)
    {
        var sb = new StringBuilder(2048);
        AppendEnvelopeStart(sb, "save");
        AppendCallContext(sb, options);
        sb.Append("<publicName xsi:type=\"xsd:string\">").Append(XmlEscape(publicName)).Append("</publicName>");
        sb.Append("<objectXml xsi:type=\"xsd:string\">").Append(XmlEscape(objectXml)).Append("</objectXml>");
        AppendEnvelopeEnd(sb, "save");
        return sb.ToString();
    }

    public static string BuildQueryEnvelope(
        SageX3Options options, string publicName, string objectKeysXml, int listSize = 100)
    {
        var sb = new StringBuilder(2048);
        AppendEnvelopeStart(sb, "query");
        AppendCallContext(sb, options);
        sb.Append("<publicName xsi:type=\"xsd:string\">").Append(XmlEscape(publicName)).Append("</publicName>");
        sb.Append("<objectKeys xsi:type=\"wss:ArrayOfCAdxParamKeyValue\" soapenc:arrayType=\"wss:CAdxParamKeyValue[]\" xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\">");
        sb.Append(objectKeysXml);
        sb.Append("</objectKeys>");
        sb.Append("<listSize xsi:type=\"xsd:int\">").Append(listSize).Append("</listSize>");
        AppendEnvelopeEnd(sb, "query");
        return sb.ToString();
    }

    public static string BuildParamKeyValue(string key, string value)
    {
        return $"<CAdxParamKeyValue xsi:type=\"wss:CAdxParamKeyValue\">" +
               $"<key xsi:type=\"xsd:string\">{XmlEscape(key)}</key>" +
               $"<value xsi:type=\"xsd:string\">{XmlEscape(value)}</value>" +
               $"</CAdxParamKeyValue>";
    }

    private static void AppendEnvelopeStart(StringBuilder sb, string op)
    {
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append("<soapenv:Envelope xmlns:soapenv=\"").Append(SoapEnvNs).Append("\" ");
        sb.Append("xmlns:wss=\"").Append(WssNs).Append("\" ");
        sb.Append("xmlns:xsi=\"").Append(XsiNs).Append("\" ");
        sb.Append("xmlns:xsd=\"").Append(XsdNs).Append("\">");
        sb.Append("<soapenv:Header/>");
        sb.Append("<soapenv:Body>");
        sb.Append("<wss:").Append(op).Append(" soapenv:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
    }

    private static void AppendEnvelopeEnd(StringBuilder sb, string op)
    {
        sb.Append("</wss:").Append(op).Append(">");
        sb.Append("</soapenv:Body>");
        sb.Append("</soapenv:Envelope>");
    }

    private static void AppendCallContext(StringBuilder sb, SageX3Options options)
    {
        sb.Append("<callContext xsi:type=\"wss:CAdxCallContext\">");
        sb.Append("<codeLang xsi:type=\"xsd:string\">").Append(XmlEscape(options.Language)).Append("</codeLang>");
        sb.Append("<poolAlias xsi:type=\"xsd:string\">").Append(XmlEscape(options.PoolAlias)).Append("</poolAlias>");
        sb.Append("<poolId xsi:type=\"xsd:string\"></poolId>");
        sb.Append("<requestConfig xsi:type=\"xsd:string\">").Append(XmlEscape(options.RequestConfig)).Append("</requestConfig>");
        sb.Append("</callContext>");
    }

    /// <summary>
    /// Escapes a string so it is safe to embed inside XML text content.
    /// The Sage X3 inputXml / objectXml parameters are ESCAPED strings, not raw XML children.
    /// </summary>
    public static string XmlEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value
            .Replace("&",  "&amp;")
            .Replace("<",  "&lt;")
            .Replace(">",  "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'",  "&apos;");
    }
}
