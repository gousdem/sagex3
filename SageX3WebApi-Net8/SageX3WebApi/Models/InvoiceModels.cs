namespace SageX3WebApi.Models;

// ==========================================================================
// GET INVOICE OK2PAY
// ==========================================================================

public class GetInvoiceOk2PayRequest
{
    public string? Company { get; set; }
    public string? Site { get; set; }
    public string? SupplierCode { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public string? Currency { get; set; }
}

public class InvoiceOk2Pay
{
    public string? InvoiceNumber { get; set; }
    public string? Company { get; set; }
    public string? Site { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal AmountGross { get; set; }
    public decimal AmountOpen { get; set; }
    public string? Currency { get; set; }
    public string? PaymentTerm { get; set; }
    public string? Ok2PayFlag { get; set; }
    public string? Reference { get; set; }
}

public class GetInvoiceOk2PayResponse
{
    public int Count { get; set; }
    public List<InvoiceOk2Pay> Invoices { get; set; } = new();
}

// ==========================================================================
// INSERT INVOICE PAYMENT
// ==========================================================================

public class InsertInvoicePaymentRequest
{
    public string? Company { get; set; }
    public string? Site { get; set; }
    public string? PaymentEntryType { get; set; }
    public string? BankCode { get; set; }
    public DateTime PaymentDate { get; set; }
    public required string SupplierCode { get; set; }
    public string? Currency { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public List<InvoicePaymentLine> Lines { get; set; } = new();
}

public class InvoicePaymentLine
{
    public required string InvoiceNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal? DiscountTaken { get; set; }
}

public class InsertInvoicePaymentResponse
{
    public string? PaymentNumber { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
}
