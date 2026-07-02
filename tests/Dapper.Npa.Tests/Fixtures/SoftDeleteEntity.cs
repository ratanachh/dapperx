using System.Data;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("archived_items")]
[SoftDelete]
public class ArchivedItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }
}

[Repository]
public interface IArchivedItemRepository : IRepository<ArchivedItem, int>
{
    [Query("SELECT a FROM ArchivedItem a WHERE a.Name = :name")]
    Task<IEnumerable<ArchivedItem>> FindByNameCpqlAsync(string name, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default);
}

[MappedSuperclass]
[SoftDelete]
public abstract class SoftDeletableBase
{
    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }
}

[Entity]
[Table("mapped_archived_items")]
public class MappedArchivedItem : SoftDeletableBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IMappedArchivedItemRepository : IRepository<MappedArchivedItem, int>
{
}

[Entity]
[Table("versioned_archived_items")]
[SoftDelete(DeletedAtColumn = "deleted_at")]
public class VersionedArchivedItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [Column(Name = "deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Version]
    [Column(Name = "version")]
    public int Version { get; set; }
}

[Repository]
public interface IVersionedArchivedItemRepository : IRepository<VersionedArchivedItem, int>
{
}

[Entity]
[Table("soft_delete_lifecycle_items")]
[SoftDelete]
public class SoftDeleteLifecycleItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [PreRemove]
    public void OnPreRemove() { }

    [PostRemove]
    public void OnPostRemove() { }
}

[Repository]
public interface ISoftDeleteLifecycleItemRepository : IRepository<SoftDeleteLifecycleItem, int>
{
}
