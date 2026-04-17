namespace SageX3WebApi.Models;

/// <summary>
/// Generic lookup request. "LookupName" is the Sage X3 miscellaneous table / local-menu code.
/// Examples: "CUR" (currencies), "CNTRY" (countries), "PTE" (payment terms), "BAN" (banks).
/// </summary>
public class GetLookupValuesRequest
{
    public required string LookupName { get; set; }
    public string LanguageCode { get; set; } = "ENG";
    public string? Filter { get; set; }
    public int MaxResults { get; set; } = 500;
}

public class LookupValue
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public bool Active { get; set; } = true;
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class GetLookupValuesResponse
{
    public string? LookupName { get; set; }
    public int Count { get; set; }
    public List<LookupValue> Values { get; set; } = new();
}
