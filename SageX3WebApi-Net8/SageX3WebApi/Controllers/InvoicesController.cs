using Microsoft.AspNetCore.Mvc;
using SageX3WebApi.Models;
using SageX3WebApi.Services;

namespace SageX3WebApi.Controllers;

[ApiController]
[Route("api/invoices")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service) => _service = service;

    /// <summary>Get invoices flagged as OK-To-Pay in Sage X3.</summary>
    [HttpPost("ok2pay")]
    [ProducesResponseType(typeof(ApiResponse<GetInvoiceOk2PayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetInvoiceOk2PayResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOk2Pay(
        [FromBody] GetInvoiceOk2PayRequest request,
        CancellationToken ct)
    {
        var response = await _service.GetInvoiceOk2PayAsync(request ?? new GetInvoiceOk2PayRequest(), ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Convenience GET with query-string filters.</summary>
    [HttpGet("ok2pay")]
    [ProducesResponseType(typeof(ApiResponse<GetInvoiceOk2PayResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOk2PayByQuery(
        [FromQuery] string? company = null,
        [FromQuery] string? site = null,
        [FromQuery] string? supplierCode = null,
        [FromQuery] string? invoiceNumber = null,
        [FromQuery] string? currency = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        CancellationToken ct = default)
    {
        var request = new GetInvoiceOk2PayRequest
        {
            Company = company, Site = site, SupplierCode = supplierCode,
            InvoiceNumber = invoiceNumber, Currency = currency,
            DueDateFrom = dueDateFrom, DueDateTo = dueDateTo
        };
        var response = await _service.GetInvoiceOk2PayAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Insert an invoice payment (creates a PAYO record in Sage X3).</summary>
    [HttpPost("payments")]
    [ProducesResponseType(typeof(ApiResponse<InsertInvoicePaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InsertInvoicePaymentResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertPayment(
        [FromBody] InsertInvoicePaymentRequest request,
        CancellationToken ct)
    {
        var response = await _service.InsertInvoicePaymentAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
