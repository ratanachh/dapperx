namespace Dapper.Npa.Generator.Utils;

/// <summary>Appends EPIC 25 logging context to generated <see cref="Dapper.Npa.Runtime.Execution.DbExecutor"/> calls.</summary>
internal static class DbExecutorEmission
{
    public const string LogContextSuffix =
        ", logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider)";

    public static string LogContextSuffixIf(bool emitLogContext)
        => emitLogContext ? LogContextSuffix : "";
}
