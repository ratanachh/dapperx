using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Performance.Tests.Fixtures;

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
