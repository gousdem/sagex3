using System.ComponentModel.DataAnnotations;

namespace SageX3WebApi.SageX3SoapClient;

/// <summary>
/// Strongly-typed options bound from the "SageX3" configuration section.
/// </summary>
public class SageX3Options
{
    public const string SectionName = "SageX3";

    [Required]
    public string EndpointUrl { get; set; } = string.Empty;

    public string? WsdlUrl { get; set; }

    [Required]
    public string PoolAlias { get; set; } = "SEED";

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string Language { get; set; } = "ENG";

    public string RequestConfig { get; set; } = "adxwss.optreturn=JSON&adxwss.beautify=true";

    [Range(5, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Map of operation name to Sage X3 ADX sub-program code.
    /// Leave a value empty to fall back to standard business-object operations.
    /// </summary>
    public SubProgramOptions SubPrograms { get; set; } = new();
}

public class SubProgramOptions
{
    public string? GetInvoiceOk2Pay { get; set; }
    public string? InsertInvoicePayment { get; set; }
    public string? GetLookupValues { get; set; }
    public string? InsertRemitToAddress { get; set; }
    public string? GetSupplierInformationSIM { get; set; }
    public string? GetSupplier { get; set; }
    public string? InsertSupplier { get; set; }
}
