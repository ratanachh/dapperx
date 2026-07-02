using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.IntegrationTests.Shared.Fixtures;

public class IntegCompositeOrderItemId
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
}

[Entity]
[Table("integ_composite_items")]
[IdClass(typeof(IntegCompositeOrderItemId))]
public class IntegCompositeOrderItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    [Column(Name = "product_id")]
    public int ProductId { get; set; }

    [Column]
    public int Quantity { get; set; }
}

[Repository]
public interface IIntegCompositeOrderItemRepository : IRepository<IntegCompositeOrderItem, IntegCompositeOrderItemId>
{
}

[Entity]
[Table("integ_documents")]
[SecondaryTable("integ_document_details", PrimaryKeyJoinColumn = "document_id")]
public class IntegDocument
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;

    [Column(Table = "integ_document_details")]
    public string Summary { get; set; } = string.Empty;
}

[Repository]
public interface IIntegDocumentRepository : IRepository<IntegDocument, int>
{
}

[Entity]
[Table("integ_users")]
public class IntegUser
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [OneToOne]
    [PrimaryKeyJoinColumn]
    public IntegUserProfile Profile { get; set; } = null!;
}

[Entity]
[Table("integ_user_profiles")]
public class IntegUserProfile
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string DisplayName { get; set; } = string.Empty;
}

[Repository]
public interface IIntegUserRepository : IRepository<IntegUser, int>
{
}

[Embeddable]
public class IntegProductImage
{
    [Column]
    public string Url { get; set; } = string.Empty;

    [Column]
    public string Caption { get; set; } = string.Empty;
}

[Entity]
[Table("integ_gallery_products")]
public class IntegGalleryProduct
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [ElementCollection]
    [CollectionTable("integ_product_images", JoinColumn = "product_id")]
    [AttributeOverride(nameof(IntegProductImage.Url), "image_url")]
    [AttributeOverride(nameof(IntegProductImage.Caption), "image_caption")]
    public LazyCollection<IntegProductImage> Images { get; set; } = new();
}

[Repository]
public interface IIntegGalleryProductRepository : IRepository<IntegGalleryProduct, int>
{
}

[Entity]
[Table("integ_departments")]
public class IntegDepartment
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(IntegEmployee.Department))]
    [MapKey("employee_code")]
    public LazyMap<string, IntegEmployee> EmployeesByCode { get; set; } = new(e => e.EmployeeCode);
}

[Entity]
[Table("integ_employees")]
public class IntegEmployee
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "department_id")]
    public int DepartmentId { get; set; }

    [Column(Name = "employee_code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column]
    public string FullName { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("department_id")]
    public IntegDepartment Department { get; set; } = null!;
}

[Repository]
public interface IIntegDepartmentRepository : IRepository<IntegDepartment, int>
{
}

[Entity]
[Table("integ_graph_parents")]
public class IntegGraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(IntegGraphChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<IntegGraphChild> Children { get; set; } = new();
}

[Entity]
[Table("integ_graph_children")]
public class IntegGraphChild
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "parent_id")]
    public int ParentId { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("parent_id")]
    public IntegGraphParent Parent { get; set; } = null!;
}

[Repository]
public interface IIntegGraphParentRepository : IRepository<IntegGraphParent, int>
{
}

[Entity]
[SoftDelete]
[Table("integ_graph_orders")]
[NamedEntityGraph("integGraphOrder.withLines", SubGraphs = ["Lines"])]
public class IntegGraphOrder
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [OneToMany(MappedBy = nameof(IntegGraphOrderLine.Order), Cascade = CascadeType.All)]
    public LazyCollection<IntegGraphOrderLine> Lines { get; set; } = new();
}

[Entity]
[SoftDelete]
[Table("integ_graph_order_lines")]
public class IntegGraphOrderLine
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public IntegGraphOrder Order { get; set; } = null!;
}

[Repository]
public interface IIntegGraphOrderRepository : IRepository<IntegGraphOrder, int>
{
    Task<IEnumerable<IntegGraphOrder>> FindByCodeWithGraphAsync(string code, string? entityGraph = null);
}

#if !DAPPERX_PROVIDER_SQLITE
public class IntegProcOrderSummary
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class IntegProcOrderLine
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
}

[Entity]
[Table("integ_proc_orders")]
public class IntegProcOrder
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Entity]
[Table("integ_proc_lines")]
public class IntegProcLine
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;
}

[Repository]
public interface IIntegProcOrderRepository : IRepository<IntegProcOrder, int>
{
    [StoredProcedure("sp_integ_list_proc_orders")]
    Task<IEnumerable<IntegProcOrder>> ListOrdersSpAsync(int customerId);

    [StoredProcedure("sp_integ_process_proc_order",
        OutParameters = ["resultCode", "message"],
        InOutParameters = ["total"])]
    Task<Dapper.Npa.Abstractions.StoredProcedures.ProcResult<int, string>> ProcessOrderSpAsync(int orderId, decimal total);

    [StoredProcedure("sp_integ_proc_order_report",
        ResultSets = [typeof(IntegProcOrderSummary), typeof(IntegProcOrderLine)])]
    Task<Dapper.Npa.Abstractions.StoredProcedures.MultiResult<IntegProcOrderSummary, IntegProcOrderLine>> GetOrderReportSpAsync(int orderId);
}
#endif
