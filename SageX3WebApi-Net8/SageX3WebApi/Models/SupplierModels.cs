namespace SageX3WebApi.Models;

// ==========================================================================
// GET SUPPLIER
// ==========================================================================

public class GetSupplierRequest
{
    public required string SupplierCode { get; set; }
}

public class Supplier
{
    public string? SupplierCode { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Category { get; set; }
    public string? Currency { get; set; }
    public string? Language { get; set; }
    public string? TaxId { get; set; }
    public string? PaymentTerm { get; set; }
    public string? AccountingCode { get; set; }
    public bool Active { get; set; }
    public SupplierAddress? DefaultAddress { get; set; }
    public List<SupplierAddress> Addresses { get; set; } = new();
    public List<SupplierContact> Contacts { get; set; } = new();
    public List<SupplierBankId> BankIds { get; set; } = new();
}

public class SupplierAddress
{
    public string? AddressCode { get; set; }
    public string? Description { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Line3 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsDefault { get; set; }
}

public class SupplierContact
{
    public string? ContactCode { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class SupplierBankId
{
    public string? BankIdCode { get; set; }
    public string? AccountNumber { get; set; }
    public string? RoutingNumber { get; set; }
    public string? Iban { get; set; }
    public string? Swift { get; set; }
    public string? Currency { get; set; }
    public bool IsDefault { get; set; }
}

// ==========================================================================
// GET SUPPLIER INFORMATION (SIM)
// ==========================================================================

public class GetSupplierInformationSimRequest
{
    public required string SupplierCode { get; set; }
    public bool IncludeInvoices { get; set; }
    public bool IncludePayments { get; set; }
    public bool IncludeContacts { get; set; } = true;
    public bool IncludeBankDetails { get; set; } = true;
}

public class SupplierInformationSim
{
    public Supplier? Supplier { get; set; }
    public SupplierStatistics? Statistics { get; set; }
    public List<SupplierComplianceItem> Compliance { get; set; } = new();
    public List<SupplierCertification> Certifications { get; set; } = new();
}

public class SupplierStatistics
{
    public decimal YearToDatePurchases { get; set; }
    public decimal LastYearPurchases { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int OpenInvoiceCount { get; set; }
    public DateTime? LastInvoiceDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
}

public class SupplierComplianceItem
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class SupplierCertification
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

// ==========================================================================
// INSERT SUPPLIER
// ==========================================================================

public class InsertSupplierRequest
{
    public string? SupplierCode { get; set; }
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public string? Category { get; set; }
    public string? Currency { get; set; }
    public string? Language { get; set; } = "ENG";
    public string? TaxId { get; set; }
    public string? PaymentTerm { get; set; }
    public string? AccountingCode { get; set; }
    public SupplierAddress? Address { get; set; }
    public List<SupplierContact> Contacts { get; set; } = new();
    public List<SupplierBankId> BankIds { get; set; } = new();
}

public class InsertSupplierResponse
{
    public string? SupplierCode { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}
