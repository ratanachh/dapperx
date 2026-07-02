namespace Dapper.Npa.Abstractions.Sequences;

/// <summary>
/// Optional injection for sequence ID allocation.
/// Dapper Npaemits the call; developer provides the stateful implementation.
/// This keeps Dapper Npastateless.
/// </summary>
public interface ISequenceAllocator
{
    Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default);
}
