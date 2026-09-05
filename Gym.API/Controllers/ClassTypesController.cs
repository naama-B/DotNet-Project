using Gym.API.Contracts;
using Gym.Core.DTOs;
using Gym.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gym.API.Controllers;

[Authorize]
public sealed class ClassTypesController : ApiControllerBase
{
    private readonly IClassTypeService _service;

    public ClassTypesController(IClassTypeService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClassTypeResponse>>> List(CancellationToken ct) =>
        Ok(await _service.ListAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ClassTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.GetAsync(id, ct), Ok);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClassTypeRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return ToResponse(result, value => CreatedAtAction(nameof(Get), new { id = value.Id }, value));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClassTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateClassTypeRequest request, CancellationToken ct) =>
        ToResponse(await _service.UpdateAsync(id, request, ct), Ok);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.DeleteAsync(id, ct));
}
