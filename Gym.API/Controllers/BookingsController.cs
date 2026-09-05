using Gym.API.Contracts;
using Gym.Core.DTOs;
using Gym.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gym.API.Controllers;

[Authorize]
public sealed class BookingsController : ApiControllerBase
{
    private readonly IBookingService _service;

    public BookingsController(IBookingService service) => _service = service;

    /// <summary>The current member's bookings, newest first.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> Mine(CancellationToken ct) =>
        Ok(await _service.ListForMemberAsync(CurrentMemberId, ct));

    /// <summary>
    /// Books the current member into a session. Returns 409 if the class filled up in the
    /// instant between reading availability and saving (optimistic-concurrency conflict) or if
    /// the member already holds a booking.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var result = await _service.BookAsync(CurrentMemberId, request, ct);
        return ToResponse(result, value => Created(string.Empty, value));
    }

    /// <summary>Cancels one of the current member's bookings.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel([FromRoute] int id, CancellationToken ct) =>
        ToResponse(await _service.CancelAsync(CurrentMemberId, id, ct));
}
