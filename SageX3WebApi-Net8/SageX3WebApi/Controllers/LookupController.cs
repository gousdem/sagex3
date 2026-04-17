using Microsoft.AspNetCore.Mvc;
using SageX3WebApi.Models;
using SageX3WebApi.Services;

namespace SageX3WebApi.Controllers;

[ApiController]
[Route("api/lookups")]
[Produces("application/json")]
public class LookupController : ControllerBase
{
    private readonly ILookupService _service;

    public LookupController(ILookupService service) => _service = service;

    /// <summary>
    /// Get lookup values for a miscellaneous table or local menu.
    /// Example: GET /api/lookups/CUR?languageCode=ENG
    /// </summary>
    [HttpGet("{lookupName}")]
    [ProducesResponseType(typeof(ApiResponse<GetLookupValuesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetLookupValuesResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(
        string lookupName,
        [FromQuery] string languageCode = "ENG",
        [FromQuery] string? filter = null,
        [FromQuery] int maxResults = 500,
        CancellationToken ct = default)
    {
        var request = new GetLookupValuesRequest
        {
            LookupName = lookupName,
            LanguageCode = languageCode,
            Filter = filter,
            MaxResults = maxResults
        };
        var response = await _service.GetLookupValuesAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>POST variant with a request body.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GetLookupValuesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetLookupValuesResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromBody] GetLookupValuesRequest request,
        CancellationToken ct)
    {
        var response = await _service.GetLookupValuesAsync(request, ct);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
