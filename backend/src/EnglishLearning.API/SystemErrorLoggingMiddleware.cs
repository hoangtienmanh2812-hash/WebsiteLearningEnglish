using EnglishLearning.Domain.Entities;
using EnglishLearning.Infrastructure.Data;

namespace EnglishLearning.API;

public sealed class SystemErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemErrorLoggingMiddleware> _logger;

    public SystemErrorLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<SystemErrorLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            if (context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                await PersistErrorLogAsync(
                    context,
                    $"HTTP {context.Response.StatusCode} response",
                    "HttpError");
            }
        }
        catch (Exception exception)
        {
            await PersistErrorLogAsync(context, exception.GetType().Name, "UnhandledException");
            throw;
        }
    }

    private async Task PersistErrorLogAsync(HttpContext context, string detail, string category)
    {
        var endpoint = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.RouteEndpoint>();
        var route = endpoint?.RoutePattern.RawText ?? "unmatched-route";
        _logger.LogError(
            "Request error. Method={Method}, Route={Route}, Detail={Detail}.",
            context.Request.Method,
            route,
            detail);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            database.SystemLogs.Add(new SystemLog
            {
                OccurredAt = DateTime.UtcNow,
                Level = "Error",
                Category = category,
                Message = $"{context.Request.Method} {route}: {detail}"
            });
            await database.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception logException)
        {
            _logger.LogError(
                "Could not persist system error log ({ExceptionType}).",
                logException.GetType().Name);
        }
    }
}
