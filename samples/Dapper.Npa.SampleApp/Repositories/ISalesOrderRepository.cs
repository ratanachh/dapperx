using Dapper.Npa.SampleApp.Entities;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;

namespace Dapper.Npa.SampleApp.Repositories;

[Repository]
public interface ISalesOrderRepository : IRepository<SalesOrder, int>
{
    Task<IReadOnlyList<SalesOrder>> FindByCodeAsync(string code, CancellationToken ct = default);
}
