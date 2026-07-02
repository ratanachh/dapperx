using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

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
