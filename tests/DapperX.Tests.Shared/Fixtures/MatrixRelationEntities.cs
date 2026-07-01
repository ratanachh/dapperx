using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Shared.Fixtures;

[Entity]
[Table("matrix_locked_products")]
public class MatrixLockedProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixLockedProductRepository : IRepository<MatrixLockedProduct, int>
{
    Task<IEnumerable<MatrixLockedProduct>> FindByNameAsync(string name);
#if !DAPPERX_PROVIDER_SQLITE
    Task<IEnumerable<MatrixLockedProduct>> FindByNameLockedAsync(string name, LockMode lockMode);
#endif
}

[Entity]
[Table("matrix_versioned_items")]
[SoftDelete]
public class MatrixVersionedItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [Version]
    [Column(Name = "row_version")]
    public int RowVersion { get; set; }
}

[Repository]
public interface IMatrixVersionedItemRepository : IRepository<MatrixVersionedItem, int>
{
}

[Entity]
[Table("matrix_documents")]
[SecondaryTable("matrix_document_details", PrimaryKeyJoinColumn = "document_id")]
public class MatrixDocument
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;

    [Column(Table = "matrix_document_details")]
    public string Summary { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixDocumentRepository : IRepository<MatrixDocument, int>
{
}

[Entity]
[Table("matrix_users")]
public class MatrixUser
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [OneToOne]
    [PrimaryKeyJoinColumn]
    public MatrixUserProfile Profile { get; set; } = null!;
}

[Entity]
[Table("matrix_user_profiles")]
public class MatrixUserProfile
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string DisplayName { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixUserRepository : IRepository<MatrixUser, int>
{
}

[Embeddable]
public class MatrixProductImage
{
    [Column]
    public string Url { get; set; } = string.Empty;

    [Column]
    public string Caption { get; set; } = string.Empty;
}

[Entity]
[Table("matrix_gallery_products")]
public class MatrixGalleryProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [ElementCollection]
    [CollectionTable("matrix_product_images", JoinColumn = "product_id")]
    [AttributeOverride(nameof(MatrixProductImage.Url), "image_url")]
    [AttributeOverride(nameof(MatrixProductImage.Caption), "image_caption")]
    public LazyCollection<MatrixProductImage> Images { get; set; } = new();
}

[Repository]
public interface IMatrixGalleryProductRepository : IRepository<MatrixGalleryProduct, int>
{
}

[Entity]
[Table("matrix_departments")]
public class MatrixDepartment
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(MatrixEmployee.Department))]
    [MapKey("employee_code")]
    public LazyMap<string, MatrixEmployee> EmployeesByCode { get; set; } = new(e => e.EmployeeCode);
}

[Entity]
[Table("matrix_employees")]
public class MatrixEmployee
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "department_id")]
    public int DepartmentId { get; set; }

    [Column(Name = "employee_code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column]
    public string FullName { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("department_id")]
    public MatrixDepartment Department { get; set; } = null!;
}

[Repository]
public interface IMatrixDepartmentRepository : IRepository<MatrixDepartment, int>
{
}
