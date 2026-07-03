using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("products")]
[NamedEntityGraph("product.withCustomer", AttributeNodes = ["Customer"])]
public class Product
{
    [Transient]
    public bool PostLoadInvoked { get; private set; }

    [Transient]
    public bool PrePersistInvoked { get; private set; }

    [Transient]
    public bool PostPersistInvoked { get; private set; }

    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Name { get; set; } = string.Empty;

    [Embedded]
    public Address Address { get; set; } = new();

    [ManyToOne]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; } = null!;

    [PrePersist]
    public void OnPrePersist() => PrePersistInvoked = true;

    [PostPersist]
    public void OnPostPersist() => PostPersistInvoked = true;

    [PostLoad]
    public void OnPostLoad() => PostLoadInvoked = true;
}

[Repository]
public interface IProductRepository : IRepository<Product, int>
{
    Task<Product?> FindByNameAsync(string name);
    Task<IEnumerable<Product>> FindByNameLockedAsync(string name, LockMode lockMode);
    Task<IEnumerable<Product>> FindByNameWithGraphAsync(string name, string? entityGraph = null);
    Task<IEnumerable<Product>> FindAllByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name);
    Task<long> CountByNameAsync(string name);
    Task<IEnumerable<Product>> FindByNameOrderByIdDescAsync(string name);
    Task<IEnumerable<Product>> FindByAddressCityAsync(string addressCity);
    Task<IEnumerable<Product>> FindByCustomerNameAsync(string customerName);
    Task<IEnumerable<Product>> FindByCustomerIdAsync(int customerId);
    Task<IEnumerable<Product>> FindByNameAsync(string name, Sort sort);
    Task<IEnumerable<Product>> FindByNameAsync(string name, Sort sort, Pageable pageable);
    Task CreateAsync(Product product);
    [BulkOperation]
    Task InsertAsync(IEnumerable<Product> products);
    [Query("SELECT id, name FROM products WHERE name = @name", NativeQuery = true)]
    Task<IEnumerable<Product>> FindByNameNativeAsync(string name);

    [Query("SELECT p FROM Product p WHERE p.Name = :name")]
    Task<IEnumerable<Product>> FindByNameCpqlAsync(string name);

    [Query("SELECT p FROM Product p WHERE p.Customer.Name = :name")]
    Task<IEnumerable<Product>> FindByCustomerNameCpqlAsync(string name);

    [Query("SELECT COUNT(p) FROM Product p WHERE p.Name = :name")]
    Task<long> CountByNameCpqlAsync(string name);
}

[Projection(typeof(Product))]
public class ProductSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
