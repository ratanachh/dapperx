using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("bulk_shipments")]
public class BulkShipment
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public long Id { get; set; }

    [Column]
    public string TrackingCode { get; set; } = string.Empty;

    [Column]
    public int WeightGrams { get; set; }
}

[Repository]
public interface IBulkShipmentRepository : IRepository<BulkShipment, long>
{
}
