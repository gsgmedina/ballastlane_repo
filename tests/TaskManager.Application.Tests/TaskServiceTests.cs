using FluentAssertions;
using FluentValidation;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Application.Tests.TestSupport;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Application.Tests;

public class TaskServiceTests
{
    private readonly ITaskRepository _repo = Substitute.For<ITaskRepository>();
    private readonly FakeClock _clock = FakeClock.Default;
    private readonly TaskService _sut;
    private readonly Guid _owner = Guid.NewGuid();

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _repo,
            _clock,
            new CreateTaskRequestValidator(_clock),
            new UpdateTaskRequestValidator(_clock));
    }

    [Fact]
    public async Task GetTasks_ReturnsMappedTasksForOwner()
    {
        var t = TaskItem.Create(_owner, "A", "d", null, _clock.UtcNow);
        _repo.GetByOwnerAsync(_owner, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { t });

        var result = await _sut.GetTasksAsync(_owner);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("A");
    }

    [Fact]
    public async Task CreateTask_WithValidData_PersistsAndDefaultsToTodo()
    {
        var req = new CreateTaskRequest("Buy milk", "2 liters", _clock.UtcNow.AddDays(1), null);

        var result = await _sut.CreateTaskAsync(_owner, req);

        result.Title.Should().Be("Buy milk");
        result.Status.Should().Be(TaskItemStatus.Todo);
        await _repo.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.OwnerUserId == _owner && t.Title == "Buy milk"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTask_WithPastDueDate_ThrowsValidation()
    {
        var req = new CreateTaskRequest("x", null, _clock.UtcNow.AddDays(-1), null);

        var act = () => _sut.CreateTaskAsync(_owner, req);

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_ThrowsValidation()
    {
        var req = new CreateTaskRequest("   ", null, null, null);
        var act = () => _sut.CreateTaskAsync(_owner, req);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetTask_WhenMissing_ThrowsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TaskItem?)null);
        var act = () => _sut.GetTaskAsync(Guid.NewGuid(), _owner);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTask_WhenOwnedByAnotherUser_ThrowsNotFound()
    {
        var someoneElse = TaskItem.Create(Guid.NewGuid(), "secret", null, null, _clock.UtcNow);
        _repo.GetByIdAsync(someoneElse.Id, Arg.Any<CancellationToken>()).Returns(someoneElse);

        var act = () => _sut.GetTaskAsync(someoneElse.Id, _owner);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateTask_WhenOwned_UpdatesAndPersists()
    {
        var task = TaskItem.Create(_owner, "old", null, null, _clock.UtcNow);
        _repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var req = new UpdateTaskRequest("new", "desc", _clock.UtcNow.AddDays(2), TaskItemStatus.InProgress);

        var result = await _sut.UpdateTaskAsync(task.Id, _owner, req);

        result.Title.Should().Be("new");
        result.Status.Should().Be(TaskItemStatus.InProgress);
        await _repo.Received(1).UpdateAsync(task, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTask_WhenNotOwned_ThrowsNotFoundAndDoesNotPersist()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "old", null, null, _clock.UtcNow);
        _repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        var req = new UpdateTaskRequest("new", null, null, TaskItemStatus.Done);

        var act = () => _sut.UpdateTaskAsync(task.Id, _owner, req);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTask_WhenOwned_Deletes()
    {
        var task = TaskItem.Create(_owner, "x", null, null, _clock.UtcNow);
        _repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        await _sut.DeleteTaskAsync(task.Id, _owner);

        await _repo.Received(1).DeleteAsync(task.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTask_WhenNotOwned_ThrowsAndDoesNotDelete()
    {
        var task = TaskItem.Create(Guid.NewGuid(), "x", null, null, _clock.UtcNow);
        _repo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => _sut.DeleteTaskAsync(task.Id, _owner);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
