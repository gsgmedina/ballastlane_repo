using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class SqliteUserRepositoryTests : SqliteTestBase
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Add_ThenGetById_RoundTripsUser()
    {
        var repo = new SqliteUserRepository(Factory);
        var user = User.Create("user@example.com", "hash", "User One", Now);

        await repo.AddAsync(user);
        var loaded = await repo.GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Email.Should().Be("user@example.com");
        loaded.DisplayName.Should().Be("User One");
        loaded.PasswordHash.Should().Be("hash");
        loaded.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task GetByEmail_IsCaseInsensitive()
    {
        var repo = new SqliteUserRepository(Factory);
        await repo.AddAsync(User.Create("user@example.com", "hash", "User", Now));

        var loaded = await repo.GetByEmailAsync("USER@EXAMPLE.COM");

        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistsByEmail_ReflectsPresence()
    {
        var repo = new SqliteUserRepository(Factory);

        (await repo.ExistsByEmailAsync("nobody@example.com")).Should().BeFalse();

        await repo.AddAsync(User.Create("somebody@example.com", "hash", "User", Now));
        (await repo.ExistsByEmailAsync("somebody@example.com")).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNull()
    {
        var repo = new SqliteUserRepository(Factory);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }
}
