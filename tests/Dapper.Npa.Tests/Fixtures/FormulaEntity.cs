using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("formula_orders")]
public class FormulaOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Formula("(SELECT COUNT(*) FROM order_items oi WHERE oi.order_id = formula_orders.id)")]
    public int ItemCount { get; set; }
}

[Repository]
public interface IFormulaOrderRepository : IRepository<FormulaOrder, int>
{
}
