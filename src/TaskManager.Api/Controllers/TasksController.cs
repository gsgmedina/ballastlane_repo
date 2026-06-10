using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Extensions;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Api.Controllers;

/// <summary>CRUD API for tasks. Every endpoint requires authentication and is scoped to the caller.</summary>
[ApiController]
[Authorize]
[Route("api/tasks")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    /// <summary>Lists all tasks owned by the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken ct)
        => Ok(await _tasks.GetTasksAsync(User.GetUserId(), ct));

    /// <summary>Gets a single task by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _tasks.GetTaskAsync(id, User.GetUserId(), ct));

    /// <summary>Creates a new task.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var created = await _tasks.CreateTaskAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing task.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken ct)
        => Ok(await _tasks.UpdateTaskAsync(id, User.GetUserId(), request, ct));

    /// <summary>Deletes a task.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _tasks.DeleteTaskAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
