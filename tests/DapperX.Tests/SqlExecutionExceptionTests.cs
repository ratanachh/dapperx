using DapperX.Abstractions.Exceptions;
using DapperX.Runtime.Execution;

namespace DapperX.Tests;

public class SqlExecutionExceptionTests
{
    [Fact]
    public void SqlExecutionException_exposes_sql_property()
    {
        var inner = new InvalidOperationException("provider");
        var ex = new SqlExecutionException("failed", "SELECT 1", inner);

        Assert.Equal("SELECT 1", ex.Sql);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void ConvertToProperty_wraps_converter_failure()
    {
        var ex = Assert.Throws<SqlExecutionException>(() =>
            DbExecutor.ConvertToProperty<int, int>(_ => throw new FormatException("bad"), 1, "Amount"));

        Assert.Contains("Amount", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
    }

    [Fact]
    public void ConvertToColumn_wraps_converter_failure()
    {
        var ex = Assert.Throws<SqlExecutionException>(() =>
            DbExecutor.ConvertToColumn<int, string>(_ => throw new FormatException("bad"), 1, "Status"));

        Assert.Contains("Status", ex.Message);
        Assert.IsType<FormatException>(ex.InnerException);
    }
}
