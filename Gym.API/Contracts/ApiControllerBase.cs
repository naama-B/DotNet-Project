using System.Security.Claims;
using Gym.API.Middleware;
using Gym.Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace Gym.API.Contracts;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The authenticated member's id, taken from the JWT.</summary>
    protected int CurrentMemberId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Authenticated request has no member id claim."));

    private string? CorrelationId =>
        HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id) ? id?.ToString() : null;

    /// <summary>Maps a non-generic <see cref="Result"/> to an HTTP response.</summary>
    protected IActionResult ToResponse(Result result, Func<IActionResult>? onSuccess = null) =>
        result.IsSuccess ? onSuccess?.Invoke() ?? NoContent() : Problem(result);

    /// <summary>Maps a <see cref="Result{T}"/> to an HTTP response.</summary>
    protected IActionResult ToResponse<T>(Result<T> result, Func<T, IActionResult>? onSuccess = null) =>
        result.IsSuccess ? onSuccess?.Invoke(result.Value!) ?? Ok(result.Value) : Problem(result);

    private ObjectResult Problem(Result result)
    {
        var status = result.Status switch
        {
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.Validation => StatusCodes.Status400BadRequest,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var body = new ApiError
        {
            Status = status,
            Title = result.Error ?? "Request failed.",
            CorrelationId = CorrelationId
        };

        return new ObjectResult(body) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
