using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Common;

namespace TaskManager.Api.Middleware;

/// <summary>
/// Translates domain/application exceptions into RFC 7807 ProblemDetails responses,
/// so controllers can stay focused on the happy path.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await WriteResponseAsync(context, ex);
        }
    }

    private async Task WriteResponseAsync(HttpContext context, Exception ex)
    {
        var (status, title, errors) = Map(ex);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
        else
            _logger.LogInformation("Request to {Path} rejected: {Message}", context.Request.Path, ex.Message);

        // Never echo raw internal exception text on a 500 — only safe, mapped messages.
        var detail = status >= StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred. Please try again later."
            : ex.Message;

        ProblemDetails problem = errors is null
            ? new ProblemDetails { Status = status, Title = title, Detail = detail }
            : new ValidationProblemDetails(errors) { Status = status, Title = title };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        // Serialize by runtime type so ValidationProblemDetails.Errors is included.
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, problem.GetType(), JsonOptions));
    }

    private static (int status, string title, IDictionary<string, string[]>? errors) Map(Exception ex) => ex switch
    {
        ValidationException v => (
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.",
            v.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
        DomainException => (StatusCodes.Status400BadRequest, "Invalid request.", null),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", null),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict.", null),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Authentication failed.", null),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
