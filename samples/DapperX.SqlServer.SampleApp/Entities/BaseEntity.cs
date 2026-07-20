using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.SqlServer.SampleApp.Entities;

[MappedSuperclass]
public abstract class BaseEntity
{
    [CreatedDate]
    [Column(Name = "created_at")]
    public DateTime CreatedAt { get; set; }

    [LastModifiedDate]
    [Column(Name = "modified_at")]
    public DateTime ModifiedAt { get; set; }

    [CreatedBy]
    [Column(Name = "created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [LastModifiedBy]
    [Column(Name = "modified_by")]
    public string ModifiedBy { get; set; } = string.Empty;

    [Version]
    [Column(Name = "row_version")]
    public int RowVersion { get; set; }

    [Generated(GenerationTime.Insert)]
    [Column(Name = "db_created_at")]
    public DateTime DbCreatedAt { get; set; }
}
