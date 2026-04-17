using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SageX3WebApi.Services;

/// <summary>
/// Helpers for building Sage X3 PARAM/GRP/FLD payloads and parsing JSON responses.
/// Uses System.Text.Json (JsonElement / JsonDocument) throughout.
/// </summary>
internal static class SagePayload
{
    public static void AppendField(StringBuilder sb, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        sb.Append("<FLD NAME=\"").Append(name).Append("\">")
          .Append(XmlEscape(value))
          .Append("</FLD>");
    }

    public static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public static string FormatDecimal(decimal value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    public static string XmlEscape(string? s) =>
        string.IsNullOrEmpty(s) ? string.Empty
        : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ----- JSON extraction helpers ------------------------------------------

    public static string? Str(JsonElement el, params string[] names)
    {
        foreach (var n in names)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(n, out var v))
            {
                return v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => v.ToString()
                };
            }
        }
        return null;
    }

    public static decimal Dec(JsonElement el, params string[] names)
    {
        foreach (var n in names)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(n, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
                if (v.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d2))
                    return d2;
            }
        }
        return 0m;
    }

    public static DateTime? Date(JsonElement el, params string[] names)
    {
        foreach (var n in names)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(n, out var v))
            {
                var s = v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                if (string.IsNullOrEmpty(s)) continue;
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
                if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            }
        }
        return null;
    }

    /// <summary>Return the first array found under any of the given property names, or the root if it is already an array.</summary>
    public static JsonElement? FindArray(JsonElement root, params string[] candidates)
    {
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind != JsonValueKind.Object) return null;

        foreach (var name in candidates)
        {
            if (root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array)
                return v;
        }
        // Fallback: first array-valued property on the root.
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
                return prop.Value;
        }
        return null;
    }
}
