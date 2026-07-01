namespace DapperX.Abstractions.Exceptions;

public sealed class SqlExecutionException : Exception
{
    public string Sql { get; }

    public SqlExecutionException(string message, string sql, Exception innerException)
        : base(message, innerException)
    {
        Sql = sql;
    }
}
