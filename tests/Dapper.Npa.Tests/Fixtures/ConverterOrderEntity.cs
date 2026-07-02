using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

public enum OrderStatus
{
    Pending,
    Shipped,
}

[Entity]
[Table("converter_orders")]
public class ConverterOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Enumerated(EnumType.String)]
    public OrderStatus Status { get; set; }
}

[Repository]
public interface IConverterOrderRepository : IRepository<ConverterOrder, int>
{
}
