using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
