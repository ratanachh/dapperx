using System.Data;
using DapperX.Abstractions.Exceptions;
using DapperX.Abstractions.Sorting;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class InvalidSortExceptionTests
{
    [Fact]
    public async Task GetAllAsync_with_invalid_sort_column_throws_InvalidSortException()
    {
        var repo = new ProductRepositoryImpl(new StubDbConnection());
        var sort = new Sort("NotASortableColumn", Ascending: true);

        var ex = await Assert.ThrowsAsync<InvalidSortException>(() =>
            repo.GetAllAsync(sort));

        Assert.Contains("NotASortableColumn", ex.Message);
    }

    private sealed class StubDbConnection : IDbConnection
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => string.Empty;
        public ConnectionState State => ConnectionState.Closed;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Open() { }
        public void Dispose() { }
    }
}
