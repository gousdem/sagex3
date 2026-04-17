namespace SageX3WebApi.Models;

/// <summary>
/// Request to insert a remit-to (pay-to) address for an existing supplier.
/// In Sage X3 this is the BPA (Business Partner Address) flagged as BPAPAY.
/// </summary>
public class InsertRemitToAddressRequest
{
    public required string SupplierCode { get; set; }
    public required string AddressCode { get; set; }
    public string? Description { get; set; }

    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Line3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }

    public bool IsDefaultPayAddress { get; set; }

    // Bank routing info - optional; creates a linked BID record.
    public string? BankIdCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? RoutingNumber { get; set; }
    public string? Iban { get; set; }
    public string? Swift { get; set; }
    public string? Currency { get; set; }
}

public class InsertRemitToAddressResponse
{
    public string? SupplierCode { get; set; }
    public string? AddressCode { get; set; }
    public string? BankIdCode { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}
