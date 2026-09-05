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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InstructorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstructorResponse>>> List(CancellationToken ct) =>
        Ok(await _service.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(InstructorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInstructorRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToResponse(result, value => Created(string.Empty, value));
    }
}
