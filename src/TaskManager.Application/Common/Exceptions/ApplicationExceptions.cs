namespace TaskManager.Application.Common.Exceptions;

/// <summary>A requested resource does not exist (or is not visible to the caller).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string resource, Guid id)
        => new($"{resource} with id '{id}' was not found.");
}

/// <summary>A request conflicts with existing state (e.g. duplicate email).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Login failed because the credentials were invalid.</summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid email or password.") { }
}
