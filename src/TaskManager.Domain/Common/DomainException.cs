namespace TaskManager.Domain.Common;

/// <summary>
/// Raised when a domain invariant is violated (e.g. constructing an entity with invalid data).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
