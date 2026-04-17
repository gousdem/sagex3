using Microsoft.AspNetCore.Mvc;
using SageX3WebApi.Models;
using SageX3WebApi.Services;

namespace SageX3WebApi.Controllers;

[ApiController]
[Route("api/suppliers")]
[Produces("application/json")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;

    public SuppliersController(ISupplierService service) => _service = service;

    /// <summary>Get a supplier by code.</summary>
    [HttpGet("{supplierCode}")]
    [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<Supplier>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(string supplierCode, CancellationToken ct)
    {
        var response = await _service.GetSupplierAsync(
            new GetSupplierRequest { SupplierCode = supplierCode }, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Get the Supplier Information Management (SIM) consolidated view for a supplier.</summary>
    [HttpGet("{supplierCode}/sim")]
    [ProducesResponseType(typeof(ApiResponse<SupplierInformationSim>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SupplierInformationSim>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSim(
        string supplierCode,
        [FromQuery] bool includeInvoices = false,
        [FromQuery] bool includePayments = false,
        [FromQuery] bool includeContacts = true,
        [FromQuery] bool includeBankDetails = true,
        CancellationToken ct = default)
    {
        var request = new GetSupplierInformationSimRequest
        {
            SupplierCode = supplierCode,
            IncludeInvoices = includeInvoices,
            IncludePayments = includePayments,
            IncludeContacts = includeContacts,
            IncludeBankDetails = includeBankDetails
        };
        var response = await _service.GetSupplierInformationSimAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Create a new supplier in Sage X3.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InsertSupplierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InsertSupplierResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Insert(
        [FromBody] InsertSupplierRequest request,
        CancellationToken ct)
    {
        var response = await _service.InsertSupplierAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
