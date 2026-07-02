namespace Dapper.Npa.Abstractions.Exceptions;

public sealed class ConcurrencyException : Exception
{
    public IReadOnlyList<object> ConflictingKeys { get; }

    public ConcurrencyException(string message, IEnumerable<object>? conflictingKeys = null)
        : base(message)
    {
        ConflictingKeys = conflictingKeys?.ToList() ?? [];
    }
}
