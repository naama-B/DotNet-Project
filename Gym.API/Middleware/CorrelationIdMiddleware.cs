namespace Gym.API.Middleware;

/// <summary>
/// Gives every request a correlation id (from the <c>X-Correlation-ID</c> header if the caller
/// sent one, otherwise a fresh GUID), echoes it on the response, and opens a logging scope so
/// every log line produced while handling the request carries <c>CorrelationId</c>.
/// Registered first-ish in the pipeline so even the error handler's logs are correlated.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var provided)
                            && !string.IsNullOrWhiteSpace(provided)
            ? provided.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            _logger.LogInformation("Incoming request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await _next(context);

            _logger.LogInformation("Request {Method} {Path} completed with {StatusCode}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode);
        }
    }
}
