namespace DapperX.Abstractions.Sequences;

/// <summary>
/// Optional injection for sequence ID allocation.
/// DapperX emits the call; developer provides the stateful implementation.
/// This keeps DapperX stateless.
/// </summary>
public interface ISequenceAllocator
{
    Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default);
}
