using DapperX.Abstractions.Configuration;
using DapperX.Core.Enums;

namespace DapperX.Runtime.Execution;

using DapperX.Abstractions.Configuration;
using DapperX.Core.Enums;

/// <summary>Optional logging context passed into <see cref="DbExecutor"/> Dapper entry points.</summary>
public readonly struct DbExecutionLogContext
{
    public string? MethodName { get; init; }
    public IDapperXOptions? Options { get; init; }
    public DatabaseProvider Provider { get; init; }

    public static DbExecutionLogContext Create(
        string methodName,
        IDapperXOptions? options,
        DatabaseProvider provider)
        => new() { MethodName = methodName, Options = options, Provider = provider };
}
