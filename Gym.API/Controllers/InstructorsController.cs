using Gym.API.Contracts;
using Gym.Core.DTOs;
using Gym.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gym.API.Controllers;

[Authorize]
public sealed class InstructorsController : ApiControllerBase
{
    private readonly IInstructorService _service;

    public InstructorsController(IInstructorService service) => _service = service;

    /// <summary>
    /// Active instructors. Admins can pass <c>includeInactive=true</c> to also see deactivated ones.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InstructorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstructorResponse>>> List(
        [FromQuery] bool includeInactive, CancellationToken ct) =>
        Ok(await _service.ListAsync(includeInactive && User.IsInRole("Admin"), ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstructorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInstructorRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToResponse(result, value => Created(string.Empty, value));
    }

    /// <summary>
    /// Deactivates an instructor: they leave the roster and no new sessions can be scheduled with
    /// them. Past sessions are kept. Refused while the instructor still has upcoming sessions that
    /// were not cancelled.
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.DeactivateAsync(id, ct));

    /// <summary>Reactivates a previously deactivated instructor.</summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.ActivateAsync(id, ct));
}
