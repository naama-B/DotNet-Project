using System.Text.Json;
using Gym.API.Contracts;

namespace Gym.API.Middleware;

/// <summary>
/// Global catch-all. Any exception that escapes a controller is logged at Error and turned
/// into a uniform JSON <see cref="ApiError"/> body with status 500. Expected business failures
/// never reach here — services return <c>Result</c> and controllers map them to 4xx.
/// Registered first so it wraps the whole pipeline.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            // The client disconnected before the response was sent, or the request timed out.
            // Normal in development: React StrictMode fires every fetch twice and aborts the first.
            // Unconditional (no `when` filter) so the debugger sees a real handler and does not
            // break on it as "user-unhandled".
            if (context.RequestAborted.IsCancellationRequested)
                _logger.LogInformation("Request {Path} was cancelled by the client.", context.Request.Path);
            else
                _logger.LogInformation("Request {Path} was cancelled.", context.Request.Path);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var id)
                ? id?.ToString()
                : null;

            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started; cannot write error body.");
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var body = new ApiError
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = _env.IsDevelopment() ? ex.Message : null,
                CorrelationId = correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
