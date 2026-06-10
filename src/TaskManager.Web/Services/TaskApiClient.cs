using System.Net;
using System.Net.Http.Json;
using TaskManager.Web.Models;

namespace TaskManager.Web.Services;

/// <summary>Typed client for the Tasks CRUD API.</summary>
public sealed class TaskApiClient
{
    private readonly HttpClient _http;

    public TaskApiClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<TaskResponse>> GetTasksAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/tasks", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<List<TaskResponse>>(cancellationToken: ct)
               ?? new List<TaskResponse>();
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", request, ct);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(cancellationToken: ct))!;
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/tasks/{id}", request, ct);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/tasks/{id}", ct);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        ProblemResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        }
        catch
        {
            // Body was not problem+json; fall back to a generic message below.
        }

        throw ApiException.From(response.StatusCode, problem);
    }
}
