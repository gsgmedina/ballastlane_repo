using FluentAssertions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.Domain.Tests;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NormalizesEmailToLowercaseAndTrims()
    {
        var user = User.Create("  Demo@Example.COM ", "hash", "  Demo User ", Now);

        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Should().Be("demo@example.com");
        user.DisplayName.Should().Be("Demo User");
        user.PasswordHash.Should().Be("hash");
        user.CreatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("", "hash", "name", "*Email*")]
    [InlineData("a@b.com", "", "name", "*Password*")]
    [InlineData("a@b.com", "hash", "", "*Display name*")]
    public void Create_WithMissingRequiredField_Throws(string email, string hash, string name, string expected)
    {
        var act = () => User.Create(email, hash, name, Now);
        act.Should().Throw<DomainException>().WithMessage(expected);
    }
}
