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
    private readonly IClassRatingService _ratings;

    public ClassSessionsController(IClassSessionService service, IClassRatingService ratings)
    {
        _service = service;
        _ratings = ratings;
    }

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

    /// <summary>
    /// The waiting list for a session, ordered by queue position. Admin only — it exposes the
    /// names of other members.
    /// </summary>
    [HttpGet("{id:int}/waitlist")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SessionWaitlistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Waitlist([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.GetWaitlistAsync(id, ct), Ok);

    /// <summary>
    /// Every session that members have rated — its title, instructor and the reviews themselves.
    /// Backs the standalone reviews page.
    /// </summary>
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewedSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewedSessionResponse>>> Reviews(CancellationToken ct) =>
        Ok(await _ratings.ListReviewedSessionsAsync(ct));

    /// <summary>Every satisfaction rating for a session, with the average star score.</summary>
    [HttpGet("{id:int}/ratings")]
    [ProducesResponseType(typeof(SessionRatingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ratings([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _ratings.ListForSessionAsync(id, ct), Ok);

    /// <summary>
    /// Leave a 1–5 star satisfaction rating for a session. Allowed only for a member who held a
    /// confirmed booking for a session that has already taken place; posting again updates it.
    /// </summary>
    [HttpPost("{id:int}/ratings")]
    [ProducesResponseType(typeof(ClassRatingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rate([FromRoute] int id, [FromBody] CreateClassRatingRequest request, CancellationToken ct) =>
        ToResponse(await _ratings.RateAsync(CurrentMemberId, id, request, ct), Ok);
}
