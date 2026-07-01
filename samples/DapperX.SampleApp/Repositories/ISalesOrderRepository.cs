using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.SampleApp.Entities;

namespace DapperX.SampleApp.Repositories;

[Repository]
public interface ISalesOrderRepository : IRepository<SalesOrder, int>
{
    Task<IReadOnlyList<SalesOrder>> FindByCodeAsync(string code, CancellationToken ct = default);
}
