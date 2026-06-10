using FluentAssertions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.Domain.Tests;

public class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsDefaultsAndTimestamps()
    {
        var task = TaskItem.Create(Owner, "  Write report  ", "  details  ", Now.AddDays(1), Now);

        task.Id.Should().NotBe(Guid.Empty);
        task.OwnerUserId.Should().Be(Owner);
        task.Title.Should().Be("Write report"); // trimmed
        task.Description.Should().Be("details"); // trimmed
        task.Status.Should().Be(TaskItemStatus.Todo);
        task.CreatedAtUtc.Should().Be(Now);
        task.UpdatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTitle_Throws(string? title)
    {
        var act = () => TaskItem.Create(Owner, title!, null, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*Title is required*");
    }

    [Fact]
    public void Create_WithTitleTooLong_Throws()
    {
        var title = new string('x', TaskItem.MaxTitleLength + 1);
        var act = () => TaskItem.Create(Owner, title, null, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*200 characters*");
    }

    [Fact]
    public void Create_WithEmptyOwner_Throws()
    {
        var act = () => TaskItem.Create(Guid.Empty, "title", null, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*owner*");
    }

    [Fact]
    public void Create_WithEmptyDescription_NormalizesToNull()
    {
        var task = TaskItem.Create(Owner, "title", "   ", null, Now);
        task.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ChangesFieldsAndBumpsUpdatedAt()
    {
        var task = TaskItem.Create(Owner, "title", null, null, Now);
        var later = Now.AddHours(2);

        task.UpdateDetails("new title", "new desc", later.AddDays(3), later);

        task.Title.Should().Be("new title");
        task.Description.Should().Be("new desc");
        task.DueDateUtc.Should().Be(later.AddDays(3));
        task.UpdatedAtUtc.Should().Be(later);
        task.CreatedAtUtc.Should().Be(Now); // unchanged
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var task = TaskItem.Create(Owner, "title", null, null, Now);
        var later = Now.AddHours(1);

        task.ChangeStatus(TaskItemStatus.Done, later);

        task.Status.Should().Be(TaskItemStatus.Done);
        task.UpdatedAtUtc.Should().Be(later);
    }
}
