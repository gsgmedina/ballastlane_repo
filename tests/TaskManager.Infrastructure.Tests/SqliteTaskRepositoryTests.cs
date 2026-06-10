using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class SqliteTaskRepositoryTests : SqliteTestBase
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    private async Task<(SqliteTaskRepository tasks, Guid ownerId)> SeedOwnerAsync()
    {
        var users = new SqliteUserRepository(Factory);
        var owner = User.Create("owner@example.com", "hash", "Owner", Now);
        await users.AddAsync(owner);
        return (new SqliteTaskRepository(Factory), owner.Id);
    }

    [Fact]
    public async Task Add_ThenGetById_RoundTripsAllFields()
    {
        var (tasks, ownerId) = await SeedOwnerAsync();
        var due = Now.AddDays(3);
        var task = TaskItem.Create(ownerId, "Title", "Description", due, Now, TaskItemStatus.InProgress);

        await tasks.AddAsync(task);
        var loaded = await tasks.GetByIdAsync(task.Id);

        loaded.Should().NotBeNull();
        loaded!.OwnerUserId.Should().Be(ownerId);
        loaded.Title.Should().Be("Title");
        loaded.Description.Should().Be("Description");
        loaded.Status.Should().Be(TaskItemStatus.InProgress);
        loaded.DueDateUtc.Should().Be(due);
        loaded.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Add_WithNullDescriptionAndDueDate_RoundTripsNulls()
    {
        var (tasks, ownerId) = await SeedOwnerAsync();
        var task = TaskItem.Create(ownerId, "No extras", null, null, Now);

        await tasks.AddAsync(task);
        var loaded = await tasks.GetByIdAsync(task.Id);

        loaded!.Description.Should().BeNull();
        loaded.DueDateUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetByOwner_ReturnsOnlyOwnersTasks()
    {
        var (tasks, ownerId) = await SeedOwnerAsync();
        var users = new SqliteUserRepository(Factory);
        var other = User.Create("other@example.com", "hash", "Other", Now);
        await users.AddAsync(other);

        await tasks.AddAsync(TaskItem.Create(ownerId, "Mine 1", null, null, Now));
        await tasks.AddAsync(TaskItem.Create(ownerId, "Mine 2", null, null, Now));
        await tasks.AddAsync(TaskItem.Create(other.Id, "Theirs", null, null, Now));

        var mine = await tasks.GetByOwnerAsync(ownerId);

        mine.Should().HaveCount(2);
        mine.Select(t => t.Title).Should().BeEquivalentTo("Mine 1", "Mine 2");
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var (tasks, ownerId) = await SeedOwnerAsync();
        var task = TaskItem.Create(ownerId, "Before", null, null, Now);
        await tasks.AddAsync(task);

        task.UpdateDetails("After", "now with description", Now.AddDays(5), Now.AddHours(1));
        task.ChangeStatus(TaskItemStatus.Done, Now.AddHours(1));
        await tasks.UpdateAsync(task);

        var loaded = await tasks.GetByIdAsync(task.Id);
        loaded!.Title.Should().Be("After");
        loaded.Description.Should().Be("now with description");
        loaded.Status.Should().Be(TaskItemStatus.Done);
        loaded.UpdatedAtUtc.Should().Be(Now.AddHours(1));
    }

    [Fact]
    public async Task Delete_RemovesTaskAndReportsResult()
    {
        var (tasks, ownerId) = await SeedOwnerAsync();
        var task = TaskItem.Create(ownerId, "Doomed", null, null, Now);
        await tasks.AddAsync(task);

        (await tasks.DeleteAsync(task.Id)).Should().BeTrue();
        (await tasks.GetByIdAsync(task.Id)).Should().BeNull();
        (await tasks.DeleteAsync(task.Id)).Should().BeFalse(); // already gone
    }
}
