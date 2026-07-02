using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[MappedSuperclass]
public abstract class DocumentBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [ManyToOne]
    [JoinColumn("owner_id")]
    public User Owner { get; set; } = null!;
}

[Entity]
[Table("admin_documents")]
[AssociationOverride(nameof(DocumentBase.Owner), "admin_user_id")]
public class AdminDocument : DocumentBase
{
    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IAdminDocumentRepository : IRepository<AdminDocument, int>
{
}
