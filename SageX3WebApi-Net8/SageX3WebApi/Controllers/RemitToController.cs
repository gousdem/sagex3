using Microsoft.AspNetCore.Mvc;
using SageX3WebApi.Models;
using SageX3WebApi.Services;

namespace SageX3WebApi.Controllers;

[ApiController]
[Route("api/remitto")]
[Produces("application/json")]
public class RemitToController : ControllerBase
{
    private readonly IRemitToService _service;

    public RemitToController(IRemitToService service) => _service = service;

    /// <summary>Insert a remit-to (pay-to) address for a supplier.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InsertRemitToAddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InsertRemitToAddressResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Insert(
        [FromBody] InsertRemitToAddressRequest request,
        CancellationToken ct)
    {
        var response = await _service.InsertRemitToAddressAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
