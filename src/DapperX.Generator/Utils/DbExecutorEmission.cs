namespace DapperX.Generator.Utils;

/// <summary>Appends EPIC 25 logging context to generated <see cref="DapperX.Runtime.Execution.DbExecutor"/> calls.</summary>
internal static class DbExecutorEmission
{
    public const string LogContextSuffix =
        ", logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider)";

    public static string LogContextSuffixIf(bool emitLogContext)
        => emitLogContext ? LogContextSuffix : "";
}
