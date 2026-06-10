using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Api.Tests;

public class TasksEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TasksEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetTasks_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        (await client.GetAsync("/api/tasks")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullCrudRoundTrip_Succeeds()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync();
        var due = DateTime.UtcNow.Date.AddDays(5);

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Write tests", "cover the API", due, null), ApiTestExtensions.Json);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.ReadAsync<TaskResponse>())!;
        created.Title.Should().Be("Write tests");
        created.Status.Should().Be(TaskItemStatus.Todo);

        // Read (by id)
        var getResponse = await client.GetAsync($"/api/tasks/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await getResponse.ReadAsync<TaskResponse>())!.Id.Should().Be(created.Id);

        // Read (list)
        var list = await (await client.GetAsync("/api/tasks")).ReadAsync<List<TaskResponse>>();
        list!.Should().Contain(t => t.Id == created.Id);

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Write more tests", "edited", due, TaskItemStatus.InProgress),
            ApiTestExtensions.Json);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.ReadAsync<TaskResponse>())!;
        updated.Title.Should().Be("Write more tests");
        updated.Status.Should().Be(TaskItemStatus.InProgress);

        // Delete
        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        // Gone
        (await client.GetAsync($"/api/tasks/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("", null, null, null), ApiTestExtensions.Json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithPastDueDate_Returns400()
    {
        var (client, _) = await _factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Past", null, DateTime.UtcNow.Date.AddDays(-2), null), ApiTestExtensions.Json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UserCannotAccessAnotherUsersTask_Returns404()
    {
        var (alice, _) = await _factory.CreateAuthenticatedClientAsync();
        var (bob, _) = await _factory.CreateAuthenticatedClientAsync();

        var created = (await (await alice.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Alice secret", null, null, null), ApiTestExtensions.Json))
            .ReadAsync<TaskResponse>())!;

        // Bob cannot see Alice's task.
        (await bob.GetAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        // ...nor delete it.
        (await bob.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Alice still can.
        (await alice.GetAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
