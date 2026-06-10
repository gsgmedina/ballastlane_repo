using System.Net;
using TaskManager.Web.Models;

namespace TaskManager.Web.Services;

/// <summary>Represents a non-success API response, carrying a user-friendly message.</summary>
public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    public ApiException(HttpStatusCode statusCode, string message, IReadOnlyList<string>? validationErrors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    public static ApiException From(HttpStatusCode statusCode, ProblemResponse? problem)
    {
        var errors = problem?.Errors?.SelectMany(kvp => kvp.Value).ToArray() ?? Array.Empty<string>();
        var message = errors.Length > 0
            ? string.Join(" ", errors)
            : problem?.Detail ?? problem?.Title ?? $"Request failed ({(int)statusCode}).";
        return new ApiException(statusCode, message, errors);
    }
}
