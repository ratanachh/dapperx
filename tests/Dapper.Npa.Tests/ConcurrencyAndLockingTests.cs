using Dapper.Npa.Core.Enums;
using Dapper.Npa.Query.Query;
using Dapper.Npa.Runtime.Query;

namespace Dapper.Npa.Tests;

public class ConcurrencyAndLockingTests
{
    [Fact]
    public void WithLock_rejects_negative_timeout()
    {
        var builder = new QueryBuilder<ProductStub>();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithLock(LockMode.Pessimistic, -1));
    }

    [Fact]
    public void QueryExecutor_emits_sqlserver_lock_timeout_preamble()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.Pessimistic, 5000)
            .Build();

        var (preamble, sql, parameters) = BuildSelectSql(state, "SqlServer");

        Assert.Equal("SET LOCK_TIMEOUT 5000", preamble);
        Assert.DoesNotContain("LOCK_TIMEOUT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WITH (UPDLOCK, ROWLOCK)", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_sqlserver_lock_hint_before_where()
    {
        var state = new QueryBuilder<ProductStub>()
            .Where(x => x.Id == 1)
            .WithLock(LockMode.Pessimistic)
            .Build();

        var (_, sql, _) = BuildSelectSql(state, "SqlServer");

        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        var updlockIndex = sql.IndexOf("UPDLOCK", StringComparison.OrdinalIgnoreCase);
        Assert.True(whereIndex >= 0);
        Assert.True(updlockIndex >= 0);
        Assert.True(updlockIndex < whereIndex);
        Assert.False(sql.TrimEnd().EndsWith("WITH (UPDLOCK, ROWLOCK)", StringComparison.Ordinal));
    }

    [Fact]
    public void QueryExecutor_emits_postgresql_nowait_when_timeout_zero()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.Pessimistic, 0)
            .Build();

        var (_, sql, parameters) = BuildSelectSql(state, "PostgreSql");

        Assert.Contains("FOR UPDATE NOWAIT", sql);
        Assert.DoesNotContain("SET lock_timeout", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_emits_postgresql_timeout_preamble_when_positive_timeout()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.Pessimistic, 1500)
            .Build();

        var (preamble, sql, parameters) = BuildSelectSql(state, "PostgreSql");

        Assert.Equal("SET lock_timeout = 1500", preamble);
        Assert.DoesNotContain("SET lock_timeout", sql);
        Assert.Contains("FOR UPDATE", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_treats_mysql_timeout_as_nowait_suffix()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.Pessimistic, 3000)
            .Build();

        var (_, sql, _) = BuildSelectSql(state, "MySql");

        Assert.Contains("FOR UPDATE NOWAIT", sql);
        Assert.DoesNotContain("SET lock_timeout", sql);
        Assert.DoesNotContain("LOCK_TIMEOUT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QueryExecutor_emits_sqlserver_pessimistic_read_holdlock()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.PessimisticRead)
            .Build();

        var (_, sql, parameters) = BuildSelectSql(state, "SqlServer");

        Assert.Contains("WITH (HOLDLOCK, ROWLOCK)", sql);
        Assert.DoesNotContain("UPDLOCK", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_emits_postgresql_for_share()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.PessimisticRead, 1500)
            .Build();

        var (preamble, sql, parameters) = BuildSelectSql(state, "PostgreSql");

        Assert.Equal("SET lock_timeout = 1500", preamble);
        Assert.DoesNotContain("SET lock_timeout", sql);
        Assert.Contains("FOR SHARE", sql);
        Assert.DoesNotContain("FOR UPDATE", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_emits_postgresql_for_share_nowait_when_timeout_zero()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.PessimisticRead, 0)
            .Build();

        var (_, sql, parameters) = BuildSelectSql(state, "PostgreSql");

        Assert.Contains("FOR SHARE NOWAIT", sql);
        Assert.DoesNotContain("SET lock_timeout", sql);
        Assert.DoesNotContain("lockTimeoutMs", parameters.Keys);
    }

    [Fact]
    public void QueryExecutor_emits_mysql_for_share_on_pessimistic_read()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.PessimisticRead)
            .Build();

        var (_, sql, _) = BuildSelectSql(state, "MySql");

        Assert.Contains("FOR SHARE", sql);
        Assert.DoesNotContain("FOR UPDATE", sql);
        Assert.DoesNotContain("LOCK IN SHARE MODE", sql);
    }

    [Fact]
    public void QueryExecutor_sqlite_pessimistic_read_throws_at_runtime()
    {
        var state = new QueryBuilder<ProductStub>()
            .WithLock(LockMode.PessimisticRead)
            .Build();

        Assert.Throws<NotSupportedException>(() => BuildSelectSql(state, "Sqlite"));
    }

    private sealed class ProductStub
    {
        public int Id { get; set; }
    }

    private static (string? LockPreambleSql, string Sql, Dictionary<string, object?> Parameters) BuildSelectSql(
        QueryBuilderState<ProductStub> state,
        string provider)
        => QueryExecutor.BuildSelectSql(
            "SELECT e.id FROM products e",
            QueryBuilderStateSnapshot.From(state),
            name => name,
            new QueryRuntimeConfig { Provider = provider });
}
