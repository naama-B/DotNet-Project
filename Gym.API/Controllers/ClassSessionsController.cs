using Gym.API.Contracts;
using Gym.Core.Common;
using Gym.Core.DTOs;
using Gym.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gym.API.Controllers;

[Authorize]
public sealed class ClassSessionsController : ApiControllerBase
{
    private readonly IClassSessionService _service;

    public ClassSessionsController(IClassSessionService service) => _service = service;

    /// <summary>Paged, filterable list of class sessions. Paging runs in the database.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClassSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClassSessionResponse>>> List(
        [FromQuery] ClassSessionQueryParameters query, CancellationToken ct) =>
        Ok(await _service.ListAsync(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClassSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.GetAsync(id, ct), Ok);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateClassSessionRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToResponse(result, value => CreatedAtAction(nameof(Get), new { id = value.Id }, value));
    }

    /// <summary>Cancels a session. Booked members keep a record; the session stops taking bookings.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.CancelAsync(id, ct));
}
