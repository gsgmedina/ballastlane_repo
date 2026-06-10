using TaskManager.Application.Abstractions;

namespace TaskManager.Application.Tests.TestSupport;

/// <summary>Deterministic clock for tests.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }

    public static FakeClock Default => new(new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc));
}
