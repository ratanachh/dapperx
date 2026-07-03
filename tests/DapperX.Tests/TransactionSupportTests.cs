using System.Data;
using System.Reflection;
using DapperX.Abstractions.Repositories;
using DapperX.Core.Enums;
using DapperX.Runtime.Repositories;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class TransactionSupportTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_crud_methods_accept_optional_transaction()
    {
        var method = typeof(ProductRepositoryImpl).GetMethod(
            nameof(IRepository<Product, int>.GetByIdAsync),
            [typeof(int), typeof(bool), typeof(IDbTransaction), typeof(CancellationToken)]);
        Assert.NotNull(method);
        var transactionParam = method!.GetParameters().FirstOrDefault(p => p.Name == "transaction");
        Assert.NotNull(transactionParam);
        Assert.Equal(typeof(IDbTransaction), transactionParam!.ParameterType);
    }

    [Fact]
    public void GraphRepositoryImpl_insert_graph_owns_transaction_when_none_supplied()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);
        Assert.Contains("ownsTransaction = transaction is null", body);
        Assert.Contains("transaction ??= _connection.BeginTransaction()", body);
        Assert.Contains("if (ownsTransaction) transaction.Commit();", body);
        Assert.Contains("if (ownsTransaction) transaction.Rollback();", body);
    }

    [Fact]
    public void DocumentRepositoryImpl_secondary_table_insert_owns_transaction()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("ownsTransaction = transaction is null", source);
        Assert.Contains("if (ownsTransaction) transaction.Rollback();", source);
    }

    [Fact]
    public async Task WithTransactionAsync_commits_on_success()
    {
        var connection = new FakeDbConnection();
        var repo = new StubRepository(connection);
        var seen = false;

        await repo.WithTransactionAsync(tx =>
        {
            seen = tx is not null;
            return Task.CompletedTask;
        });

        Assert.True(seen);
        Assert.Equal(1, connection.BeginTransactionCallCount);
        Assert.Equal(1, connection.Transaction!.CommitCount);
        Assert.Equal(0, connection.Transaction.RollbackCount);
    }

    [Fact]
    public async Task WithTransactionAsync_rolls_back_on_failure()
    {
        var connection = new FakeDbConnection();
        var repo = new StubRepository(connection);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.WithTransactionAsync(_ => throw new InvalidOperationException("fail")));

        Assert.Equal(0, connection.Transaction!.CommitCount);
        Assert.Equal(1, connection.Transaction.RollbackCount);
    }

    private sealed class StubRepository(IDbConnection connection) : DapperXRepositoryBase<Product, int>(connection)
    {
        protected override DatabaseProvider Provider => DatabaseProvider.SqlServer;

        protected override string SelectByIdSql => "SELECT 1";
        protected override string SelectAllSql => "SELECT 1";
        protected override string SelectByIdsSql => "SELECT 1";
        protected override string ExistsSql => "SELECT 1";
        protected override string CountSql => "SELECT 1";
        protected override string InsertSql => "INSERT 1";
        protected override string UpdateSql => "UPDATE 1";
        protected override string DeleteSql => "DELETE 1";
        protected override string DeleteByIdSql => "DELETE 1";
        protected override string DeleteByIdsSql => "DELETE 1";
        protected override string SelectAllPageSql => "SELECT 1";
        protected override string SelectAllSliceSql => "SELECT 1";
        protected override string CountPageSql => "SELECT 1";
        protected override string UpsertSql => "INSERT 1";
    }

    private sealed class FakeDbConnection : IDbConnection
    {
        public FakeDbTransaction? Transaction { get; private set; }
        public int BeginTransactionCallCount { get; private set; }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => string.Empty;
        public ConnectionState State => ConnectionState.Open;

        public IDbTransaction BeginTransaction() => BeginTransaction(IsolationLevel.Unspecified);
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            BeginTransactionCallCount++;
            Transaction = new FakeDbTransaction(this);
            return Transaction;
        }

        public void Close() { }
        public void ChangeDatabase(string databaseName) { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class FakeDbTransaction(FakeDbConnection connection) : IDbTransaction
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public IDbConnection? Connection => connection;
        public IsolationLevel IsolationLevel => IsolationLevel.Unspecified;

        public void Commit() => CommitCount++;
        public void Rollback() => RollbackCount++;
        public void Dispose() { }
    }
}
