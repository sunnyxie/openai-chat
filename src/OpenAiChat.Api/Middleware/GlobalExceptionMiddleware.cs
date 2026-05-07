using System.Net;
using System.Text.Json;
using OpenAiChat.Api.Models;
using OpenAiChat.Api.Services;

namespace OpenAiChat.Api.Middleware;

/// <summary>
/// Pipeline middleware that catches unhandled exceptions and converts them
/// into a consistent JSON error response so the client always receives a
/// structured payload rather than a raw stack trace.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, error, detail) = exception switch
        {
            ChatServiceException cse => (
                HttpStatusCode.BadGateway,
                "OpenAI request failed.",
                cse.Message),

            OperationCanceledException => (
                HttpStatusCode.RequestTimeout,
                "The request timed out.",
                "The request was cancelled or timed out before a response was received."),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                null as string)
        };

        // Only log the full exception for unexpected (500-level) failures
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "{Error} for {Method} {Path}",
                error, context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var payload = new ErrorResponse { Error = error, Detail = detail };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}

/// <summary>Extension to register the middleware cleanly in Program.cs.</summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
