using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.StoredProcedures;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

public class ProcOrderSummary
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class ProcOrderLine
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
}

[Entity]
[Table("proc_orders")]
public class ProcOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Repository]
public interface IProcOrderRepository : IRepository<ProcOrder, int>
{
    [StoredProcedure("sp_list_proc_orders")]
    Task<IEnumerable<ProcOrder>> ListOrdersSpAsync(int customerId);

    [StoredProcedure("sp_process_proc_order",
        OutParameters = ["resultCode", "message"],
        InOutParameters = ["total"])]
    Task<ProcResult<int, string>> ProcessOrderSpAsync(int orderId, decimal total);

    [StoredProcedure("sp_proc_order_report",
        ResultSets = [typeof(ProcOrderSummary), typeof(ProcOrderLine)])]
    Task<MultiResult<ProcOrderSummary, ProcOrderLine>> GetOrderReportSpAsync(int orderId);
}
