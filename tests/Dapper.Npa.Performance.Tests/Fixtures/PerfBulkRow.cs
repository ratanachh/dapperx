using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Performance.Tests.Fixtures;

[Entity]
[Table("perf_bulk")]
public class PerfBulkRow
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public long Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Repository]
public interface IPerfBulkRowRepository : IRepository<PerfBulkRow, long>
{
}
